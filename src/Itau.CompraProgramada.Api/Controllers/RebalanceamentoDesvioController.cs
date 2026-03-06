using Itau.CompraProgramada.Application.UseCases;
using Microsoft.AspNetCore.Mvc;

namespace Itau.CompraProgramada.Api.Controllers;

[ApiController]
[Route("api/admin")]
public class RebalanceamentoDesvioController : ControllerBase
{
    private readonly IRebalanceamentoPorDesvioUseCase _rebalanceamentoDesvioUseCase;

    public RebalanceamentoDesvioController(IRebalanceamentoPorDesvioUseCase rebalanceamentoDesvioUseCase)
    {
        _rebalanceamentoDesvioUseCase = rebalanceamentoDesvioUseCase;
    }

    /// <summary>
    /// RN-050 a RN-052: Executa rebalanceamento por desvio de proporção.
    /// </summary>
    [HttpPost("rebalanceamento-desvio")]
    public async Task<IActionResult> ExecutarRebalanceamentoPorDesvio([FromQuery] decimal limiar = 5m)
    {
        try
        {
            var resultado = await _rebalanceamentoDesvioUseCase.ExecutarRebalanceamentoPorDesvioAsync(limiar);
            return Ok(resultado);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { Erro = ex.Message });
        }
    }
}
