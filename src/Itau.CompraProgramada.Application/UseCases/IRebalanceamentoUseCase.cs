using System.Threading.Tasks;

namespace Itau.CompraProgramada.Application.UseCases;

public interface IRebalanceamentoUseCase
{
    Task<string> ExecutarVendaRebalanceamentoAsync(long clienteId, string ticker, int quantidade, decimal precoVendaAtual);
}