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

        // Base prices if file doesn't exist
        var precosAtuais = new System.Collections.Generic.Dictionary<string, decimal>
        {
            { "PETR4", 35.00m },
            { "VALE3", 60.00m },
            { "ITUB4", 30.00m },
            { "BBDC4", 15.00m },
            { "WEGE3", 40.00m }
        };

        // Try to read last generated file to get the latest prices, allowing continuous trends
        if (System.IO.File.Exists(caminho))
        {
            try
            {
                var linhasAtuais = System.IO.File.ReadAllLines(caminho);
                foreach (var l in linhasAtuais)
                {
                    if (l.Length >= 121)
                    {
                        var ticker = l.Substring(12, 12).Trim();
                        var precoStr = l.Substring(108, 13);
                        if (decimal.TryParse(precoStr, out var precoCentavos))
                        {
                            precosAtuais[ticker] = precoCentavos / 100m;
                        }
                    }
                }
            }
            catch { /* Ignora e usa a base se falhar leitura */ }
        }

        var rand = new Random();
        foreach (var kvp in precosAtuais)
        {
            // Calculate a random variation between -5% to +5%
            decimal variacaoPercentual = (decimal)(rand.NextDouble() * 0.10 - 0.05);
            decimal novoPreco = Math.Round(kvp.Value * (1 + variacaoPercentual), 2);
            
            // Format price to 13-digit string (in cents)
            string precoCentavosFormatado = ((int)(novoPreco * 100)).ToString();
            linhas.Add(CriarLinhaPosicional(kvp.Key, precoCentavosFormatado));
        }

        System.IO.File.WriteAllLines(caminho, linhas);

        return Ok(new { Mensagem = "Arquivo da B3 atualizado com FLUTUAÇÕES de até 5% nos preços!", CaminhoArquivo = caminho });
    }
}