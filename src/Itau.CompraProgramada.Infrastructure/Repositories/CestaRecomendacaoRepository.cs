using Itau.CompraProgramada.Domain.Entities;
using Itau.CompraProgramada.Domain.Interfaces;
using Itau.CompraProgramada.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Itau.CompraProgramada.Infrastructure.Repositories;

public class CestaRecomendacaoRepository : ICestaRecomendacaoRepository
{
    private readonly AppDbContext _context;

    public CestaRecomendacaoRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<CestaRecomendacao?> ObterAtivaAsync()
    {
        return await _context.CestasRecomendacao
            .Include(c => c.Itens)
            .FirstOrDefaultAsync(c => c.Ativa);
    }

    public async Task<IEnumerable<CestaRecomendacao>> ObterHistoricoAsync()
    {
        return await _context.CestasRecomendacao
            .Include(c => c.Itens)
            .OrderByDescending(c => c.DataCriacao)
            .ToListAsync();
    }

    public async Task AdicionarAsync(CestaRecomendacao cesta)
    {
        await _context.CestasRecomendacao.AddAsync(cesta);
    }

    public void Atualizar(CestaRecomendacao cesta)
    {
        _context.CestasRecomendacao.Update(cesta);
    }
}
