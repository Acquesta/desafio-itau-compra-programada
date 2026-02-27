using System;

namespace Itau.CompraProgramada.Domain.Services;

public class DataCompraService
{
    private readonly int[] _diasDeCompra = { 5, 15, 25 };

    /// <summary>
    /// Verifica se a data fornecida é um dia válido para execução da compra programada.
    /// Caso o dia 5, 15 ou 25 caia num fim de semana, a compra é transferida para o próximo dia útil.
    /// (RN-020 a RN-022)
    /// </summary>
    public bool EhDiaDeCompraValido(DateTime dataAtual)
    {
        foreach (var diaAlvo in _diasDeCompra)
        {
            var dataPrevista = new DateTime(dataAtual.Year, dataAtual.Month, diaAlvo);
            var dataAjustada = AjustarParaProximoDiaUtil(dataPrevista);

            if (dataAtual.Date == dataAjustada.Date)
                return true;
        }

        return false;
    }

    private static DateTime AjustarParaProximoDiaUtil(DateTime data)
    {
        while (data.DayOfWeek == DayOfWeek.Saturday || data.DayOfWeek == DayOfWeek.Sunday)
        {
            data = data.AddDays(1);
        }
        
        // TODO: Integração futura com API de Feriados Nacionais (ex: B3 ou Banco Central)
        // Se a data cair em um feriado, data = AjustarParaProximoDiaUtil(data.AddDays(1))

        return data;
    }
}
