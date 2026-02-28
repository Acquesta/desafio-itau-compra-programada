using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Itau.CompraProgramada.Domain.Entities;
using Itau.CompraProgramada.Domain.Interfaces;
using Itau.CompraProgramada.Domain.Services;

namespace Itau.CompraProgramada.Application.UseCases;

/// <summary>
/// RN-050 a RN-052: Rebalanceamento por desvio de proporção.
/// Analisa todos os clientes ativos, detecta desvios e executa compra/venda.
/// </summary>
public class RebalanceamentoPorDesvioUseCase : IRebalanceamentoPorDesvioUseCase
{
    private readonly IClienteRepository _clienteRepository;
    private readonly ICestaRecomendacaoRepository _cestaRepository;
    private readonly ICotacaoB3Provider _cotacaoProvider;
    private readonly CalculoDesvioService _calculoDesvioService;
    private readonly IRebalanceamentoRepository _rebalanceamentoRepository;
    private readonly CalculoIRService _calculoIRService;
    private readonly IEventoIRPublisher _eventoIRPublisher;
    private readonly IUnitOfWork _unitOfWork;

    public RebalanceamentoPorDesvioUseCase(
        IClienteRepository clienteRepository,
        ICestaRecomendacaoRepository cestaRepository,
        ICotacaoB3Provider cotacaoProvider,
        CalculoDesvioService calculoDesvioService,
        IRebalanceamentoRepository rebalanceamentoRepository,
        CalculoIRService calculoIRService,
        IEventoIRPublisher eventoIRPublisher,
        IUnitOfWork unitOfWork)
    {
        _clienteRepository = clienteRepository;
        _cestaRepository = cestaRepository;
        _cotacaoProvider = cotacaoProvider;
        _calculoDesvioService = calculoDesvioService;
        _rebalanceamentoRepository = rebalanceamentoRepository;
        _calculoIRService = calculoIRService;
        _eventoIRPublisher = eventoIRPublisher;
        _unitOfWork = unitOfWork;
    }

    public async Task<RebalanceamentoDesvioResponse> ExecutarRebalanceamentoPorDesvioAsync(decimal limiarPontoPercentual = 5m)
    {
        var cestaAtual = await _cestaRepository.ObterAtivaAsync();
        if (cestaAtual == null)
            throw new InvalidOperationException("Não há cesta ativa para rebalancear.");

        var clientesAtivos = await _clienteRepository.ObterClientesAtivosComCustodiaAsync();
        var cotacoes = _cotacaoProvider.ObterCotacoesDeFechamento()
            .ToDictionary(c => c.Ticker, c => c.PrecoFechamento);

        int clientesAnalisados = 0;
        int clientesRebalanceados = 0;
        int totalVendas = 0;
        int totalCompras = 0;

        foreach (var cliente in clientesAtivos)
        {
            if (cliente.ContaGrafica == null) continue;
            clientesAnalisados++;

            var desvios = _calculoDesvioService.CalcularDesvios(
                cliente.ContaGrafica.Custodias, cestaAtual, cotacoes, limiarPontoPercentual);

            if (!desvios.Any()) continue;
            clientesRebalanceados++;

            decimal totalCarteira = desvios.First().TotalCarteira;

            // Para cada ativo na cesta, calcular qtd alvo e ajustar
            foreach (var item in cestaAtual.Itens)
            {
                decimal valorAlvo = totalCarteira * (item.Percentual / 100m);
                decimal preco = cotacoes.TryGetValue(item.Ticker, out var p) ? p : 0m;
                if (preco == 0) continue;

                int qtdAlvo = (int)Math.Truncate(valorAlvo / preco);
                var custodia = cliente.ContaGrafica.Custodias.FirstOrDefault(c => c.Ticker == item.Ticker);
                int qtdAtual = custodia?.Quantidade ?? 0;

                if (qtdAtual > qtdAlvo)
                {
                    // Sobre-alocado: vender excesso
                    int qtdVender = qtdAtual - qtdAlvo;
                    var precoMedioAntigo = custodia!.PrecoMedio;
                    
                    custodia.RemoverVenda(qtdVender);
                    totalVendas += qtdVender;
                    
                    decimal valorOperacao = qtdVender * preco;
                    decimal lucroDaOperacao = (preco - precoMedioAntigo) * qtdVender;
                    
                    await _rebalanceamentoRepository.AdicionarAsync(new Rebalanceamento(
                        cliente.Id, 
                        Itau.CompraProgramada.Domain.Enums.TipoRebalanceamento.Desvio,
                        item.Ticker,
                        "",
                        valorOperacao,
                        qtdVender,
                        precoMedioAntigo,
                        lucroDaOperacao));
                }
                else if (qtdAtual < qtdAlvo)
                {
                    // Sub-alocado: comprar déficit
                    int qtdComprar = qtdAlvo - qtdAtual;
                    if (custodia == null)
                    {
                        custodia = new Custodia(cliente.ContaGrafica.Id, item.Ticker, 0, preco);
                        cliente.ContaGrafica.AdicionarCustodia(custodia);
                    }
                    custodia.AdicionarCompra(qtdComprar, preco);
                    totalCompras += qtdComprar;
                }
            }

            // Vender ativos que não estão na cesta
            var tickersCesta = cestaAtual.Itens.Select(i => i.Ticker).ToHashSet();
            foreach (var custodia in cliente.ContaGrafica.Custodias.ToList())
            {
                if (tickersCesta.Contains(custodia.Ticker)) continue;
                if (custodia.Quantidade > 0)
                {
                    totalVendas += custodia.Quantidade;
                    
                    var precoMedioAntigo = custodia.PrecoMedio;
                    decimal precoVendaAtivo = cotacoes.TryGetValue(custodia.Ticker, out var precoCusto) ? precoCusto : 0m;
                    decimal valorOperacao = custodia.Quantidade * precoVendaAtivo;
                    decimal lucroDaOperacao = (precoVendaAtivo - precoMedioAntigo) * custodia.Quantidade;
                    
                    await _rebalanceamentoRepository.AdicionarAsync(new Rebalanceamento(
                        cliente.Id, 
                        Itau.CompraProgramada.Domain.Enums.TipoRebalanceamento.Desvio,
                        custodia.Ticker,
                        "",
                        valorOperacao,
                        custodia.Quantidade,
                        precoMedioAntigo,
                        lucroDaOperacao));
                        
                    custodia.RemoverVenda(custodia.Quantidade);
                }
            }

            // RN-057 a RN-061: Calcular o IR sobre as vendas deste cliente neste mês (se exceder os 20k de vendas)
            var vendasMes = await _rebalanceamentoRepository.ObterVendasMesCorrenteAsync(cliente.Id, DateTime.UtcNow.Month, DateTime.UtcNow.Year);
            decimal valorTotalVendasMes = vendasMes.Sum(v => v.ValorVenda);
            decimal lucroLiquidoTotal = vendasMes.Sum(v => v.LucroLiquido);
            
            decimal impostoDevido = _calculoIRService.CalcularIRSobreVendas(valorTotalVendasMes, lucroLiquidoTotal);
            
            if (impostoDevido > 0)
            {
                var eventoIR = new EventoIR(
                    cliente.Id, 
                    cliente.Cpf, 
                    "MULTIPLE", 
                    Itau.CompraProgramada.Domain.Enums.TipoEventoIR.Venda20Percent, 
                    lucroLiquidoTotal, 
                    impostoDevido, 
                    1, 
                    1m); // Estes dois últimos parâmetros não fazem sentido pro payload global, mas instanciam o evento corretamente
                    
                await _eventoIRPublisher.PublicarEventoAsync(eventoIR);
            }

            _clienteRepository.Atualizar(cliente);
        }

        if (clientesRebalanceados > 0)
            await _unitOfWork.CommitAsync();

        return new RebalanceamentoDesvioResponse(clientesAnalisados, clientesRebalanceados, totalVendas, totalCompras);
    }
}
