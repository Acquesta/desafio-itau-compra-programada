using System.Threading.Tasks;
using Itau.CompraProgramada.Domain.Entities;

namespace Itau.CompraProgramada.Domain.Interfaces;

public interface ICestaRepository
{
    Task<CestaRecomendacao?> ObterCestaAtivaAsync();
}