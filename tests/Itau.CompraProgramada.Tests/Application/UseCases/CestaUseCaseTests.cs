using FluentAssertions;
using Itau.CompraProgramada.Application.DTOs;
using Itau.CompraProgramada.Application.UseCases;
using Itau.CompraProgramada.Domain.Entities;
using Itau.CompraProgramada.Domain.Interfaces;
using NSubstitute;

namespace Itau.CompraProgramada.Tests.Application.UseCases;

public class CestaUseCaseTests
{
    private readonly ICestaRecomendacaoRepository _cestaRepositoryMock;
    private readonly IUnitOfWork _unitOfWorkMock;
    private readonly CestaUseCase _sut;

    public CestaUseCaseTests()
    {
        _cestaRepositoryMock = Substitute.For<ICestaRecomendacaoRepository>();
        _unitOfWorkMock = Substitute.For<IUnitOfWork>();
        _sut = new CestaUseCase(_cestaRepositoryMock, _unitOfWorkMock);
    }

    [Fact]
    public async Task CriarCestaAsync_SemCestaAnterior_DeveSalvarNovaCesta()
    {
        // Arrange
        var request = CriarCestaRequestValida();
        _cestaRepositoryMock.ObterAtivaAsync().Returns((CestaRecomendacao?)null);

        // Act
        var result = await _sut.CriarCestaAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.Nome.Should().Be("Nova Cesta");
        result.Ativa.Should().BeTrue();
        result.Itens.Should().HaveCount(5);

        await _cestaRepositoryMock.Received(1).AdicionarAsync(Arg.Any<CestaRecomendacao>());
        _cestaRepositoryMock.DidNotReceive().Atualizar(Arg.Any<CestaRecomendacao>());
        await _unitOfWorkMock.Received(1).CommitAsync();
    }

    [Fact]
    public async Task CriarCestaAsync_ComCestaAnteriorAtiva_DeveDesativarAnteriorESalvarNova()
    {
        // Arrange
        var request = CriarCestaRequestValida();
        
        var cestaAntiga = new CestaRecomendacao("Antiga", new List<ItemCesta>
        {
            new("PETR4", 20m), new("VALE3", 20m), new("ITUB4", 20m),
            new("BBDC4", 20m), new("WEGE3", 20m)
        });
        
        _cestaRepositoryMock.ObterAtivaAsync().Returns(cestaAntiga);

        // Act
        await _sut.CriarCestaAsync(request);

        // Assert
        cestaAntiga.Ativa.Should().BeFalse();
        cestaAntiga.DataDesativacao.Should().NotBeNull();

        _cestaRepositoryMock.Received(1).Atualizar(cestaAntiga);
        await _cestaRepositoryMock.Received(1).AdicionarAsync(Arg.Is<CestaRecomendacao>(c => c.Nome == "Nova Cesta"));
        await _unitOfWorkMock.Received(1).CommitAsync();
    }

    [Fact]
    public async Task ObterCestaAtualAsync_SemCestaCadastrada_DeveRetornarNull()
    {
        // Arrange
        _cestaRepositoryMock.ObterAtivaAsync().Returns((CestaRecomendacao?)null);

        // Act
        var result = await _sut.ObterCestaAtualAsync();

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task ObterHistoricoCestasAsync_DeveRetornarListaMapeada()
    {
        // Arrange
        var cesta = new CestaRecomendacao("Cesta 1", new List<ItemCesta>
        {
            new("PETR4", 20m), new("VALE3", 20m), new("ITUB4", 20m),
            new("BBDC4", 20m), new("WEGE3", 20m)
        });
        _cestaRepositoryMock.ObterHistoricoAsync().Returns(new List<CestaRecomendacao> { cesta });

        // Act
        var result = await _sut.ObterHistoricoCestasAsync();

        // Assert
        result.Should().HaveCount(1);
        result.First().Nome.Should().Be("Cesta 1");
    }

    private static CestaRequest CriarCestaRequestValida()
    {
        return new CestaRequest("Nova Cesta", new List<CestaItemRequest>
        {
            new("PETR4", 20m),
            new("VALE3", 20m),
            new("ITUB4", 30m),
            new("BBDC4", 15m),
            new("WEGE3", 15m)
        });
    }
}
