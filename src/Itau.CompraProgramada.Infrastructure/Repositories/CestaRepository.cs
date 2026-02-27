using System.Threading.Tasks;
using Itau.CompraProgramada.Domain.Entities;
using Itau.CompraProgramada.Domain.Interfaces;
using Itau.CompraProgramada.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Itau.CompraProgramada.Infrastructure.Repositories;

public class CestaRepository : ICestaRepository
{
    private readonly AppDbContext _context;

    public CestaRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<CestaRecomendacao?> ObterCestaAtivaAsync()
    {
        return await _context.CestasRecomendacao
            .Include(c => c.Itens)
            .FirstOrDefaultAsync(c => c.Ativa);
    }
}