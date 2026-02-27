using System.Threading.Tasks;

namespace Itau.CompraProgramada.Domain.Interfaces;

public interface IUnitOfWork
{
    Task<bool> CommitAsync();
}