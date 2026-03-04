using System.Collections.Generic;
using System.IO;
using System.Linq;
using System;
using Itau.CompraProgramada.Domain.Interfaces;

namespace Itau.CompraProgramada.Infrastructure.B3;

public class CotacaoB3Provider : ICotacaoB3Provider
{
    private readonly CotahistParser _parser;

    public CotacaoB3Provider()
    {
        _parser = new CotahistParser();
    }

    public IEnumerable<CotacaoDto> ObterCotacoesDeFechamento()
    {
        var diretorio = Directory.GetCurrentDirectory();

        var arquivoMaisRecente = Directory.GetFiles(diretorio, "COTAHIST*.TXT")
                                          .OrderByDescending(f => f)
                                          .FirstOrDefault();
                                          
        if (arquivoMaisRecente == null)
            throw new FileNotFoundException($"Nenhum arquivo de cotação COTAHIST*.TXT encontrado em {diretorio}");

        var cotacoesBrutas = _parser.LerCotacoesDeFechamento(arquivoMaisRecente);
        
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