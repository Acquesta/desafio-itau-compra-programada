using System;
using System.Threading.Tasks;
using Itau.CompraProgramada.Application.UseCases;
using Microsoft.AspNetCore.Mvc;

namespace Itau.CompraProgramada.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class RebalanceamentoController : ControllerBase
{
    private readonly IRebalanceamentoUseCase _rebalanceamentoUseCase;

    public RebalanceamentoController(IRebalanceamentoUseCase rebalanceamentoUseCase)
    {
        _rebalanceamentoUseCase = rebalanceamentoUseCase;
    }

    /// <summary>
    /// Simula uma venda de ativos para o rebalanceamento da carteira, aplicando a regra de IR.
    /// </summary>
    [HttpPost("vender")]
    public async Task<IActionResult> VenderAtivo(
        [FromQuery] long clienteId, 
        [FromQuery] string ticker, 
        [FromQuery] int quantidade, 
        [FromQuery] decimal precoVenda)
    {
        try
        {
            var resultado = await _rebalanceamentoUseCase.ExecutarVendaRebalanceamentoAsync(clienteId, ticker, quantidade, precoVenda);
            return Ok(new { Mensagem = resultado });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { Erro = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { Erro = "Erro interno no servidor.", Detalhe = ex.Message });
        }
    }
}