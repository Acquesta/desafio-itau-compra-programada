namespace Itau.CompraProgramada.Application.DTOs;

public record CarteiraResponse(
    long ClienteId,
    string Nome,
    string Cpf,
    bool Ativo,
    decimal ValorMensal,
    decimal SaldoTotal,
    List<CustodiaItemDto> Ativos
);

public record CustodiaItemDto(
    string Ticker,
    int Quantidade,
    decimal PrecoMedio,
    decimal ValorAtual,
    decimal PL
);
