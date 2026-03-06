namespace Itau.CompraProgramada.Application.DTOs;

public record CestaItemRequest(string Ticker, decimal Percentual);

public record CestaRequest(string Nome, List<CestaItemRequest> Itens);
