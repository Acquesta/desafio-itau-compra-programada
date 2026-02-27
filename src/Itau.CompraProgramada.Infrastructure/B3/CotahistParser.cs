using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace Itau.CompraProgramada.Infrastructure.B3;

// DTO para carregar os dados parseados em memória
public class CotacaoB3Dto
{
    public string Ticker { get; set; } = string.Empty;
    public decimal PrecoFechamento { get; set; }
    public int TipoMercado { get; set; }
}

public class CotahistParser
{
    public IEnumerable<CotacaoB3Dto> LerCotacoesDeFechamento(string caminhoArquivo)
    {
        var cotacoes = new List<CotacaoB3Dto>();

        // Registra o provider para suportar o encoding ISO-8859-1 (Latin1) usado pela B3
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        var encoding = Encoding.GetEncoding("ISO-8859-1");

        if (!File.Exists(caminhoArquivo))
            throw new FileNotFoundException($"Arquivo COTAHIST não encontrado no caminho: {caminhoArquivo}");

        // ReadLines lê linha por linha, economizando muita memória em arquivos grandes
        foreach (var linha in File.ReadLines(caminhoArquivo, encoding))
        {
            // Ignorar header (00), trailer (99) ou linhas inválidas
            if (linha.Length < 245 || !linha.StartsWith("01"))
                continue;

            // Extrair o Tipo de Mercado (Posição 25-27)
            if (!int.TryParse(linha.Substring(24, 3), out int tipoMercado))
                continue;

            // Filtramos apenas Mercado a Vista (010) e Fracionário (020)
            if (tipoMercado != 10 && tipoMercado != 20)
                continue;

            var ticker = linha.Substring(12, 12).Trim();
            
            // O preço de fechamento está nas posições 109-121 (13 caracteres)
            // Lembrete: O valor vem com 2 casas decimais implícitas. Ex: 0000000003850 = 38.50
            var valorBrutoStr = linha.Substring(108, 13).Trim();
            
            if (long.TryParse(valorBrutoStr, out long valorBruto))
            {
                decimal precoFechamento = valorBruto / 100m;

                cotacoes.Add(new CotacaoB3Dto
                {
                    Ticker = ticker,
                    PrecoFechamento = precoFechamento,
                    TipoMercado = tipoMercado
                });
            }
        }

        return cotacoes;
    }
}