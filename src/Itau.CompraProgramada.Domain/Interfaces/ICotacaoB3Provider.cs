using System.Collections.Generic;

namespace Itau.CompraProgramada.Domain.Interfaces;

// Um DTO simples para transitar os dados da B3
public class CotacaoDto
{
    public string Ticker { get; set; } = string.Empty;
    public decimal PrecoFechamento { get; set; }
}

public interface ICotacaoB3Provider
{
    IEnumerable<CotacaoDto> ObterCotacoesDeFechamento(string caminhoArquivo);
}