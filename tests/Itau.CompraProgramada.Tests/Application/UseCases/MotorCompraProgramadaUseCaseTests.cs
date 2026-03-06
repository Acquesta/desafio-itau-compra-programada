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
    private readonly IOrdemCompraRepository _ordemCompraRepositoryMock;
    private readonly IEventoIRPublisher _eventoIRPublisherMock;
    private readonly IUnitOfWork _unitOfWorkMock;
    private readonly MotorCompraProgramadaUseCase _sut;

    public MotorCompraProgramadaUseCaseTests()
    {
        _clienteRepositoryMock = Substitute.For<IClienteRepository>();
        _cestaRepositoryMock = Substitute.For<ICestaRecomendacaoRepository>();
        _cotacaoProviderMock = Substitute.For<ICotacaoB3Provider>();
        _ordemCompraRepositoryMock = Substitute.For<IOrdemCompraRepository>();
        _eventoIRPublisherMock = Substitute.For<IEventoIRPublisher>();
        _unitOfWorkMock = Substitute.For<IUnitOfWork>();

        var calculadora = new CalculadoraLoteFracionarioService();
        var distribuicao = new DistribuicaoProporcionalService();
        var dataCompra = new DataCompraService();

        _sut = new MotorCompraProgramadaUseCase(
            _clienteRepositoryMock,
            _cestaRepositoryMock,
            _cotacaoProviderMock,
            _ordemCompraRepositoryMock,
            _eventoIRPublisherMock,
            _unitOfWorkMock,
            calculadora,
            distribuicao,
            dataCompra);
    }

    [Fact]
    public async Task ExecutarComprasAsync_DataNaoEhDiaDeCompra_DeveLancarInvalidOperationException()
    {
        // Arrange
        // Dia 10 não é dia de compra
        var dataReferencia = new DateTime(2023, 10, 10);

        // Act
        var act = async () => await _sut.ExecutarComprasAsync(dataReferencia);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*não é um dia válido*");
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
        result.ClientesProcessados.Should().Be(1);
        result.OrdensExecutadas.Should().NotBeEmpty();
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

        var master = new Cliente("Master", "00000000000", "master@banco.com", 100m);
        master.VincularContaGrafica(new ContaGrafica(null, "MST-001", TipoContaGrafica.Master));

        _cestaRepositoryMock.ObterAtivaAsync().Returns(cesta);
        _clienteRepositoryMock.ObterClientesAtivosComCustodiaAsync().Returns(new List<Cliente> { cliente });
        _clienteRepositoryMock.ObterClienteMasterAsync().Returns(master);
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

        var master = new Cliente("Master", "00000000000", "master@banco.com", 100m);
        master.VincularContaGrafica(new ContaGrafica(null, "MST-001", TipoContaGrafica.Master));

        _cestaRepositoryMock.ObterAtivaAsync().Returns(cesta);
        _clienteRepositoryMock.ObterClientesAtivosComCustodiaAsync().Returns(new List<Cliente> { cliente });
        _clienteRepositoryMock.ObterClienteMasterAsync().Returns(master);
        
        _cotacaoProviderMock.ObterCotacoesDeFechamento().Returns(new List<CotacaoDto>
        {
            new() { Ticker = "PETR4", PrecoFechamento = 10m },
            new() { Ticker = "VALE3", PrecoFechamento = 10m },
            new() { Ticker = "ITUB4", PrecoFechamento = 10m },
            new() { Ticker = "BBDC4", PrecoFechamento = 10m },
            new() { Ticker = "ABEV3", PrecoFechamento = 10m }
        });
    }

    [Fact]
    public async Task ExecutarComprasAsync_Ren039_DevolverResiduosParaContaMaster()
    {
        // Assemble
        var dataReferencia = new DateTime(2023, 10, 5); // Quinta-feira válida
        
        var master = new Cliente("Master", "00000000000", "master@banco.com", 100m);
        master.VincularContaGrafica(new ContaGrafica(null, "MST-001", TipoContaGrafica.Master));

        // Cliente aporte = 100. 1/3 = 33,33.
        var cliente1 = new Cliente("Cliente 1", "12345678901", "a@a.com", 100m);
        cliente1.VincularContaGrafica(new ContaGrafica(null, "FFF-001", TipoContaGrafica.Filhote));

        var cesta = new CestaRecomendacao("Cesta", new List<ItemCesta> 
        { 
            new("PETR4", 90m),
            new("VALE3", 2.5m),
            new("ITUB4", 2.5m),
            new("BBDC4", 2.5m),
            new("WEGE3", 2.5m)
        });
        
        // Cotacao = 10,00. 33,33 / 10 = 3 cotas a comprar no mercado
        // Total = 3 cotas.
        // O cliente deveria receber as 3 cotas. Vamos criar 2 clientes e limitar o saldo disponível.
        
        var cliente2 = new Cliente("Cliente 2", "22222222222", "b@b.com", 200m);
        cliente2.VincularContaGrafica(new ContaGrafica(null, "FFF-002", TipoContaGrafica.Filhote));
        
        // Atribuir IDs diferentes para evitar o erro "Item with same key has already been added" no ToDictionary
        typeof(Cliente).GetProperty("Id")!.SetValue(cliente1, 1L);
        typeof(Cliente).GetProperty("Id")!.SetValue(cliente2, 2L);
        
        // C1 = 100 (1/3 = 33,33)
        // C2 = 200 (1/3 = 66,67)
        // Total Aporte = 100,00
        // Montante C1 = 33,33 (3 cotas)
        // Montante C2 = 66,67 (6 cotas)
        // Mercado comprar = 10 cotas
        // Se a divisão deixar algo, so sobra pro Master
        
        _cestaRepositoryMock.ObterAtivaAsync().Returns(cesta);
        _clienteRepositoryMock.ObterClientesAtivosComCustodiaAsync().Returns(new List<Cliente> { cliente1, cliente2 });
        _clienteRepositoryMock.ObterClienteMasterAsync().Returns(master);
        
        // Cotacao a 25.00
        // C1 = 33,33 = 1 cota (usa 25, sobra 8,33 virtual)
        // C2 = 66,67 = 2 cotas (usa 50, sobra 16,67 virtual)
        // Total Aportes = 100,00 -> Mercado comprar = 100 / 25 = 4 cotas.
        // C1 recebe 1, C2 recebe 2. (Total 3 distribuído)
        // O resíduo (4 mercado - 3 distribuído) = 1 cota. 
        // 1 Cota deve sobrar pro Master.
        var cotacoes = new List<CotacaoDto> 
        { 
            new() { Ticker = "PETR4", PrecoFechamento = 25m },
            new() { Ticker = "VALE3", PrecoFechamento = 25m },
            new() { Ticker = "ITUB4", PrecoFechamento = 25m },
            new() { Ticker = "BBDC4", PrecoFechamento = 25m },
            new() { Ticker = "WEGE3", PrecoFechamento = 25m }
        };
        _cotacaoProviderMock.ObterCotacoesDeFechamento().Returns(cotacoes);

        // Act
        await _sut.ExecutarComprasAsync(dataReferencia);

        // Assert
        var custodiaMaster = master.ContaGrafica.Custodias.First(c => c.Ticker == "PETR4");
        custodiaMaster.Quantidade.Should().Be(1);
    }
}
