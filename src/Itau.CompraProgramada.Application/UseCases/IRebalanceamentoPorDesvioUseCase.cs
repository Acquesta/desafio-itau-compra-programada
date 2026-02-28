using System.Threading.Tasks;
using Itau.CompraProgramada.Domain.Entities;

namespace Itau.CompraProgramada.Application.UseCases;

public interface IRebalanceamentoPorDesvioUseCase
{
    Task<RebalanceamentoDesvioResponse> ExecutarRebalanceamentoPorDesvioAsync(decimal limiarPontoPercentual = 5m);
}

public record RebalanceamentoDesvioResponse(
    int ClientesAnalisados,
    int ClientesRebalanceados,
    int TotalVendas,
    int TotalCompras);
