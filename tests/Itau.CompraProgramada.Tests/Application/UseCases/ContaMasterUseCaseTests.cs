using FluentAssertions;
using Itau.CompraProgramada.Application.UseCases;
using Itau.CompraProgramada.Domain.Entities;
using Itau.CompraProgramada.Domain.Enums;
using Itau.CompraProgramada.Domain.Interfaces;
using NSubstitute;

namespace Itau.CompraProgramada.Tests.Application.UseCases;

public class ContaMasterUseCaseTests
{
    private readonly IClienteRepository _clienteRepositoryMock;
    private readonly ContaMasterUseCase _sut;

    public ContaMasterUseCaseTests()
    {
        _clienteRepositoryMock = Substitute.For<IClienteRepository>();
        _sut = new ContaMasterUseCase(_clienteRepositoryMock);
    }

    [Fact]
    public async Task ObterCustodiaMasterAsync_QuandoNaoExisteMaster_DeveLancarKeyNotFoundException()
    {
        // Arrange
        _clienteRepositoryMock.ObterClienteMasterAsync().Returns((Cliente?)null);

        // Act
        var act = async () => await _sut.ObterCustodiaMasterAsync();

        // Assert
        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage("A Conta Gráfica Master não foi encontrada no sistema.");
    }

    [Fact]
    public async Task ObterCustodiaMasterAsync_QuandoExisteMaster_DeveRetornarCustoCorretoETickers()
    {
        // Arrange
        var master = new Cliente("Master", "00000000000", "master@banco.com", 100m);
        var contaGrafica = new ContaGrafica(null, "MST-001", TipoContaGrafica.Master);
        master.VincularContaGrafica(contaGrafica);

        contaGrafica.AdicionarCustodia(new Custodia(contaGrafica.Id, "PETR4", 5, 20m));
        contaGrafica.AdicionarCustodia(new Custodia(contaGrafica.Id, "VALE3", 2, 50m));

        _clienteRepositoryMock.ObterClienteMasterAsync().Returns(master);

        // Act
        var result = await _sut.ObterCustodiaMasterAsync();

        // Assert
        result.Should().NotBeNull();
        result.Nome.Should().Be("Master");
        
        // 5 * 20 + 2 * 50 = 100 + 100 = 200
        result.SaldoTotal.Should().Be(200m);
        
        result.Ativos.Should().HaveCount(2);
        
        var petr = result.Ativos.First(a => a.Ticker == "PETR4");
        petr.Quantidade.Should().Be(5);
        petr.PrecoMedio.Should().Be(20m);
        petr.ValorAtual.Should().Be(100m);
    }
}
