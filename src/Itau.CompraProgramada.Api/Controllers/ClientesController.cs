using Itau.CompraProgramada.Application.DTOs;
using Itau.CompraProgramada.Application.UseCases;
using Microsoft.AspNetCore.Mvc;

namespace Itau.CompraProgramada.Api.Controllers;

[ApiController]
[Route("api/clientes")]
public class ClientesController : ControllerBase
{
    private readonly IClienteUseCase _clienteUseCase;

    public ClientesController(IClienteUseCase clienteUseCase)
    {
        _clienteUseCase = clienteUseCase;
    }

    /// <summary>
    /// RN-001 a RN-006: Adesão do cliente ao produto.
    /// </summary>
    [HttpPost("adesao")]
    [ProducesResponseType(typeof(AdesaoResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Aderir([FromBody] AdesaoRequest request)
    {
        try
        {
            var response = await _clienteUseCase.AderirAsync(request);
            // Retorna 201 Created apontando para a consulta da carteira do recém-criado cliente
            return CreatedAtAction(nameof(ObterCarteira), new { id = response.ClienteId }, response);
        }
        catch (InvalidOperationException ex) when (ex.Message == "CLIENTE_CPF_DUPLICADO")
        {
            return Conflict(new ErrorResponse("O CPF informado já está registrado em outro cliente ativo.", ex.Message));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new ErrorResponse(ex.Message, "DADOS_INVALIDOS"));
        }
    }

    /// <summary>
    /// RN-007 a RN-010: Saída do cliente do produto.
    /// </summary>
    [HttpPost("{id}/saida")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Sair(long id)
    {
        try
        {
            await _clienteUseCase.SairAsync(id);
            return NoContent();
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new ErrorResponse("Cliente não encontrado.", ex.Message));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new ErrorResponse(ex.Message, "OPERACAO_INVALIDA"));
        }
    }

    /// <summary>
    /// RN-011 a RN-013: Alteração do valor mensal.
    /// </summary>
    [HttpPut("{id}/valor-mensal")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AlterarValorMensal(long id, [FromBody] AlterarValorRequest request)
    {
        try
        {
            await _clienteUseCase.AlterarValorMensalAsync(id, request);
            return NoContent();
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new ErrorResponse("Cliente não encontrado.", ex.Message));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new ErrorResponse(ex.Message, "OPERACAO_INVALIDA"));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new ErrorResponse(ex.Message, "DADOS_INVALIDOS"));
        }
    }

    /// <summary>
    /// RN-010 + preview RN-063 a RN-070: Consulta da carteira do cliente.
    /// </summary>
    [HttpGet("{id}/carteira")]
    [ProducesResponseType(typeof(CarteiraResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ObterCarteira(long id)
    {
        try
        {
            var carteira = await _clienteUseCase.ObterCarteiraAsync(id);
            return Ok(carteira);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new ErrorResponse("Cliente não encontrado.", ex.Message));
        }
    }
}
