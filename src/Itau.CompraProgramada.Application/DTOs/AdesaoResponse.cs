using System.Text.Json.Serialization;

namespace Itau.CompraProgramada.Application.DTOs;

public record AdesaoResponse(
    long ClienteId,
    string Nome,
    string Cpf,
    string Email,
    decimal ValorMensal,
    bool Ativo,
    DateTime DataAdesao,
    ContaGraficaDto ContaGrafica
);

public record ContaGraficaDto(
    [property: JsonPropertyName("id")] long ContaId,
    string NumeroConta,
    string Tipo
);
