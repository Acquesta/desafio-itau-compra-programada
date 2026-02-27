using System.Threading.Tasks;
using Itau.CompraProgramada.Domain.Entities;

namespace Itau.CompraProgramada.Domain.Interfaces;

public interface IEventoIRPublisher
{
    Task PublicarEventoAsync(EventoIR evento);
}