using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Itau.CompraProgramada.Application.UseCases;
using Itau.CompraProgramada.Domain.Entities;
using Itau.CompraProgramada.Domain.Enums;
using Itau.CompraProgramada.Domain.Interfaces;
using Itau.CompraProgramada.Domain.Services;
using NSubstitute;
using Xunit;

namespace Itau.CompraProgramada.Tests.Application.UseCases;

public class RebalanceamentoPorMudancaCestaUseCaseTests
{
    private readonly IClienteRepository _clienteRepositoryMock;
    private readonly ICotacaoB3Provider _cotacaoProviderMock;
    private readonly IRebalanceamentoRepository _rebalanceamentoRepositoryMock;
    private readonly CalculoIRService _calculoIRService;
    private readonly IEventoIRPublisher _eventoIRPublisherMock;
    private readonly IUnitOfWork _unitOfWorkMock;
    private readonly RebalanceamentoPorMudancaCestaUseCase _sut;

    public RebalanceamentoPorMudancaCestaUseCaseTests()
    {
        _clienteRepositoryMock = Substitute.For<IClienteRepository>();
        _cotacaoProviderMock = Substitute.For<ICotacaoB3Provider>();
        _rebalanceamentoRepositoryMock = Substitute.For<IRebalanceamentoRepository>();
        _eventoIRPublisherMock = Substitute.For<IEventoIRPublisher>();
        _unitOfWorkMock = Substitute.For<IUnitOfWork>();
        _calculoIRService = new CalculoIRService();

        _sut = new RebalanceamentoPorMudancaCestaUseCase(
            _clienteRepositoryMock,
            _cotacaoProviderMock,
            _rebalanceamentoRepositoryMock,
            _calculoIRService,
            _eventoIRPublisherMock,
            _unitOfWorkMock
        );
    }

    [Fact]
    public async Task ExecutarRebalanceamentoAsync_AtivoSaiuDaCesta_DeveVenderTodaPosicao()
    {
        // Arrange
        var cestaAntiga = new CestaRecomendacao("Cesta Antiga", new List<ItemCesta>
        {
            new("PETR4", 30),
            new("VALE3", 25),
            new("ITUB4", 20),
            new("BBDC4", 15),
            new("WEGE3", 10)
        });

        var cestaNova = new CestaRecomendacao("Cesta Nova", new List<ItemCesta>
        {
            new("PETR4", 25),
            new("VALE3", 20),
            new("ITUB4", 20),
            new("ABEV3", 20),
            new("RENT3", 15)
        });

        var cliente = new Cliente("Joao", "11122233344", "joao@email.com", 1000m);
        var contaCorrente = new ContaGrafica((long?)cliente.Id, "123", TipoContaGrafica.Filhote);

        typeof(Cliente).GetProperty("ContaGrafica")!.SetValue(cliente, contaCorrente);
        
        // Setup Custodia para BBDC4 (vai sair) e WEGE3 (vai sair)
        var custodiaBbdc = new Custodia(contaCorrente.Id, "BBDC4", 10, 15m);
        var custodiaWege = new Custodia(contaCorrente.Id, "WEGE3", 2, 40m);
        typeof(ContaGrafica).GetField("_custodias", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!.SetValue(contaCorrente, new List<Custodia> { custodiaBbdc, custodiaWege });

        _clienteRepositoryMock.ObterClientesAtivosComCustodiaAsync().Returns(new List<Cliente> { cliente });

        _cotacaoProviderMock.ObterCotacoesDeFechamento().Returns(new List<CotacaoDto>
        {
            new CotacaoDto { Ticker = "VALE3", PrecoFechamento = 60m },
            new CotacaoDto { Ticker = "PETR4", PrecoFechamento = 40m },
            new CotacaoDto { Ticker = "ITUB4", PrecoFechamento = 30m },
            new CotacaoDto { Ticker = "BBDC4", PrecoFechamento = 15m },
            new CotacaoDto { Ticker = "WEGE3", PrecoFechamento = 40m }
        });

        _rebalanceamentoRepositoryMock.ObterVendasMesCorrenteAsync(Arg.Any<long>(), Arg.Any<int>(), Arg.Any<int>())
            .Returns(new List<Rebalanceamento>());

        // Act
        await _sut.ExecutarRebalanceamentoAsync(cestaAntiga, cestaNova);

        // Assert
        cliente.ContaGrafica.Custodias.First(c => c.Ticker == "BBDC4").Quantidade.Should().Be(0);
        cliente.ContaGrafica.Custodias.First(c => c.Ticker == "WEGE3").Quantidade.Should().Be(0);
        await _unitOfWorkMock.Received(1).CommitAsync();
    }

    [Fact]
    public async Task ExecutarRebalanceamentoAsync_AtivoMudouPercentual_DeveVenderExcessoEComprarDeficit()
    {
        // Arrange
        var cestaAntiga = new CestaRecomendacao("Cesta Antiga", new List<ItemCesta>
        {
            new("PETR4", 30), new("VALE3", 25), new("ITUB4", 20), new("BBDC4", 15), new("WEGE3", 10)
        });

        // Nova cesta com ABEV3 e RENT3, e pesos ajustados
        var cestaNova = new CestaRecomendacao("Cesta Nova", new List<ItemCesta>
        {
            new("PETR4", 25), new("VALE3", 20), new("ITUB4", 20), new("ABEV3", 20), new("RENT3", 15)
        });

        var cliente = new Cliente("Maria", "99988877766", "maria@email.com", 1000m);
        var contaCorrente = new ContaGrafica((long?)cliente.Id, "456", TipoContaGrafica.Filhote);

        typeof(Cliente).GetProperty("ContaGrafica")!.SetValue(cliente, contaCorrente);
        
        // Carteira inicial da Maria = Total R$ 938,00
        // PETR4: 8 acoes x R$ 35,00 = R$ 280,00 (30%)
        // VALE3: 4 acoes x R$ 62,00 = R$ 248,00 (26,5%)
        // ITUB4: 6 acoes x R$ 30,00 = R$ 180,00 (19,2%)
        // BBDC4: 10 acoes x R$ 15,00 = R$ 150,00 (16%)
        // WEGE3: 2 acoes x R$ 40,00 = R$ 80,00 (8,5%)
        var custodias = new List<Custodia>
        {
            new(contaCorrente.Id, "PETR4", 8, 30m),
            new(contaCorrente.Id, "VALE3", 4, 60m),
            new(contaCorrente.Id, "ITUB4", 6, 25m),
            new(contaCorrente.Id, "BBDC4", 10, 10m),
            new(contaCorrente.Id, "WEGE3", 2, 35m)
        };
        typeof(ContaGrafica).GetField("_custodias", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!.SetValue(contaCorrente, custodias);

        _clienteRepositoryMock.ObterClientesAtivosComCustodiaAsync().Returns(new List<Cliente> { cliente });

        _cotacaoProviderMock.ObterCotacoesDeFechamento().Returns(new List<CotacaoDto>
        {
            new CotacaoDto { Ticker = "PETR4", PrecoFechamento = 35m },
            new CotacaoDto { Ticker = "VALE3", PrecoFechamento = 62m },
            new CotacaoDto { Ticker = "ITUB4", PrecoFechamento = 30m },
            new CotacaoDto { Ticker = "BBDC4", PrecoFechamento = 15m },
            new CotacaoDto { Ticker = "WEGE3", PrecoFechamento = 40m },
            new CotacaoDto { Ticker = "ABEV3", PrecoFechamento = 14m },
            new CotacaoDto { Ticker = "RENT3", PrecoFechamento = 48m }
        });

        _rebalanceamentoRepositoryMock.ObterVendasMesCorrenteAsync(Arg.Any<long>(), Arg.Any<int>(), Arg.Any<int>())
            .Returns(new List<Rebalanceamento>());

        // Act
        await _sut.ExecutarRebalanceamentoAsync(cestaAntiga, cestaNova);

        // Assert
        // Target Total = 938
        // BBDC4 e WEGE3 zerados
        cliente.ContaGrafica.Custodias.First(c => c.Ticker == "BBDC4").Quantidade.Should().Be(0);
        cliente.ContaGrafica.Custodias.First(c => c.Ticker == "WEGE3").Quantidade.Should().Be(0);
        
        // PETR4 Target = 938 * 0.25 = 234.50 => 234.50 / 35 = 6.7 -> 6 ações. Maria tem 8. Deve vender 2.
        cliente.ContaGrafica.Custodias.First(c => c.Ticker == "PETR4").Quantidade.Should().Be(6);
        
        // VALE3 Target = 938 * 0.20 = 187.60 => 187.60 / 62 = 3.02 -> 3 ações. Maria tem 4. Deve vender 1.
        cliente.ContaGrafica.Custodias.First(c => c.Ticker == "VALE3").Quantidade.Should().Be(3);
        
        // ITUB4 Target = 938 * 0.20 = 187.60 => 187.60 / 30 = 6.25 -> 6 ações. Maria já tem 6. Mantém 6.
        cliente.ContaGrafica.Custodias.First(c => c.Ticker == "ITUB4").Quantidade.Should().Be(6);
        
        // ABEV3 Target = 938 * 0.20 = 187.60 => 187.60 / 14 = 13.4 -> 13 ações. Maria tem 0. Deve comprar 13.
        cliente.ContaGrafica.Custodias.FirstOrDefault(c => c.Ticker == "ABEV3")?.Quantidade.Should().Be(13);
        
        // RENT3 Target = 938 * 0.15 = 140.70 => 140.70 / 48 = 2.93 -> 2 ações. Maria tem 0. Deve comprar 2.
        cliente.ContaGrafica.Custodias.FirstOrDefault(c => c.Ticker == "RENT3")?.Quantidade.Should().Be(2);

        await _unitOfWorkMock.Received(1).CommitAsync();
    }
}
