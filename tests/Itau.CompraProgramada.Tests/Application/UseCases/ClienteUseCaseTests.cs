using FluentAssertions;
using Itau.CompraProgramada.Application.DTOs;
using Itau.CompraProgramada.Application.UseCases;
using Itau.CompraProgramada.Domain.Entities;
using Itau.CompraProgramada.Domain.Enums;
using Itau.CompraProgramada.Domain.Interfaces;
using NSubstitute;

namespace Itau.CompraProgramada.Tests.Application.UseCases;

public class ClienteUseCaseTests
{
    private readonly IClienteRepository _clienteRepositoryMock;
    private readonly IUnitOfWork _unitOfWorkMock;
    private readonly ClienteUseCase _sut; // System Under Test

    public ClienteUseCaseTests()
    {
        _clienteRepositoryMock = Substitute.For<IClienteRepository>();
        _unitOfWorkMock = Substitute.For<IUnitOfWork>();
        _sut = new ClienteUseCase(_clienteRepositoryMock, _unitOfWorkMock);
    }

    [Fact]
    public async Task AderirAsync_CpfJaExistente_DeveLancarInvalidOperationException()
    {
        // Arrange
        var request = new AdesaoRequest("Teste", "12345678901", "teste@email.com", 150m);
        _clienteRepositoryMock.ExisteCpfAsync(request.Cpf).Returns(true);

        // Act
        var act = async () => await _sut.AderirAsync(request);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
                 .WithMessage("CLIENTE_CPF_DUPLICADO");
                 
        await _clienteRepositoryMock.DidNotReceive().AdicionarAsync(Arg.Any<Cliente>());
        await _unitOfWorkMock.DidNotReceive().CommitAsync();
    }

    [Fact]
    public async Task AderirAsync_DadosValidos_DeveSalvarERetornarResponseComContaFilhote()
    {
        // Arrange
        var request = new AdesaoRequest("Teste", "12345678901", "teste@email.com", 150m);
        _clienteRepositoryMock.ExisteCpfAsync(request.Cpf).Returns(false);

        // Act
        var result = await _sut.AderirAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.Nome.Should().Be(request.Nome);
        result.ValorMensal.Should().Be(request.ValorMensal);
        result.Ativo.Should().BeTrue();
        result.ContaGrafica.Should().NotBeNull();
        result.ContaGrafica.Tipo.Should().Be("Filhote");
        result.ContaGrafica.NumeroConta.Should().StartWith("FLH-");

        await _clienteRepositoryMock.Received(1).AdicionarAsync(Arg.Is<Cliente>(c => c.Cpf == request.Cpf));
        await _unitOfWorkMock.Received(1).CommitAsync();
    }

    [Fact]
    public async Task SairAsync_ClienteNaoExiste_DeveLancarKeyNotFoundException()
    {
        // Arrange
        _clienteRepositoryMock.ObterPorIdAsync(99).Returns((Cliente?)null);

        // Act
        var act = async () => await _sut.SairAsync(99);

        // Assert
        await act.Should().ThrowAsync<KeyNotFoundException>()
                 .WithMessage("CLIENTE_NAO_ENCONTRADO");
    }

    [Fact]
    public async Task SairAsync_ClienteAtivo_DeveChamarSairDoProdutoESalvar()
    {
        // Arrange
        var cliente = new Cliente("Teste", "12345678901", "teste@email.com", 150m);
        _clienteRepositoryMock.ObterPorIdAsync(1).Returns(cliente);

        // Act
        await _sut.SairAsync(1);

        // Assert
        cliente.Ativo.Should().BeFalse();
        cliente.DataSaida.Should().NotBeNull();

        _clienteRepositoryMock.Received(1).Atualizar(cliente);
        await _unitOfWorkMock.Received(1).CommitAsync();
    }

    [Fact]
    public async Task AlterarValorMensalAsync_ClienteInativo_DeveLancarInvalidOperationException()
    {
        // Arrange
        var cliente = new Cliente("Teste", "12345678901", "teste@email.com", 150m);
        cliente.SairDoProduto();
        _clienteRepositoryMock.ObterPorIdAsync(1).Returns(cliente);
        var request = new AlterarValorRequest(200m);

        // Act
        var act = async () => await _sut.AlterarValorMensalAsync(1, request);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task AlterarValorMensalAsync_ValorValidoEClienteAtivo_DeveAtualizarValorESalvar()
    {
        // Arrange
        var cliente = new Cliente("Teste", "12345678901", "teste@email.com", 150m);
        _clienteRepositoryMock.ObterPorIdAsync(1).Returns(cliente);
        var request = new AlterarValorRequest(200m);

        // Act
        await _sut.AlterarValorMensalAsync(1, request);

        // Assert
        cliente.ValorMensal.Should().Be(200m);
        cliente.HistoricoValores.Should().HaveCount(1); // Foi criado hitórico

        _clienteRepositoryMock.Received(1).Atualizar(cliente);
        await _unitOfWorkMock.Received(1).CommitAsync();
    }

    [Fact]
    public async Task ObterCarteiraAsync_ClienteSemCustodia_DeveRetornarCarteiraVaziaMasComSaldoEValorMensal()
    {
        // Arrange
        var cliente = new Cliente("Teste", "12345678901", "teste@email.com", 150m);
        var conta = new ContaGrafica(null, "FLH-001", TipoContaGrafica.Filhote);
        cliente.VincularContaGrafica(conta);
        _clienteRepositoryMock.ObterPorIdComCustodiaAsync(1).Returns(cliente);

        // Act
        var result = await _sut.ObterCarteiraAsync(1);

        // Assert
        result.Should().NotBeNull();
        result.Nome.Should().Be(cliente.Nome);
        result.ValorMensal.Should().Be(150m);
        result.SaldoTotal.Should().Be(0);
        result.Ativos.Should().BeEmpty();
    }

    [Fact]
    public async Task ObterCarteiraAsync_ClienteComCustodia_DeveCalcularSaldoCorretamente()
    {
        // Arrange
        var cliente = new Cliente("Teste", "12345678901", "teste@email.com", 150m);
        var conta = new ContaGrafica(null, "FLH-001", TipoContaGrafica.Filhote);
        
        // Simular que o cliente comprou 10 PETR4 por 30.00
        var custodia = new Custodia(conta.Id, "PETR4", 10, 30.00m);
        
        // Usar reflection para adicionar na lista privada contornando o encapsulamento ou
        // como é teste a ContaGrafica não expõe setter, vamos instanciar e atribuir via reflection para o teste
        var custodiasField = typeof(ContaGrafica).GetField("_custodias", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var listaCustodias = new List<Custodia> { custodia };
        custodiasField?.SetValue(conta, listaCustodias);

        cliente.VincularContaGrafica(conta);
        _clienteRepositoryMock.ObterPorIdComCustodiaAsync(1).Returns(cliente);

        // Act
        var result = await _sut.ObterCarteiraAsync(1);

        // Assert
        result.SaldoTotal.Should().Be(300m); // 10 * 30.00
        result.Ativos.Should().HaveCount(1);
        result.Ativos.First().Ticker.Should().Be("PETR4");
    }
}
