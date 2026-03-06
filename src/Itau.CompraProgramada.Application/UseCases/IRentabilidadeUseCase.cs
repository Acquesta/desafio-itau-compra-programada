using System.Threading.Tasks;
using Itau.CompraProgramada.Application.DTOs;

namespace Itau.CompraProgramada.Application.UseCases;

public interface IRentabilidadeUseCase
{
    Task<RentabilidadeResponse> ObterRentabilidadeAsync(long clienteId);
}
