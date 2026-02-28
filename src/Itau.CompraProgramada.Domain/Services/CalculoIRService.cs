using System;

namespace Itau.CompraProgramada.Domain.Services;

/// <summary>
/// RN-053 a RN-060: Cálculos fiscais de IR Dedo-Duro e IR sobre vendas.
/// </summary>
public class CalculoIRService
{
    private const decimal ALIQUOTA_DEDO_DURO = 0.00005m; // 0,005%
    private const decimal LIMITE_ISENCAO_MENSAL = 20_000m;
    private const decimal ALIQUOTA_LUCRO_VENDA = 0.20m; // 20%

    /// <summary>
    /// RN-053: Calcula o IR Dedo-Duro sobre uma operação de compra.
    /// </summary>
    public decimal CalcularDedoDuro(decimal valorOperacao)
    {
        return Math.Round(valorOperacao * ALIQUOTA_DEDO_DURO, 2);
    }

    /// <summary>
    /// RN-057 a RN-061: Calcula o IR sobre vendas considerando o total mensal.
    /// Retorna 0 se isento (vendas <= R$20k ou prejuízo líquido).
    /// </summary>
    public decimal CalcularIRSobreVendas(decimal totalVendasMes, decimal lucroLiquidoTotal)
    {
        // RN-058: Se total de vendas <= R$ 20.000, isento
        if (totalVendasMes <= LIMITE_ISENCAO_MENSAL)
            return 0m;

        // RN-061: Se houve prejuízo, IR = R$ 0,00
        if (lucroLiquidoTotal <= 0m)
            return 0m;

        // RN-059: 20% sobre o lucro líquido total
        return Math.Round(lucroLiquidoTotal * ALIQUOTA_LUCRO_VENDA, 2);
    }
}
