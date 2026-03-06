using Itau.CompraProgramada.Application.DTOs;
using Itau.CompraProgramada.Application.UseCases;
using Microsoft.AspNetCore.Mvc;

namespace Itau.CompraProgramada.Api.Controllers;

[ApiController]
[Route("api/admin/cesta")]
public class AdminCestaController : ControllerBase
{
    private readonly ICestaUseCase _cestaUseCase;

    public AdminCestaController(ICestaUseCase cestaUseCase)
    {
        _cestaUseCase = cestaUseCase;
    }

    /// <summary>
    /// RN-014 a RN-018: Cria uma nova cesta de recomendação e desativa a anterior.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(CestaResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CriarCesta([FromBody] CestaRequest request)
    {
        try
        {
            var response = await _cestaUseCase.CriarCestaAsync(request);
            return CreatedAtAction(nameof(ObterCestaAtual), new { }, response);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new ErrorResponse(ex.Message, "CESTA_INVALIDA"));
        }
    }

    /// <summary>
    /// RN-018: Obtém a cesta de recomendação atualmente vigente.
    /// </summary>
    [HttpGet("atual")]
    [ProducesResponseType(typeof(CestaResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> ObterCestaAtual()
    {
        var response = await _cestaUseCase.ObterCestaAtualAsync();
        
        if (response == null)
            return NoContent();

        return Ok(response);
    }

    /// <summary>
    /// RN-017: Obtém o histórico de cestas, trazendo as ativas e desativadas.
    /// </summary>
    [HttpGet("historico")]
    [ProducesResponseType(typeof(IEnumerable<CestaResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ObterHistorico()
    {
        var response = await _cestaUseCase.ObterHistoricoCestasAsync();
        return Ok(response);
    }
}
