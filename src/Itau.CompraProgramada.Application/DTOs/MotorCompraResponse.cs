using System.Collections.Generic;

namespace Itau.CompraProgramada.Application.DTOs;

public record MotorCompraResponse(
    int ClientesProcessados,
    int EventosIRGerados,
    List<OrdemExecutadaDto> OrdensExecutadas
);

public record OrdemExecutadaDto(
    string Ticker,
    int QuantidadeLote,
    int QuantidadeFracionaria,
    decimal PrecoMedio
);
