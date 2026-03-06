using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Itau.CompraProgramada.Domain.Entities;
using Itau.CompraProgramada.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Itau.CompraProgramada.Infrastructure.Data.Repositories;

public class RebalanceamentoRepository : IRebalanceamentoRepository
{
    private readonly AppDbContext _context;

    public RebalanceamentoRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task AdicionarAsync(Rebalanceamento rebalanceamento, CancellationToken cancellationToken = default)
    {
        await _context.Rebalanceamentos.AddAsync(rebalanceamento, cancellationToken);
    }

    public async Task<IEnumerable<Rebalanceamento>> ObterVendasMesCorrenteAsync(long clienteId, int mes, int ano, CancellationToken cancellationToken = default)
    {
        return await _context.Rebalanceamentos
            .Where(r => r.ClienteId == clienteId &&
                        r.DataRebalanceamento.Month == mes &&
                        r.DataRebalanceamento.Year == ano &&
                        r.ValorVenda > 0)
            .ToListAsync(cancellationToken);
    }
}
