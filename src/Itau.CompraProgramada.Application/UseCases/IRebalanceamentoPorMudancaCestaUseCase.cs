using System.Threading.Tasks;
using Itau.CompraProgramada.Domain.Entities;

namespace Itau.CompraProgramada.Application.UseCases;

public interface IRebalanceamentoPorMudancaCestaUseCase
{
    Task ExecutarRebalanceamentoAsync(CestaRecomendacao cestaAntiga, CestaRecomendacao cestaNova);
}
