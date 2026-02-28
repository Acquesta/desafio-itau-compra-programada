using System.Threading;
using System.Threading.Tasks;
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

    public async Task<CestaRecomendacao?> ObterAtivaAsync(CancellationToken cancellationToken = default)
    {
        return await _context.CestasRecomendacao
            .Include(c => c.Itens)
            .FirstOrDefaultAsync(c => c.Ativa, cancellationToken);
    }

    public async Task<IEnumerable<CestaRecomendacao>> ObterHistoricoAsync(CancellationToken cancellationToken = default)
    {
        return await _context.CestasRecomendacao
            .Include(c => c.Itens)
            .OrderByDescending(c => c.DataCriacao)
            .ToListAsync(cancellationToken);
    }

    public async Task AdicionarAsync(CestaRecomendacao cesta, CancellationToken cancellationToken = default)
    {
        await _context.CestasRecomendacao.AddAsync(cesta, cancellationToken);
    }

    public void Atualizar(CestaRecomendacao cesta)
    {
        _context.CestasRecomendacao.Update(cesta);
    }
}
