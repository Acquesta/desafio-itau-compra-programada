using System.Threading.Tasks;
using System.Threading;

namespace Itau.CompraProgramada.Domain.Interfaces;

public interface IUnitOfWork
{
    Task<bool> CommitAsync(CancellationToken cancellationToken = default);
}