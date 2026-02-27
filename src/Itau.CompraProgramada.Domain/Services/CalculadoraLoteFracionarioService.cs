using System;

namespace Itau.CompraProgramada.Domain.Services;

// Usamos record para representar o resultado imutável da nossa operação matemática
public record ResultadoLoteFracionario(string TickerLote, int QtdLote, string TickerFracionario, int QtdFracionaria);

public class CalculadoraLoteFracionarioService
{
    public ResultadoLoteFracionario Calcular(string tickerPadrao, int quantidadeTotal)
    {
        if (quantidadeTotal <= 0)
            return new ResultadoLoteFracionario(tickerPadrao, 0, $"{tickerPadrao}F", 0);

        // Lote padrão é sempre múltiplo de 100. A divisão inteira em C# já trunca os decimais.
        int qtdLote = (quantidadeTotal / 100) * 100;
        
        // O resto da divisão por 100 vai para o mercado fracionário
        int qtdFracionaria = quantidadeTotal % 100;
        
        string tickerFracionario = $"{tickerPadrao}F";

        return new ResultadoLoteFracionario(tickerPadrao, qtdLote, tickerFracionario, qtdFracionaria);
    }
}