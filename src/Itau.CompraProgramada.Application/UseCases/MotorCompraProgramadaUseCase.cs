using System;
using System.Linq;
using System.Threading.Tasks;
using Itau.CompraProgramada.Domain.Entities;
using Itau.CompraProgramada.Domain.Enums;
using Itau.CompraProgramada.Domain.Interfaces;
using Itau.CompraProgramada.Domain.Services;

namespace Itau.CompraProgramada.Application.UseCases;

public class MotorCompraProgramadaUseCase : IMotorCompraProgramadaUseCase
{
    private readonly IClienteRepository _clienteRepository;
    private readonly ICestaRecomendacaoRepository _cestaRepository;
    private readonly ICotacaoB3Provider _cotacaoProvider;
    private readonly IEventoIRPublisher _eventoIRPublisher;
    private readonly IUnitOfWork _unitOfWork;
    
    // Serviços de Domínio injetados via DI
    private readonly CalculadoraLoteFracionarioService _calculadoraLote;
    private readonly DistribuicaoProporcionalService _distribuicaoService;
    private readonly DataCompraService _dataCompraService;

    public MotorCompraProgramadaUseCase(
        IClienteRepository clienteRepository,
        ICestaRecomendacaoRepository cestaRepository,
        ICotacaoB3Provider cotacaoProvider,
        IEventoIRPublisher eventoIRPublisher,
        IUnitOfWork unitOfWork,
        CalculadoraLoteFracionarioService calculadoraLote,
        DistribuicaoProporcionalService distribuicaoService,
        DataCompraService dataCompraService)
    {
        _clienteRepository = clienteRepository;
        _cestaRepository = cestaRepository;
        _cotacaoProvider = cotacaoProvider;
        _eventoIRPublisher = eventoIRPublisher;
        _unitOfWork = unitOfWork;
        _calculadoraLote = calculadoraLote;
        _distribuicaoService = distribuicaoService;
        _dataCompraService = dataCompraService;
    }

    public async Task<string> ExecutarComprasAsync(DateTime dataReferencia)
    {
        // RN-020 a RN-022: Validação da data de compra (apenas dias úteis 5, 15, 25)
        if (!_dataCompraService.EhDiaDeCompraValido(dataReferencia))
        {
            return $"A data {dataReferencia:dd/MM/yyyy} não é um dia válido para execução da compra programada.";
        }

        // 1. Obter a Cesta Ativa
        var cesta = await _cestaRepository.ObterAtivaAsync();
        if (cesta == null)
            throw new InvalidOperationException("Nenhuma cesta de recomendação ativa encontrada.");

        // 2. Obter Clientes Ativos
        var clientes = (await _clienteRepository.ObterClientesAtivosComCustodiaAsync()).ToList();
        if (!clientes.Any())
            return "Nenhum cliente ativo para processar.";

        // 2.1 Obter Conta Master (RN-029, RN-030)
        var master = await _clienteRepository.ObterClienteMasterAsync();
        if (master == null)
            throw new InvalidOperationException("Conta Master não encontrada. Impossível prosseguir sem a custódia central.");

        // 3. Ler Cotações do ficheiro da B3 e colocar num Dicionário em memória
        var cotacoes = _cotacaoProvider.ObterCotacoesDeFechamento()
            .ToDictionary(c => c.Ticker, c => c.PrecoFechamento);

        // Validar se o ficheiro tem a cotação de todos os ativos da cesta
        foreach (var item in cesta.Itens)
        {
            if (!cotacoes.ContainsKey(item.Ticker))
                throw new InvalidOperationException($"Cotação de fechamento não encontrada para o ticker {item.Ticker}.");
        }

        // 4. Calcular o montante financeiro total a investir
        // RN-023: Usar apenas 1/3 do valor mensal configurado (arredondado para 2 casas)
        var aportesClientes = clientes.ToDictionary(c => c.Id, c => Math.Round(c.ValorMensal / 3m, 2));
        decimal totalAportes = aportesClientes.Values.Sum();

        int eventosPublicados = 0;

        // 5. Processar cada ativo da cesta individualmente
        foreach (var item in cesta.Itens)
        {
            string ticker = item.Ticker;
            decimal precoCotacao = cotacoes[ticker];
            
            // Valor financeiro total destinado a este ativo (ex: 20% do total de aportes)
            decimal valorAlocado = totalAportes * (item.Percentual / 100m);
            
            // Quantidade total a comprar no mercado (truncada)
            int qtdMercadoComprar = (int)Math.Truncate(valorAlocado / precoCotacao);

            // RN-037: Usar saldo da conta master da compra anterior e somar à quantidade disponível para rateio
            var custodiaMasterAtivo = master.ContaGrafica.Custodias.FirstOrDefault(c => c.Ticker == ticker);
            int qtdDisponivelMaster = custodiaMasterAtivo?.Quantidade ?? 0;
            
            int qtdTotalDisponivelDistribuicao = qtdMercadoComprar + qtdDisponivelMaster;

            if (qtdTotalDisponivelDistribuicao <= 0) continue;

            // 6. Calcular a divisão em Lote Padrão e Mercado Fracionário (Apenas informativo para o log/ordem)
            var divisoesMercado = _calculadoraLote.Calcular(ticker, qtdTotalDisponivelDistribuicao);

            // 7. Rateio Proporcional entre os Clientes
            // O serviço de domínio trata a matemática complexa da distribuição e retorna o que sobra!
            var resultadoRateio = _distribuicaoService.Distribuir(qtdTotalDisponivelDistribuicao, aportesClientes);

            foreach (var distribuicao in resultadoRateio.Distribuicoes)
            {
                var cliente = clientes.First(c => c.Id == distribuicao.ClienteId);
                var custodia = cliente.ContaGrafica.Custodias.FirstOrDefault(c => c.Ticker == ticker);

                if (custodia == null)
                {
                    custodia = new Custodia(cliente.ContaGrafica.Id, ticker, 0, 0);
                    cliente.ContaGrafica.AdicionarCustodia(custodia);
                }

                // A própria entidade atualiza o Preço Médio e adiciona a nova quantidade
                custodia.AdicionarCompra(distribuicao.Quantidade, precoCotacao);

                // 8. Cálculo do IR Dedo-Duro (0,005% sobre o valor distribuído na compra)
                decimal valorOperacao = distribuicao.Quantidade * precoCotacao;
                decimal valorIR = Math.Round(valorOperacao * 0.00005m, 2);

                if (valorIR > 0)
                {
                    var eventoIR = new EventoIR(cliente.Id, TipoEventoIR.DedoDuro, valorOperacao, valorIR);
                    
                    // Publicar a mensagem no Kafka
                    await _eventoIRPublisher.PublicarEventoAsync(eventoIR);
                    eventosPublicados++;
                }
                
                // Informar ao repositório que este cliente sofreu alterações
                _clienteRepository.Atualizar(cliente);
            }

            // RN-039 / RN-040: O que sobrou na divisão fracionária fica para a Conta Master
            if (resultadoRateio.ResiduoMaster > 0 || qtdDisponivelMaster > 0)
            {
                if (custodiaMasterAtivo == null)
                {
                    custodiaMasterAtivo = new Custodia(master.ContaGrafica.Id, ticker, 0, 0);
                    master.ContaGrafica.AdicionarCustodia(custodiaMasterAtivo);
                }
                
                // Sobrescreve com o resíduo correto desta operação (se sobrou menos que o saldo anterior, o saldo ajusta)
                // Usamos AtualizarSaldo (ou hack com reflection/adição neutra no Preço Médio)
                // Como não vendemos, simulamos uma "reatribuição" limpando e recadastrando pra ficar limpo
                // Custo médio não é obrigatório no master, vamos apenas fixar a quantidade
                var campoQtd = typeof(Custodia).GetProperty("Quantidade");
                campoQtd!.SetValue(custodiaMasterAtivo, resultadoRateio.ResiduoMaster);
                
                _clienteRepository.Atualizar(master);
            }
        }

        // 9. Guardar tudo na base de dados numa transação única!
        await _unitOfWork.CommitAsync();

        return $"Compra executada com sucesso para {clientes.Count} clientes. {eventosPublicados} eventos de retenção na fonte publicados.";
    }
}