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

public class RebalanceamentoPorDesvioUseCaseTests
{
    private readonly IClienteRepository _clienteRepositoryMock;
    private readonly ICestaRecomendacaoRepository _cestaRepositoryMock;
    private readonly ICotacaoB3Provider _cotacaoProviderMock;
    private readonly IUnitOfWork _unitOfWorkMock;
    private readonly CalculoDesvioService _calculoDesvioService;
    private readonly RebalanceamentoPorDesvioUseCase _sut;

    public RebalanceamentoPorDesvioUseCaseTests()
    {
        _clienteRepositoryMock = Substitute.For<IClienteRepository>();
        _cestaRepositoryMock = Substitute.For<ICestaRecomendacaoRepository>();
        _cotacaoProviderMock = Substitute.For<ICotacaoB3Provider>();
        _unitOfWorkMock = Substitute.For<IUnitOfWork>();
        _calculoDesvioService = new CalculoDesvioService();

        _sut = new RebalanceamentoPorDesvioUseCase(
            _clienteRepositoryMock,
            _cestaRepositoryMock,
            _cotacaoProviderMock,
            _calculoDesvioService,
            _unitOfWorkMock
        );
    }

    [Fact]
    public async Task ExecutarRebalanceamentoPorDesvioAsync_SemCestaAtiva_DeveLancarInvalidOperationException()
    {
        _cestaRepositoryMock.ObterAtivaAsync().Returns((CestaRecomendacao?)null);

        var act = async () => await _sut.ExecutarRebalanceamentoPorDesvioAsync();

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*cesta ativa*");
    }

    [Fact]
    public async Task ExecutarRebalanceamentoPorDesvioAsync_CarteirasAlinhadas_NaoDeveRebalancear()
    {
        // Arrange
        var cesta = new CestaRecomendacao("Cesta", new List<ItemCesta>
        {
            new("PETR4", 20), new("VALE3", 20), new("ITUB4", 20),
            new("BBDC4", 20), new("WEGE3", 20)
        });
        _cestaRepositoryMock.ObterAtivaAsync().Returns(cesta);

        var cliente = new Cliente("Joao", "11122233344", "joao@email.com", 1000m);
        var contaCorrente = new ContaGrafica((long?)cliente.Id, "123", TipoContaGrafica.Filhote);
        typeof(Cliente).GetProperty("ContaGrafica")!.SetValue(cliente, contaCorrente);

        // Carteira alinhada: cada ação = R$ 100 = 20% de R$ 500
        var custodias = new List<Custodia>
        {
            new(contaCorrente.Id, "PETR4", 10, 10m),
            new(contaCorrente.Id, "VALE3", 5, 20m),
            new(contaCorrente.Id, "ITUB4", 4, 25m),
            new(contaCorrente.Id, "BBDC4", 2, 50m),
            new(contaCorrente.Id, "WEGE3", 10, 10m)
        };
        typeof(ContaGrafica).GetField("_custodias", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
            .SetValue(contaCorrente, custodias);

        _clienteRepositoryMock.ObterClientesAtivosComCustodiaAsync().Returns(new List<Cliente> { cliente });
        _cotacaoProviderMock.ObterCotacoesDeFechamento().Returns(new List<CotacaoDto>
        {
            new() { Ticker = "PETR4", PrecoFechamento = 10m },
            new() { Ticker = "VALE3", PrecoFechamento = 20m },
            new() { Ticker = "ITUB4", PrecoFechamento = 25m },
            new() { Ticker = "BBDC4", PrecoFechamento = 50m },
            new() { Ticker = "WEGE3", PrecoFechamento = 10m }
        });

        // Act
        var result = await _sut.ExecutarRebalanceamentoPorDesvioAsync();

        // Assert
        result.ClientesAnalisados.Should().Be(1);
        result.ClientesRebalanceados.Should().Be(0);
        result.TotalVendas.Should().Be(0);
        result.TotalCompras.Should().Be(0);
        await _unitOfWorkMock.DidNotReceive().CommitAsync();
    }

    [Fact]
    public async Task ExecutarRebalanceamentoPorDesvioAsync_CarteiraDesalinhada_DeveRebalancear()
    {
        // Arrange
        var cesta = new CestaRecomendacao("Cesta", new List<ItemCesta>
        {
            new("PETR4", 20), new("VALE3", 20), new("ITUB4", 20),
            new("BBDC4", 20), new("WEGE3", 20)
        });
        _cestaRepositoryMock.ObterAtivaAsync().Returns(cesta);

        var cliente = new Cliente("Maria", "99988877766", "maria@email.com", 1000m);
        var contaCorrente = new ContaGrafica((long?)cliente.Id, "456", TipoContaGrafica.Filhote);
        typeof(Cliente).GetProperty("ContaGrafica")!.SetValue(cliente, contaCorrente);

        // Total = R$ 1000
        // PETR4 = 10 x 40 = 400 = 40% (alvo 20%, desvio +20pp) -> vender até 5 ações
        // VALE3 = 5 x 40 = 200 = 20% (ok)
        // ITUB4 = 4 x 50 = 200 = 20% (ok)
        // BBDC4 = 2 x 50 = 100 = 10% (alvo 20%, desvio -10pp) -> comprar 2 ações
        // WEGE3 = 2 x 50 = 100 = 10% (alvo 20%, desvio -10pp) -> comprar 2 ações
        var custodias = new List<Custodia>
        {
            new(contaCorrente.Id, "PETR4", 10, 30m),
            new(contaCorrente.Id, "VALE3", 5, 30m),
            new(contaCorrente.Id, "ITUB4", 4, 40m),
            new(contaCorrente.Id, "BBDC4", 2, 40m),
            new(contaCorrente.Id, "WEGE3", 2, 40m)
        };
        typeof(ContaGrafica).GetField("_custodias", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
            .SetValue(contaCorrente, custodias);

        _clienteRepositoryMock.ObterClientesAtivosComCustodiaAsync().Returns(new List<Cliente> { cliente });
        _cotacaoProviderMock.ObterCotacoesDeFechamento().Returns(new List<CotacaoDto>
        {
            new() { Ticker = "PETR4", PrecoFechamento = 40m },
            new() { Ticker = "VALE3", PrecoFechamento = 40m },
            new() { Ticker = "ITUB4", PrecoFechamento = 50m },
            new() { Ticker = "BBDC4", PrecoFechamento = 50m },
            new() { Ticker = "WEGE3", PrecoFechamento = 50m }
        });

        // Act
        var result = await _sut.ExecutarRebalanceamentoPorDesvioAsync();

        // Assert
        result.ClientesAnalisados.Should().Be(1);
        result.ClientesRebalanceados.Should().Be(1);

        // PETR4: alvo 20% de 1000 = 200/40 = 5. Tem 10, vende 5.
        cliente.ContaGrafica.Custodias.First(c => c.Ticker == "PETR4").Quantidade.Should().Be(5);
        // BBDC4: alvo 20% de 1000 = 200/50 = 4. Tem 2, compra 2.
        cliente.ContaGrafica.Custodias.First(c => c.Ticker == "BBDC4").Quantidade.Should().Be(4);
        // WEGE3: alvo 20% de 1000 = 200/50 = 4. Tem 2, compra 2.
        cliente.ContaGrafica.Custodias.First(c => c.Ticker == "WEGE3").Quantidade.Should().Be(4);

        result.TotalVendas.Should().Be(5);
        result.TotalCompras.Should().Be(4); // 2 BBDC4 + 2 WEGE3

        await _unitOfWorkMock.Received(1).CommitAsync();
    }
}
