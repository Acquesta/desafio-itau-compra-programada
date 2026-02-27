using System.Collections.Generic;

namespace Itau.CompraProgramada.Domain.Interfaces;

public interface ICotacaoB3Provider
{
    IEnumerable<CotacaoDto> ObterCotacoesDeFechamento();
}