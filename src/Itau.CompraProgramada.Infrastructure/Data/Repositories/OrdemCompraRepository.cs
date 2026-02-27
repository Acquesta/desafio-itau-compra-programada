using System.Collections.Generic;
using System.Linq;
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

    public async Task SalvarAsync(OrdemCompra ordemCompra)
    {
        await _context.OrdensCompra.AddAsync(ordemCompra);
        await _context.SaveChangesAsync();
    }

    public async Task SalvarVariosAsync(IEnumerable<OrdemCompra> ordensCompra)
    {
        await _context.OrdensCompra.AddRangeAsync(ordensCompra);
        await _context.SaveChangesAsync();
    }

    public async Task<OrdemCompra?> ObterPorIdAsync(long id)
    {
        return await _context.OrdensCompra
            .Include(o => o.Distribuicoes)
            .FirstOrDefaultAsync(o => o.Id == id);
    }

    public async Task<IEnumerable<OrdemCompra>> ObterTodasDaContaMasterAsync(long contaMasterId)
    {
        return await _context.OrdensCompra
            .Include(o => o.Distribuicoes)
            .Where(o => o.ContaMasterId == contaMasterId)
            .OrderByDescending(o => o.DataExecucao)
            .ToListAsync();
    }
}
