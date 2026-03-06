using System.Threading.Tasks;
using Itau.CompraProgramada.Domain.Interfaces;
using Itau.CompraProgramada.Infrastructure.Data;

namespace Itau.CompraProgramada.Infrastructure.Repositories;

public class UnitOfWork : IUnitOfWork
{
    private readonly AppDbContext _context;

    public UnitOfWork(AppDbContext context)
    {
        _context = context;
    }

    public async Task<bool> CommitAsync(CancellationToken cancellationToken = default)
    {
        // Guarda todas as alterações pendentes no MySQL. Retorna true se algo foi guardado.
        return await _context.SaveChangesAsync(cancellationToken) > 0;
    }
}