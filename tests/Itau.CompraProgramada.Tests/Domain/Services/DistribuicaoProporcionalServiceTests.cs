using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Itau.CompraProgramada.Domain.Services;
using Xunit;

namespace Itau.CompraProgramada.Tests.Domain.Services;

public class DistribuicaoProporcionalServiceTests
{
    private readonly DistribuicaoProporcionalService _sut = new();

    [Fact]
    public void Deve_Distribuir_E_Calcular_Residuos_Exatamente_Como_O_Exemplo_Do_Itau()
    {
        // Arrange (Preparação conforme o PDF de regras)
        int totalPetr4Disponivel = 30; // 28 compradas + 2 de saldo na master
        
        var aportesClientes = new Dictionary<long, decimal>
        {
            { 1, 1000m }, // Cliente A: R$ 1.000
            { 2, 2000m }, // Cliente B: R$ 2.000
            { 3, 500m }   // Cliente C: R$ 500
        };

        // Act (Ação)
        var resultado = _sut.Distribuir(totalPetr4Disponivel, aportesClientes);

        // Assert (Validação)
        var distA = resultado.Distribuicoes.First(d => d.ClienteId == 1);
        var distB = resultado.Distribuicoes.First(d => d.ClienteId == 2);
        var distC = resultado.Distribuicoes.First(d => d.ClienteId == 3);

        // Validando se o truncamento bateu com as regras (RN-034 a RN-039)
        distA.Quantidade.Should().Be(8);
        distB.Quantidade.Should().Be(17);
        distC.Quantidade.Should().Be(4);

        // 8 + 17 + 4 = 29. Devem sobrar exatamente 1 ação na master.
        resultado.ResiduoMaster.Should().Be(1);
    }
}