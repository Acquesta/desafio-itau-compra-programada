using System.Collections.Generic;
using System.Linq;
using Itau.CompraProgramada.Domain.Interfaces;

namespace Itau.CompraProgramada.Infrastructure.B3;

public class CotacaoB3Provider : ICotacaoB3Provider
{
    private readonly CotahistParser _parser;

    public CotacaoB3Provider()
    {
        _parser = new CotahistParser();
    }

    public IEnumerable<CotacaoDto> ObterCotacoesDeFechamento(string caminhoArquivo)
    {
        var cotacoesBrutas = _parser.LerCotacoesDeFechamento(caminhoArquivo);
        
        // Mapeia do DTO da Infra para o DTO do Domínio (Mantendo o Lote Padrão)
        return cotacoesBrutas
            .Where(c => c.TipoMercado == 10) 
            .Select(c => new CotacaoDto 
            { 
                Ticker = c.Ticker, 
                PrecoFechamento = c.PrecoFechamento 
            });
    }
}