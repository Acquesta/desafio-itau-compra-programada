using Itau.CompraProgramada.Application.DTOs;

namespace Itau.CompraProgramada.Application.UseCases;

public interface IClienteUseCase
{
    Task<AdesaoResponse> AderirAsync(AdesaoRequest request);
    Task SairAsync(long clienteId);
    Task AlterarValorMensalAsync(long clienteId, AlterarValorRequest request);
    Task<CarteiraResponse> ObterCarteiraAsync(long clienteId);
}
