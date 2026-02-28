using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Itau.CompraProgramada.Application.UseCases;
using Itau.CompraProgramada.Domain.Entities;
using Itau.CompraProgramada.Domain.Enums;
using Itau.CompraProgramada.Domain.Interfaces;
using NSubstitute;
using Xunit;

namespace Itau.CompraProgramada.Tests.Application.UseCases;

public class RentabilidadeUseCaseTests
{
    private readonly IClienteRepository _clienteRepositoryMock;
    private readonly ICotacaoB3Provider _cotacaoProviderMock;
    private readonly RentabilidadeUseCase _sut;

    public RentabilidadeUseCaseTests()
    {
        _clienteRepositoryMock = Substitute.For<IClienteRepository>();
        _cotacaoProviderMock = Substitute.For<ICotacaoB3Provider>();
        _sut = new RentabilidadeUseCase(_clienteRepositoryMock, _cotacaoProviderMock);
    }

    [Fact]
    public async Task ObterRentabilidadeAsync_ClienteNaoExiste_DeveLancarKeyNotFoundException()
    {
        _clienteRepositoryMock.ObterPorIdAsync(99).Returns((Cliente?)null);

        var act = async () => await _sut.ObterRentabilidadeAsync(99);

        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task ObterRentabilidadeAsync_ClienteComCarteira_DeveRetornarRentabilidadeCorreta()
    {
        // Arrange — Exemplo do spec:
        // Valor Investido: R$ 6000, Valor Atual: R$ 6450, P/L: +R$ 450, Rent: +7,50%
        var cliente = new Cliente("Cliente A", "11122233344", "a@email.com", 3000m);
        var conta = new ContaGrafica((long?)cliente.Id, "001", TipoContaGrafica.Filhote);
        typeof(Cliente).GetProperty("ContaGrafica")!.SetValue(cliente, conta);

        var custodias = new List<Custodia>
        {
            new(conta.Id, "PETR4", 24, 35.50m),
            new(conta.Id, "VALE3", 12, 60.00m),
            new(conta.Id, "ITUB4", 18, 29.00m),
            new(conta.Id, "BBDC4", 30, 14.50m),
            new(conta.Id, "WEGE3", 6, 38.00m)
        };
        typeof(ContaGrafica).GetField("_custodias", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
            .SetValue(conta, custodias);

        _clienteRepositoryMock.ObterPorIdAsync(cliente.Id).Returns(cliente);
        _cotacaoProviderMock.ObterCotacoesDeFechamento().Returns(new List<CotacaoDto>
        {
            new() { Ticker = "PETR4", PrecoFechamento = 37.00m },
            new() { Ticker = "VALE3", PrecoFechamento = 65.00m },
            new() { Ticker = "ITUB4", PrecoFechamento = 31.00m },
            new() { Ticker = "BBDC4", PrecoFechamento = 15.50m },
            new() { Ticker = "WEGE3", PrecoFechamento = 42.00m }
        });

        // Act
        var result = await _sut.ObterRentabilidadeAsync(cliente.Id);

        // Assert — RN-063: Saldo total
        // PETR4: 24x37 = 888, VALE3: 12x65 = 780, ITUB4: 18x31 = 558, BBDC4: 30x15.5 = 465, WEGE3: 6x42 = 252
        // Total Atual = 2943
        result.ValorAtualTotal.Should().Be(2943m);

        // Investido: PETR4: 24x35.5 = 852, VALE3: 12x60 = 720, ITUB4: 18x29 = 522, BBDC4: 30x14.5 = 435, WEGE3: 6x38 = 228
        // Total Investido = 2757
        result.ValorInvestidoTotal.Should().Be(2757m);

        // RN-065: P/L total = 2943 - 2757 = 186
        result.PLTotal.Should().Be(186m);

        // RN-066: Rentabilidade = ((2943 - 2757) / 2757) * 100 = 6.75%
        result.RentabilidadePercentual.Should().Be(6.75m);

        // RN-064, RN-067, RN-068, RN-069: Detalhes por ativo
        result.Ativos.Should().HaveCount(5);

        var petr4 = result.Ativos.First(a => a.Ticker == "PETR4");
        petr4.Quantidade.Should().Be(24);
        petr4.PrecoMedio.Should().Be(35.50m);
        petr4.CotacaoAtual.Should().Be(37.00m);
        petr4.ValorAtual.Should().Be(888m);
        petr4.PL.Should().Be(36m); // (37 - 35.5) * 24

        // RN-070: Composição percentual
        petr4.ComposicaoPercentual.Should().BeApproximately(30.17m, 0.01m); // 888/2943 * 100
    }

    [Fact]
    public async Task ObterRentabilidadeAsync_ClienteComPrejuizo_DeveRetornarNegativo()
    {
        var cliente = new Cliente("Cliente B", "99988877766", "b@email.com", 1000m);
        var conta = new ContaGrafica((long?)cliente.Id, "002", TipoContaGrafica.Filhote);
        typeof(Cliente).GetProperty("ContaGrafica")!.SetValue(cliente, conta);

        var custodias = new List<Custodia>
        {
            new(conta.Id, "PETR4", 10, 40.00m) // Comprou a R$ 40, agora R$ 30
        };
        typeof(ContaGrafica).GetField("_custodias", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
            .SetValue(conta, custodias);

        _clienteRepositoryMock.ObterPorIdAsync(cliente.Id).Returns(cliente);
        _cotacaoProviderMock.ObterCotacoesDeFechamento().Returns(new List<CotacaoDto>
        {
            new() { Ticker = "PETR4", PrecoFechamento = 30.00m }
        });

        var result = await _sut.ObterRentabilidadeAsync(cliente.Id);

        result.PLTotal.Should().Be(-100m); // (30 - 40) * 10
        result.RentabilidadePercentual.Should().Be(-25m); // ((300 - 400) / 400) * 100
    }
}
