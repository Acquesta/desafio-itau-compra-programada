using System;
using System.Threading.Tasks;
using Itau.CompraProgramada.Application.DTOs;

namespace Itau.CompraProgramada.Application.UseCases;

public interface IMotorCompraProgramadaUseCase
{
    // Retorna detalhes das ordens executadas no mercado
    Task<MotorCompraResponse> ExecutarComprasAsync(DateTime dataReferencia);
}