using Itau.CompraProgramada.Application.DTOs;

namespace Itau.CompraProgramada.Application.UseCases;

public interface ICestaUseCase
{
    Task<CestaResponse> CriarCestaAsync(CestaRequest request);
    Task<CestaResponse?> ObterCestaAtualAsync();
    Task<IEnumerable<CestaResponse>> ObterHistoricoCestasAsync();
}
