using System;
using System.Collections.Generic;
using System.Linq;
using Itau.CompraProgramada.Domain.Entities;
using Itau.CompraProgramada.Domain.Interfaces;

namespace Itau.CompraProgramada.Domain.Services;

/// <summary>
/// RN-050: Detecta desvios na proporção real da carteira vs. percentuais alvo da cesta.
/// RN-051: Limiar de desvio configurável (padrão: 5 pontos percentuais).
/// </summary>
public class CalculoDesvioService
{
    /// <summary>
    /// Retorna os tickers que estão acima do limiar de desvio.
    /// Cada item contém o ticker, a proporção real, a proporção alvo e a diferença.
    /// </summary>
    public IReadOnlyList<DesvioAtivo> CalcularDesvios(
        IReadOnlyCollection<Custodia> custodias,
        CestaRecomendacao cesta,
        IDictionary<string, decimal> cotacoes,
        decimal limiarPontoPercentual = 5m)
    {
        if (!custodias.Any()) return Array.Empty<DesvioAtivo>();

        // Calcular valor total da carteira
        decimal totalCarteira = 0;
        foreach (var c in custodias)
        {
            if (cotacoes.TryGetValue(c.Ticker, out var preco))
                totalCarteira += c.Quantidade * preco;
        }

        if (totalCarteira == 0) return Array.Empty<DesvioAtivo>();

        var desvios = new List<DesvioAtivo>();

        foreach (var item in cesta.Itens)
        {
            var custodia = custodias.FirstOrDefault(c => c.Ticker == item.Ticker);
            decimal valorAtual = 0;

            if (custodia != null && cotacoes.TryGetValue(item.Ticker, out var preco))
                valorAtual = custodia.Quantidade * preco;

            decimal proporcaoReal = (valorAtual / totalCarteira) * 100m;
            decimal proporcaoAlvo = item.Percentual;
            decimal diferenca = proporcaoReal - proporcaoAlvo;

            if (Math.Abs(diferenca) >= limiarPontoPercentual)
            {
                desvios.Add(new DesvioAtivo(
                    item.Ticker,
                    proporcaoReal,
                    proporcaoAlvo,
                    diferenca,
                    totalCarteira));
            }
        }

        // Verificar ativos na custódia que NÃO estão na cesta (0% alvo)
        foreach (var custodia in custodias)
        {
            if (custodia.Quantidade == 0) continue;
            if (cesta.Itens.Any(i => i.Ticker == custodia.Ticker)) continue;

            decimal valorAtual = 0;
            if (cotacoes.TryGetValue(custodia.Ticker, out var preco))
                valorAtual = custodia.Quantidade * preco;

            decimal proporcaoReal = (valorAtual / totalCarteira) * 100m;
            if (proporcaoReal >= limiarPontoPercentual)
            {
                desvios.Add(new DesvioAtivo(custodia.Ticker, proporcaoReal, 0m, proporcaoReal, totalCarteira));
            }
        }

        return desvios;
    }
}

public record DesvioAtivo(
    string Ticker,
    decimal ProporcaoReal,
    decimal ProporcaoAlvo,
    decimal Diferenca,
    decimal TotalCarteira);
