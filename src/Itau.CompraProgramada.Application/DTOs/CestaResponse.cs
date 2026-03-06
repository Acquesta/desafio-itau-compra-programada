using System.Text.Json.Serialization;

namespace Itau.CompraProgramada.Application.DTOs;

public record CestaItemResponse(string Ticker, decimal Percentual);

public record CestaAnteriorDto(
    [property: JsonPropertyName("cestaId")] long CestaId,
    string Nome,
    DateTime DataDesativacao
);

public record CestaResponse(
    [property: JsonPropertyName("cestaId")] long Id, 
    string Nome, 
    bool Ativa, 
    DateTime DataCriacao, 
    DateTime? DataDesativacao, 
    List<CestaItemResponse> Itens,
    bool? RebalanceamentoDisparado = null,
    List<string>? AtivosRemovidos = null,
    List<string>? AtivosAdicionados = null,
    CestaAnteriorDto? CestaAnteriorDesativada = null,
    string? Mensagem = null
);
