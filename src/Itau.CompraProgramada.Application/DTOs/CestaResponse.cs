namespace Itau.CompraProgramada.Application.DTOs;

public record CestaItemResponse(string Ticker, decimal Percentual);

public record CestaResponse(
    long Id, 
    string Nome, 
    bool Ativa, 
    DateTime DataCriacao, 
    DateTime? DataDesativacao, 
    List<CestaItemResponse> Itens
);
