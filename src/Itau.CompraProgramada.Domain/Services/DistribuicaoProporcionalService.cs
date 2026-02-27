using System;
using System.Collections.Generic;
using System.Linq;

namespace Itau.CompraProgramada.Domain.Services;

public record ResultadoDistribuicao(long ClienteId, int Quantidade);
public record RelatorioDistribuicao(IReadOnlyCollection<ResultadoDistribuicao> Distribuicoes, int ResiduoMaster);

public class DistribuicaoProporcionalService
{
    /// <summary>
    /// Calcula a distribuição de um ativo baseado na proporção financeira de cada cliente.
    /// </summary>
    /// <param name="quantidadeTotalDisponivel">Qtd comprada no pregão + Saldo Master anterior</param>
    /// <param name="aportesClientes">Dicionário contendo ClienteId e o Valor do Aporte mensal correspondente</param>
    public RelatorioDistribuicao Distribuir(int quantidadeTotalDisponivel, Dictionary<long, decimal> aportesClientes)
    {
        var distribuicoes = new List<ResultadoDistribuicao>();

        if (quantidadeTotalDisponivel <= 0 || aportesClientes == null || !aportesClientes.Any())
            return new RelatorioDistribuicao(distribuicoes.AsReadOnly(), quantidadeTotalDisponivel);

        // Soma total dos aportes da rodada
        decimal totalAportes = aportesClientes.Sum(x => x.Value);
        int totalDistribuido = 0;

        foreach (var aporte in aportesClientes)
        {
            long clienteId = aporte.Key;
            decimal valorAporteCliente = aporte.Value;

            // RN-035: Proporcao do cliente = Aporte do Cliente / Total de Aportes
            decimal proporcao = valorAporteCliente / totalAportes;

            // RN-036: Quantidade = TRUNCAR(Proporcao x Quantidade Total)
            int quantidadeCliente = (int)Math.Truncate(proporcao * quantidadeTotalDisponivel);

            if (quantidadeCliente > 0)
            {
                distribuicoes.Add(new ResultadoDistribuicao(clienteId, quantidadeCliente));
                totalDistribuido += quantidadeCliente;
            }
        }

        // RN-039: Ações não distribuídas permanecem na custódia master
        int residuo = quantidadeTotalDisponivel - totalDistribuido;

        return new RelatorioDistribuicao(distribuicoes.AsReadOnly(), residuo);
    }
}