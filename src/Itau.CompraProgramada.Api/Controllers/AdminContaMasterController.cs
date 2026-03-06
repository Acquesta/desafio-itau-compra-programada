using Itau.CompraProgramada.Application.DTOs;
using Itau.CompraProgramada.Application.UseCases;
using Microsoft.AspNetCore.Mvc;

namespace Itau.CompraProgramada.Api.Controllers;

[ApiController]
[Route("api/admin/conta-master")]
public class AdminContaMasterController : ControllerBase
{
    private readonly IContaMasterUseCase _contaMasterUseCase;

    public AdminContaMasterController(IContaMasterUseCase contaMasterUseCase)
    {
        _contaMasterUseCase = contaMasterUseCase;
    }

    /// <summary>
    /// Lista os ativos e resíduos acumulados na Conta Gráfica Master (Admin only).
    /// </summary>
    [HttpGet("custodia")]
    [ProducesResponseType(typeof(CarteiraResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ObterCustodia()
    {
        try
        {
            var response = await _contaMasterUseCase.ObterCustodiaMasterAsync();
            return Ok(response);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new ErrorResponse("Master não encontrada", ex.Message));
        }
    }

    /// <summary>
    /// Força a criação da Conta Master caso o banco esteja vazio. Função auxiliar de QA.
    /// </summary>
    [HttpPost("injetar-mock")]
    public async Task<IActionResult> InjetarMasterMock()
    {
        try
        {
            await _contaMasterUseCase.InjetarMasterAsync();
            return Ok(new { Mensagem = "Conta Master injetada/verificada com sucesso." });
        }
        catch (Exception ex)
        {
            return BadRequest(new { Erro = ex.Message });
        }
    }
}
