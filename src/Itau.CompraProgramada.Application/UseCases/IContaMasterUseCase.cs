using Itau.CompraProgramada.Application.DTOs;
using Itau.CompraProgramada.Domain.Interfaces;

namespace Itau.CompraProgramada.Application.UseCases;

public interface IContaMasterUseCase
{
    Task<CarteiraResponse> ObterCustodiaMasterAsync();
    Task InjetarMasterAsync();
}
