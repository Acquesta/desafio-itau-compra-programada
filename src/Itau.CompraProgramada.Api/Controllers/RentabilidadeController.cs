using Itau.CompraProgramada.Application.UseCases;
using Microsoft.AspNetCore.Mvc;

namespace Itau.CompraProgramada.Api.Controllers;

[ApiController]
[Route("api/clientes")]
public class RentabilidadeController : ControllerBase
{
    private readonly IRentabilidadeUseCase _rentabilidadeUseCase;

    public RentabilidadeController(IRentabilidadeUseCase rentabilidadeUseCase)
    {
        _rentabilidadeUseCase = rentabilidadeUseCase;
    }

    /// <summary>
    /// RN-063 a RN-070: Retorna a rentabilidade da carteira do cliente.
    /// </summary>
    [HttpGet("{clienteId}/rentabilidade")]
    public async Task<IActionResult> ObterRentabilidade(long clienteId)
    {
        try
        {
            var resultado = await _rentabilidadeUseCase.ObterRentabilidadeAsync(clienteId);
            return Ok(resultado);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { Erro = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { Erro = ex.Message });
        }
    }
}
