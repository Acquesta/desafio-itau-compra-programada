using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Itau.CompraProgramada.Domain.Entities;
using Itau.CompraProgramada.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Itau.CompraProgramada.Infrastructure.Data.Repositories;

public class OrdemCompraRepository : IOrdemCompraRepository
{
    private readonly AppDbContext _context;

    public OrdemCompraRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task SalvarAsync(OrdemCompra ordemCompra, CancellationToken cancellationToken = default)
    {
        await _context.OrdensCompra.AddAsync(ordemCompra, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task SalvarVariosAsync(IEnumerable<OrdemCompra> ordensCompra, CancellationToken cancellationToken = default)
    {
        await _context.OrdensCompra.AddRangeAsync(ordensCompra, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<OrdemCompra?> ObterPorIdAsync(long id, CancellationToken cancellationToken = default)
    {
        return await _context.OrdensCompra
            .Include(o => o.Distribuicoes)
            .FirstOrDefaultAsync(o => o.Id == id, cancellationToken);
    }

    public async Task<IEnumerable<OrdemCompra>> ObterTodasDaContaMasterAsync(long contaMasterId, CancellationToken cancellationToken = default)
    {
        return await _context.OrdensCompra
            .Include(o => o.Distribuicoes)
            .Where(o => o.ContaMasterId == contaMasterId)
            .OrderByDescending(o => o.DataExecucao)
            .ToListAsync(cancellationToken);
    }
}
