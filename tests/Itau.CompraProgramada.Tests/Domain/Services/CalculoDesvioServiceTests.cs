using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Itau.CompraProgramada.Domain.Entities;
using Itau.CompraProgramada.Domain.Interfaces;
using Itau.CompraProgramada.Domain.Services;
using Xunit;

namespace Itau.CompraProgramada.Tests.Domain.Services;

public class CalculoDesvioServiceTests
{
    private readonly CalculoDesvioService _sut = new();

    [Fact]
    public void CalcularDesvios_CarteiraAlinhada_DeveRetornarVazio()
    {
        // Arrange — carteira perfeitamente alinhada à cesta (20% cada)
        var cesta = new CestaRecomendacao("Cesta", new List<ItemCesta>
        {
            new("PETR4", 20), new("VALE3", 20), new("ITUB4", 20),
            new("BBDC4", 20), new("WEGE3", 20)
        });

        // Total = R$ 500, cada um R$ 100 = 20%
        var custodias = new List<Custodia>
        {
            new(1, "PETR4", 10, 10m),
            new(1, "VALE3", 5, 20m),
            new(1, "ITUB4", 4, 25m),
            new(1, "BBDC4", 2, 50m),
            new(1, "WEGE3", 10, 10m)
        };

        var cotacoes = new Dictionary<string, decimal>
        {
            { "PETR4", 10m }, { "VALE3", 20m }, { "ITUB4", 25m },
            { "BBDC4", 50m }, { "WEGE3", 10m }
        };

        // Act
        var result = _sut.CalcularDesvios(custodias.AsReadOnly(), cesta, cotacoes, 5m);

        // Assert — nenhum desvio >= 5pp
        result.Should().BeEmpty();
    }

    [Fact]
    public void CalcularDesvios_CarteiraDesalinhada_DeveRetornarDesvios()
    {
        // Arrange — carteira com desvio significativo
        var cesta = new CestaRecomendacao("Cesta", new List<ItemCesta>
        {
            new("PETR4", 20), new("VALE3", 20), new("ITUB4", 20),
            new("BBDC4", 20), new("WEGE3", 20)
        });

        // Total = R$ 1000
        // PETR4 = 400 => 40% (alvo 20%, desvio +20)
        // VALE3 = 200 => 20% (ok)
        // ITUB4 = 200 => 20% (ok)
        // BBDC4 = 100 => 10% (alvo 20%, desvio -10)
        // WEGE3 = 100 => 10% (alvo 20%, desvio -10)
        var custodias = new List<Custodia>
        {
            new(1, "PETR4", 10, 30m),
            new(1, "VALE3", 5, 30m),
            new(1, "ITUB4", 4, 40m),
            new(1, "BBDC4", 2, 40m),
            new(1, "WEGE3", 2, 40m)
        };

        var cotacoes = new Dictionary<string, decimal>
        {
            { "PETR4", 40m }, { "VALE3", 40m }, { "ITUB4", 50m },
            { "BBDC4", 50m }, { "WEGE3", 50m }
        };

        // Act
        var result = _sut.CalcularDesvios(custodias.AsReadOnly(), cesta, cotacoes, 5m);

        // Assert
        result.Should().NotBeEmpty();
        result.Should().Contain(d => d.Ticker == "PETR4" && d.Diferenca > 0); // sobre-alocado
        result.Should().Contain(d => d.Ticker == "BBDC4" && d.Diferenca < 0); // sub-alocado
        result.Should().Contain(d => d.Ticker == "WEGE3" && d.Diferenca < 0); // sub-alocado
    }

    [Fact]
    public void CalcularDesvios_CustodiaVazia_DeveRetornarVazio()
    {
        var cesta = new CestaRecomendacao("Cesta", new List<ItemCesta>
        {
            new("PETR4", 20), new("VALE3", 20), new("ITUB4", 20),
            new("BBDC4", 20), new("WEGE3", 20)
        });

        var result = _sut.CalcularDesvios(
            new List<Custodia>().AsReadOnly(), cesta,
            new Dictionary<string, decimal>(), 5m);

        result.Should().BeEmpty();
    }
}
