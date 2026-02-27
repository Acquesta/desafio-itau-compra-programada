using FluentAssertions;
using Itau.CompraProgramada.Application.UseCases;
using Itau.CompraProgramada.Domain.Entities;
using Itau.CompraProgramada.Domain.Enums;
using Itau.CompraProgramada.Domain.Interfaces;
using Itau.CompraProgramada.Domain.Services;
using NSubstitute;

namespace Itau.CompraProgramada.Tests.Application.UseCases;

public class MotorCompraProgramadaUseCaseTests
{
    private readonly IClienteRepository _clienteRepositoryMock;
    private readonly ICestaRecomendacaoRepository _cestaRepositoryMock;
    private readonly ICotacaoB3Provider _cotacaoProviderMock;
    private readonly IEventoIRPublisher _eventoIRPublisherMock;
    private readonly IUnitOfWork _unitOfWorkMock;
    private readonly MotorCompraProgramadaUseCase _sut;

    public MotorCompraProgramadaUseCaseTests()
    {
        _clienteRepositoryMock = Substitute.For<IClienteRepository>();
        _cestaRepositoryMock = Substitute.For<ICestaRecomendacaoRepository>();
        _cotacaoProviderMock = Substitute.For<ICotacaoB3Provider>();
        _eventoIRPublisherMock = Substitute.For<IEventoIRPublisher>();
        _unitOfWorkMock = Substitute.For<IUnitOfWork>();

        var calculadora = new CalculadoraLoteFracionarioService();
        var distribuicao = new DistribuicaoProporcionalService();
        var dataCompra = new DataCompraService();

        _sut = new MotorCompraProgramadaUseCase(
            _clienteRepositoryMock,
            _cestaRepositoryMock,
            _cotacaoProviderMock,
            _eventoIRPublisherMock,
            _unitOfWorkMock,
            calculadora,
            distribuicao,
            dataCompra);
    }

    [Fact]
    public async Task ExecutarComprasAsync_DataNaoEhDiaDeCompra_DeveRetornarMensagemInvalida()
    {
        // Arrange
        // Dia 10 não é dia de compra
        var dataReferencia = new DateTime(2023, 10, 10);

        // Act
        var result = await _sut.ExecutarComprasAsync(dataReferencia);

        // Assert
        result.Should().Contain("não é um dia válido");
        await _unitOfWorkMock.DidNotReceive().CommitAsync();
    }

    [Fact]
    public async Task ExecutarComprasAsync_DataFimDeSemana5_TransfereParaProximoDiaUtil_DeveExecutarSucesso()
    {
        // Arrange
        // 5 de Agosto de 2023 foi Sábado. O ajuste vai para segunda-feira, 7 de Agosto.
        var dataReferencia = new DateTime(2023, 8, 7);
        
        ConfigurarMocksComSucesso();

        // Act
        var result = await _sut.ExecutarComprasAsync(dataReferencia);

        // Assert
        result.Should().Contain("Compra executada com sucesso");
        await _unitOfWorkMock.Received(1).CommitAsync();
    }

    [Fact]
    public async Task ExecutarComprasAsync_RN023_DeveRatearUsandoApenasUmTercoDoValorMensal()
    {
        // Arrange
        var dataReferencia = new DateTime(2023, 10, 5); // Quinta-feira válida
        
        var cliente = new Cliente("Teste Algoritmo", "12345678901", "teste@email.com", 3000m);
        cliente.VincularContaGrafica(new ContaGrafica(null, "FLH-999", TipoContaGrafica.Filhote));
        
        var cesta = new CestaRecomendacao("Cesta", new List<ItemCesta> 
        { 
            new("PETR4", 90m),
            new("VALE3", 2.5m),
            new("ITUB4", 2.5m),
            new("BBDC4", 2.5m),
            new("WEGE3", 2.5m)
        });
        
        // Cotacao PETR4 = 10,00. 
        // Valor Mensal = 3000. 1/3 do valor = 1000,00.
        // Qtd Compra = 1000 / 10 = 100 ações. 
        var cotacoes = new List<CotacaoDto> 
        { 
            new() { Ticker = "PETR4", PrecoFechamento = 10m },
            new() { Ticker = "VALE3", PrecoFechamento = 50m },
            new() { Ticker = "ITUB4", PrecoFechamento = 30m },
            new() { Ticker = "BBDC4", PrecoFechamento = 15m },
            new() { Ticker = "WEGE3", PrecoFechamento = 40m }
        };

        _cestaRepositoryMock.ObterAtivaAsync().Returns(cesta);
        _clienteRepositoryMock.ObterClientesAtivosComCustodiaAsync().Returns(new List<Cliente> { cliente });
        _cotacaoProviderMock.ObterCotacoesDeFechamento().Returns(cotacoes);

        // Act
        await _sut.ExecutarComprasAsync(dataReferencia);

        // Assert
        var custodia = cliente.ContaGrafica.Custodias.First(c => c.Ticker == "PETR4");
        
        // Confirmando que usou apenas 1000,00 (1/3 de 3000) e 90% disso é 900,00 
        // 900 / 10 = 90 ações (e não 300 ou 270)
        custodia.Quantidade.Should().Be(90);
    }

    private void ConfigurarMocksComSucesso()
    {
        var cliente = new Cliente("João", "12345678901", "a@a.com", 300m);
        cliente.VincularContaGrafica(new ContaGrafica(null, "FFF", TipoContaGrafica.Filhote));
        
        var cesta = new CestaRecomendacao("Ativa", new List<ItemCesta>
        {
            new("PETR4", 20m), new("VALE3", 20m), new("ITUB4", 20m), new("BBDC4", 20m), new("ABEV3", 20m)
        });

        _cestaRepositoryMock.ObterAtivaAsync().Returns(cesta);
        _clienteRepositoryMock.ObterClientesAtivosComCustodiaAsync().Returns(new List<Cliente> { cliente });
        
        _cotacaoProviderMock.ObterCotacoesDeFechamento().Returns(new List<CotacaoDto>
        {
            new() { Ticker = "PETR4", PrecoFechamento = 10m },
            new() { Ticker = "VALE3", PrecoFechamento = 10m },
            new() { Ticker = "ITUB4", PrecoFechamento = 10m },
            new() { Ticker = "BBDC4", PrecoFechamento = 10m },
            new() { Ticker = "ABEV3", PrecoFechamento = 10m }
        });
    }
}
