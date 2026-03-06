using FluentAssertions;
using Itau.CompraProgramada.Domain.Entities;

namespace Itau.CompraProgramada.Tests.Domain.Entities;

public class CestaRecomendacaoTests
{
    // === CONSTRUTOR ===

    [Fact]
    public void Construtor_Com5ItensE100Porcento_DeveCriarCestaAtiva()
    {
        // Arrange & Act (RN-014 + RN-015)
        var cesta = CriarCestaValida();

        // Assert
        cesta.Nome.Should().Be("Top Five");
        cesta.Ativa.Should().BeTrue();
        cesta.Itens.Should().HaveCount(5);
    }

    [Fact]
    public void Construtor_ComMenosDe5Itens_DeveLancarArgumentException()
    {
        // Arrange (RN-014)
        var itens = new List<ItemCesta>
        {
            new("PETR4", 50m),
            new("VALE3", 50m)
        };

        // Act & Assert
        var act = () => new CestaRecomendacao("Inválida", itens);
        act.Should().Throw<ArgumentException>()
           .WithMessage("*5*");
    }

    [Fact]
    public void Construtor_ComSomaDiferenteDe100_DeveLancarArgumentException()
    {
        // Arrange (RN-015)
        var itens = new List<ItemCesta>
        {
            new("PETR4", 30m), new("VALE3", 20m), new("ITUB4", 20m),
            new("BBDC4", 15m), new("WEGE3", 10m) // Soma = 95%
        };

        // Act & Assert
        var act = () => new CestaRecomendacao("Inválida", itens);
        act.Should().Throw<ArgumentException>()
           .WithMessage("*100%*");
    }

    // === DESATIVAR ===

    [Fact]
    public void Desativar_CestaAtiva_DeveDesativarComData()
    {
        // Arrange (RN-017)
        var cesta = CriarCestaValida();

        // Act
        cesta.Desativar();

        // Assert
        cesta.Ativa.Should().BeFalse();
    }

    // === HELPERS ===

    private static CestaRecomendacao CriarCestaValida()
    {
        var itens = new List<ItemCesta>
        {
            new("PETR4", 20m), new("VALE3", 20m), new("ITUB4", 20m),
            new("BBDC4", 20m), new("WEGE3", 20m)
        };
        return new CestaRecomendacao("Top Five", itens);
    }
}
