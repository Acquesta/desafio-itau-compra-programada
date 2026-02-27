using System;
using System.Threading.Tasks;

namespace Itau.CompraProgramada.Application.UseCases;

public interface IMotorCompraProgramadaUseCase
{
    // Retorna uma string com o resumo da operação (ex: "Compra executada para 5 clientes")
    Task<string> ExecutarComprasAsync(DateTime dataReferencia);
}