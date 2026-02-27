using FluentAssertions;
using Itau.CompraProgramada.Domain.Entities;
using Itau.CompraProgramada.Domain.Enums;

namespace Itau.CompraProgramada.Tests.Domain.Entities;

public class ClienteTests
{
    // === CONSTRUTOR ===

    [Fact]
    public void Construtor_ComDadosValidos_DeveCriarClienteAtivoComDataAdesao()
    {
        // Act
        var cliente = new Cliente("João Silva", "12345678901", "joao@email.com", 1000m);

        // Assert
        cliente.Nome.Should().Be("João Silva");
        cliente.Cpf.Should().Be("12345678901");
        cliente.Email.Should().Be("joao@email.com");
        cliente.ValorMensal.Should().Be(1000m);
        cliente.Ativo.Should().BeTrue(); // RN-005
        cliente.DataAdesao.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5)); // RN-006
        cliente.DataSaida.Should().BeNull();
        cliente.HistoricoValores.Should().BeEmpty();
    }

    [Fact]
    public void Construtor_ComValorMenorQue100_DeveLancarArgumentException()
    {
        // Act & Assert (RN-003)
        var act = () => new Cliente("João", "12345678901", "joao@email.com", 99m);
        act.Should().Throw<ArgumentException>()
           .WithMessage("*valor mensal mínimo*");
    }

    [Fact]
    public void Construtor_ComValorIgualA100_DeveCriarCliente()
    {
        // Act (RN-003 - boundary)
        var cliente = new Cliente("João", "12345678901", "joao@email.com", 100m);

        // Assert
        cliente.ValorMensal.Should().Be(100m);
        cliente.Ativo.Should().BeTrue();
    }

    [Fact]
    public void Construtor_ComNomeVazio_DeveLancarArgumentException()
    {
        var act = () => new Cliente("", "12345678901", "joao@email.com", 1000m);
        act.Should().Throw<ArgumentException>()
           .WithMessage("*Nome*");
    }

    [Fact]
    public void Construtor_ComCpfInvalido_DeveLancarArgumentException()
    {
        var act = () => new Cliente("João", "123", "joao@email.com", 1000m);
        act.Should().Throw<ArgumentException>()
           .WithMessage("*CPF*11*");
    }

    [Fact]
    public void Construtor_ComEmailVazio_DeveLancarArgumentException()
    {
        var act = () => new Cliente("João", "12345678901", "", 1000m);
        act.Should().Throw<ArgumentException>()
           .WithMessage("*Email*");
    }

    // === ALTERAR VALOR MENSAL ===

    [Fact]
    public void AlterarValorMensal_ComValorValido_DeveAtualizarERegistrarHistorico()
    {
        // Arrange
        var cliente = new Cliente("João", "12345678901", "joao@email.com", 1000m);

        // Act (RN-011 + RN-013)
        cliente.AlterarValorMensal(2000m);

        // Assert
        cliente.ValorMensal.Should().Be(2000m);
        cliente.HistoricoValores.Should().HaveCount(1);
        cliente.HistoricoValores.First().ValorAnterior.Should().Be(1000m);
        cliente.HistoricoValores.First().ValorNovo.Should().Be(2000m);
    }

    [Fact]
    public void AlterarValorMensal_MultiplasVezes_DeveRegistrarTodoHistorico()
    {
        // Arrange
        var cliente = new Cliente("João", "12345678901", "joao@email.com", 1000m);

        // Act
        cliente.AlterarValorMensal(2000m);
        cliente.AlterarValorMensal(3000m);

        // Assert (RN-013)
        cliente.ValorMensal.Should().Be(3000m);
        cliente.HistoricoValores.Should().HaveCount(2);
        cliente.HistoricoValores.Last().ValorAnterior.Should().Be(2000m);
        cliente.HistoricoValores.Last().ValorNovo.Should().Be(3000m);
    }

    [Fact]
    public void AlterarValorMensal_ComValorAbaixoDoMinimo_DeveLancarArgumentException()
    {
        // Arrange
        var cliente = new Cliente("João", "12345678901", "joao@email.com", 1000m);

        // Act & Assert
        var act = () => cliente.AlterarValorMensal(50m);
        act.Should().Throw<ArgumentException>()
           .WithMessage("*valor mensal mínimo*");
        
        // Verifica que NÃO alterou
        cliente.ValorMensal.Should().Be(1000m);
        cliente.HistoricoValores.Should().BeEmpty();
    }

    [Fact]
    public void AlterarValorMensal_ClienteInativo_DeveLancarInvalidOperationException()
    {
        // Arrange
        var cliente = new Cliente("João", "12345678901", "joao@email.com", 1000m);
        cliente.SairDoProduto();

        // Act & Assert
        var act = () => cliente.AlterarValorMensal(2000m);
        act.Should().Throw<InvalidOperationException>()
           .WithMessage("*inativo*");
    }

    // === SAIR DO PRODUTO ===

    [Fact]
    public void SairDoProduto_ClienteAtivo_DeveDesativarERegistrarDataSaida()
    {
        // Arrange
        var cliente = new Cliente("João", "12345678901", "joao@email.com", 1000m);

        // Act (RN-007)
        cliente.SairDoProduto();

        // Assert
        cliente.Ativo.Should().BeFalse();
        cliente.DataSaida.Should().NotBeNull();
        cliente.DataSaida.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void SairDoProduto_ClienteJaInativo_DeveLancarInvalidOperationException()
    {
        // Arrange
        var cliente = new Cliente("João", "12345678901", "joao@email.com", 1000m);
        cliente.SairDoProduto();

        // Act & Assert
        var act = () => cliente.SairDoProduto();
        act.Should().Throw<InvalidOperationException>()
           .WithMessage("*já está inativo*");
    }

    // === VINCULAR CONTA GRAFICA ===

    [Fact]
    public void VincularContaGrafica_DeveAssociarConta()
    {
        // Arrange
        var cliente = new Cliente("João", "12345678901", "joao@email.com", 1000m);
        var conta = new ContaGrafica(null, "FLH-001", TipoContaGrafica.Filhote);

        // Act
        cliente.VincularContaGrafica(conta);

        // Assert
        cliente.ContaGrafica.Should().Be(conta);
    }
}
