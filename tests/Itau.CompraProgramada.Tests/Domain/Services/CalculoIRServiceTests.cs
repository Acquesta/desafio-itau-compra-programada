using System;
using FluentAssertions;
using Itau.CompraProgramada.Domain.Services;
using Xunit;

namespace Itau.CompraProgramada.Tests.Domain.Services;

public class CalculoIRServiceTests
{
    private readonly CalculoIRService _sut = new();

    // ============================================================
    // RN-053: IR Dedo-Duro = 0,005% sobre o valor da operação
    // ============================================================

    [Fact]
    public void CalcularDedoDuro_ValorNormal_DeveRetornar0005Porcento()
    {
        // R$ 280 x 0,005% = R$ 0,014 => R$ 0,01
        var result = _sut.CalcularDedoDuro(280m);
        result.Should().Be(0.01m);
    }

    [Fact]
    public void CalcularDedoDuro_ValorAlto_DeveArredondarCorretamente()
    {
        // R$ 100.000 x 0,005% = R$ 5,00
        var result = _sut.CalcularDedoDuro(100_000m);
        result.Should().Be(5.00m);
    }

    [Fact]
    public void CalcularDedoDuro_ValorZero_DeveRetornarZero()
    {
        var result = _sut.CalcularDedoDuro(0m);
        result.Should().Be(0m);
    }

    // ============================================================
    // RN-057 a RN-061: IR sobre Vendas (Rebalanceamento)
    // ============================================================

    [Fact]
    public void CalcularIRSobreVendas_VendasAbaixoDe20k_DeveRetornarZero()
    {
        // Exemplo 1: R$ 230 < R$ 20.000 => ISENTO
        var result = _sut.CalcularIRSobreVendas(230m, 50m);
        result.Should().Be(0m);
    }

    [Fact]
    public void CalcularIRSobreVendas_VendasAcimaDe20k_ComLucro_Deve20PorcentoSobreLucro()
    {
        // Exemplo 2: Vendas R$ 21.500, Lucro R$ 3.100 => IR = R$ 620
        var result = _sut.CalcularIRSobreVendas(21_500m, 3_100m);
        result.Should().Be(620m);
    }

    [Fact]
    public void CalcularIRSobreVendas_VendasAcimaDe20k_ComPrejuizo_DeveRetornarZero()
    {
        // Exemplo 3: Vendas R$ 24.400, Lucro -R$ 600 => IR = R$ 0
        var result = _sut.CalcularIRSobreVendas(24_400m, -600m);
        result.Should().Be(0m);
    }

    [Fact]
    public void CalcularIRSobreVendas_VendasExatamente20k_DeveSerIsento()
    {
        var result = _sut.CalcularIRSobreVendas(20_000m, 500m);
        result.Should().Be(0m);
    }

    [Fact]
    public void CalcularIRSobreVendas_VendasAcimaDe20k_LucroZero_DeveRetornarZero()
    {
        var result = _sut.CalcularIRSobreVendas(25_000m, 0m);
        result.Should().Be(0m);
    }
}
