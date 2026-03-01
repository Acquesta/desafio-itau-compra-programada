using System;
using System.Threading.Tasks;
using Itau.CompraProgramada.Application.UseCases;
using Microsoft.AspNetCore.Mvc;

namespace Itau.CompraProgramada.Api.Controllers;

[ApiController]
[Route("api/motor")]
public class ComprasController : ControllerBase
{
    private readonly IMotorCompraProgramadaUseCase _motorCompra;

    public ComprasController(IMotorCompraProgramadaUseCase motorCompra)
    {
        _motorCompra = motorCompra;
    }

    /// <summary>
    /// Executa o Motor de Compra Programada para todos os clientes ativos.
    /// </summary>
    /// <param name="dataReferencia">Data de referência para a execução da compra (por padrão hoje)</param>
    [HttpPost("executar-compra")]
    public async Task<IActionResult> ExecutarCompraProgramada([FromQuery] DateTime? dataReferencia)
    {
        try
        {
            var data = dataReferencia ?? DateTime.Today;
            var resultado = await _motorCompra.ExecutarComprasAsync(data);
            
            return Ok(resultado);
        }
        catch (InvalidOperationException ex)
        {
            // Erros de regra de negócio (Ex: Cesta não encontrada, Ficheiro sem os tickers)
            return BadRequest(new { Erro = ex.Message });
        }
        catch (Exception ex)
        {
            // Erros não previstos
            return StatusCode(500, new { Erro = "Ocorreu um erro interno no servidor.", Detalhe = ex.Message });
        }
    }

    /// <summary>
    /// Gera um arquivo COTAHIST mockado no disco para facilitar o teste.
    /// </summary>
    [HttpGet("gerar-mock-b3")]
    public IActionResult GerarMockB3()
    {
        var caminho = System.IO.Path.Combine(System.IO.Directory.GetCurrentDirectory(), "COTAHIST_MOCK.TXT");
        var linhas = new System.Collections.Generic.List<string>();

        // Função local para simular exatamente o layout posicional de 245 caracteres da B3
        string CriarLinhaPosicional(string ticker, string precoCentavos)
        {
            var linha = new string(' ', 245).ToCharArray();
            "01".CopyTo(0, linha, 0, 2); // Código de Detalhe
            ticker.PadRight(12).CopyTo(0, linha, 12, 12); // Ticker na posição 12
            "010".CopyTo(0, linha, 24, 3); // Lote Padrão na posição 24
            precoCentavos.PadLeft(13, '0').CopyTo(0, linha, 108, 13); // Preço na posição 108
            return new string(linha);
        }

        linhas.Add(CriarLinhaPosicional("PETR4", "3500")); // R$ 35,00
        linhas.Add(CriarLinhaPosicional("VALE3", "6000")); // R$ 60,00
        linhas.Add(CriarLinhaPosicional("ITUB4", "3000")); // R$ 30,00
        linhas.Add(CriarLinhaPosicional("BBDC4", "1500")); // R$ 15,00
        linhas.Add(CriarLinhaPosicional("WEGE3", "4000")); // R$ 40,00

        System.IO.File.WriteAllLines(caminho, linhas);

        return Ok(new { Mensagem = "Arquivo da B3 gerado com sucesso para testes!", CaminhoArquivo = caminho });
    }
}