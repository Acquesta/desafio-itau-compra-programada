using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Itau.CompraProgramada.Domain.Entities;
using Itau.CompraProgramada.Domain.Interfaces;
using Itau.CompraProgramada.Domain.Services;

namespace Itau.CompraProgramada.Application.UseCases;

public class RebalanceamentoPorMudancaCestaUseCase : IRebalanceamentoPorMudancaCestaUseCase
{
    private readonly IClienteRepository _clienteRepository;
    private readonly ICotacaoB3Provider _cotacaoProvider;
    private readonly IRebalanceamentoRepository _rebalanceamentoRepository;
    private readonly CalculoIRService _calculoIRService;
    private readonly IEventoIRPublisher _eventoIRPublisher;
    private readonly IUnitOfWork _unitOfWork;

    public RebalanceamentoPorMudancaCestaUseCase(
        IClienteRepository clienteRepository,
        ICotacaoB3Provider cotacaoProvider,
        IRebalanceamentoRepository rebalanceamentoRepository,
        CalculoIRService calculoIRService,
        IEventoIRPublisher eventoIRPublisher,
        IUnitOfWork unitOfWork)
    {
        _clienteRepository = clienteRepository;
        _cotacaoProvider = cotacaoProvider;
        _rebalanceamentoRepository = rebalanceamentoRepository;
        _calculoIRService = calculoIRService;
        _eventoIRPublisher = eventoIRPublisher;
        _unitOfWork = unitOfWork;
    }

    public async Task ExecutarRebalanceamentoAsync(CestaRecomendacao cestaAntiga, CestaRecomendacao cestaNova)
    {
        var clientesAtivos = await _clienteRepository.ObterClientesAtivosComCustodiaAsync();
        var cotacoes = _cotacaoProvider.ObterCotacoesDeFechamento().ToDictionary(c => c.Ticker, c => c.PrecoFechamento);

        foreach (var cliente in clientesAtivos)
        {
            if (cliente.ContaGrafica == null) continue;

            decimal totalCarteira = 0;
            foreach (var custodia in cliente.ContaGrafica.Custodias)
            {
                if (cotacoes.TryGetValue(custodia.Ticker, out var preco))
                    totalCarteira += custodia.Quantidade * preco;
            }

            if (totalCarteira == 0) continue;

            // 1. Vender ativos sobre-alocados ou que saíram
            var custodiasAtuais = cliente.ContaGrafica.Custodias.ToList();
            foreach (var custodia in custodiasAtuais)
            {
                var itemNovaCesta = cestaNova.Itens.FirstOrDefault(i => i.Ticker == custodia.Ticker);
                decimal percentualAlvo = itemNovaCesta?.Percentual ?? 0m;

                decimal valorAlvo = totalCarteira * (percentualAlvo / 100m);
                decimal valorAtual = 0;
                
                if (cotacoes.TryGetValue(custodia.Ticker, out var preco))
                    valorAtual = custodia.Quantidade * preco;

                if (valorAtual > valorAlvo && preco > 0)
                {
                    int qtdAlvo = (int)Math.Truncate(valorAlvo / preco);
                    int qtdParaVender = custodia.Quantidade - qtdAlvo;

                    if (qtdParaVender > 0)
                    {
                        var precoMedioAntigo = custodia.PrecoMedio;
                        decimal valorOperacao = qtdParaVender * preco;
                        decimal lucroDaOperacao = (preco - precoMedioAntigo) * qtdParaVender;
                        
                        await _rebalanceamentoRepository.AdicionarAsync(new Rebalanceamento(
                            cliente.Id, 
                            Itau.CompraProgramada.Domain.Enums.TipoRebalanceamento.MudancaCesta,
                            custodia.Ticker,
                            "",
                            valorOperacao,
                            qtdParaVender,
                            precoMedioAntigo,
                            lucroDaOperacao));
                            
                        custodia.RemoverVenda(qtdParaVender);
                    }
                }
            }

            // 2. Comprar ativos sub-alocados
            foreach (var itemNovo in cestaNova.Itens)
            {
                var custodia = cliente.ContaGrafica.Custodias.FirstOrDefault(c => c.Ticker == itemNovo.Ticker);
                decimal valorAlvo = totalCarteira * (itemNovo.Percentual / 100m);
                decimal valorAtual = 0;
                decimal preco = cotacoes.TryGetValue(itemNovo.Ticker, out var p) ? p : 0m;
                
                if (custodia != null && preco > 0)
                    valorAtual = custodia.Quantidade * preco;

                if (valorAlvo > valorAtual && preco > 0)
                {
                    int qtdAlvo = (int)Math.Truncate(valorAlvo / preco);
                    int qtdAtual = custodia?.Quantidade ?? 0;
                    int qtdParaComprar = qtdAlvo - qtdAtual;

                    if (qtdParaComprar > 0)
                    {
                        if (custodia == null)
                        {
                            custodia = new Custodia(cliente.ContaGrafica.Id, itemNovo.Ticker, 0, preco);
                            cliente.ContaGrafica.AdicionarCustodia(custodia);
                        }
                        custodia.AdicionarCompra(qtdParaComprar, preco);
                    }
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

        await _unitOfWork.CommitAsync();
    }
}
