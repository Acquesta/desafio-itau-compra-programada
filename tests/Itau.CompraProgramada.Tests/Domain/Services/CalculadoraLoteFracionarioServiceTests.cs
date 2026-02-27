using FluentAssertions;
using Itau.CompraProgramada.Domain.Services;
using Xunit;

namespace Itau.CompraProgramada.Tests.Domain.Services;

public class CalculadoraLoteFracionarioServiceTests
{
    private readonly CalculadoraLoteFracionarioService _sut = new(); // SUT = System Under Test

    [Theory]
    [InlineData("PETR4", 350, "PETR4", 300, "PETR4F", 50)] // Cenário Misto
    [InlineData("VALE3", 100, "VALE3", 100, "VALE3F", 0)]  // Apenas Lote Padrão
    [InlineData("ITUB4", 45, "ITUB4", 0, "ITUB4F", 45)]    // Apenas Fracionário
    public void Deve_Separar_Quantidade_Corretamente(
        string tickerPadrao, int qtdTotal, 
        string tickerLoteEsperado, int qtdLoteEsperada, 
        string tickerFracEsperado, int qtdFracEsperada)
    {
        // Act (Ação)
        var resultado = _sut.Calcular(tickerPadrao, qtdTotal);

        // Assert (Validação)
        resultado.TickerLote.Should().Be(tickerLoteEsperado);
        resultado.QtdLote.Should().Be(qtdLoteEsperada);
        resultado.TickerFracionario.Should().Be(tickerFracEsperado);
        resultado.QtdFracionaria.Should().Be(qtdFracEsperada);
    }
}