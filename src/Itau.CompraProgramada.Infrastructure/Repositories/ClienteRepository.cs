using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Itau.CompraProgramada.Domain.Entities;
using Itau.CompraProgramada.Domain.Interfaces;
using Itau.CompraProgramada.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Itau.CompraProgramada.Infrastructure.Repositories;

public class ClienteRepository : IClienteRepository
{
    private readonly AppDbContext _context;

    public ClienteRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Cliente?> ObterPorIdAsync(long id, CancellationToken cancellationToken = default)
    {
        return await _context.Clientes.FindAsync(new object[] { id }, cancellationToken);
    }

    public async Task<Cliente?> ObterPorIdComCustodiaAsync(long id, CancellationToken cancellationToken = default)
    {
        // O Include e ThenInclude garantem que trazemos as contas e as ações em custódia juntas!
        return await _context.Clientes
            .Include(c => c.ContaGrafica)
                .ThenInclude(cg => cg.Custodias)
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
    }

    public async Task<Cliente?> ObterPorCpfAsync(string cpf, CancellationToken cancellationToken = default)
    {
        return await _context.Clientes.FirstOrDefaultAsync(c => c.Cpf == cpf, cancellationToken);
    }

    public async Task<IEnumerable<Cliente>> ObterClientesAtivosComCustodiaAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Clientes
            .Include(c => c.ContaGrafica)
                .ThenInclude(cg => cg.Custodias)
            .Where(c => c.Ativo && c.ContaGrafica.Tipo == Domain.Enums.TipoContaGrafica.Filhote)
            .ToListAsync(cancellationToken);
    }

    public async Task<Cliente?> ObterClienteMasterAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Clientes
            .Include(c => c.ContaGrafica)
                .ThenInclude(cg => cg.Custodias)
            .FirstOrDefaultAsync(c => c.ContaGrafica.Tipo == Domain.Enums.TipoContaGrafica.Master, cancellationToken);
    }

    public async Task<bool> ExisteCpfAsync(string cpf, CancellationToken cancellationToken = default)
    {
        return await _context.Clientes.AnyAsync(c => c.Cpf == cpf, cancellationToken);
    }

    public async Task AdicionarAsync(Cliente cliente, CancellationToken cancellationToken = default)
    {
        await _context.Clientes.AddAsync(cliente, cancellationToken);
    }

    public void Atualizar(Cliente cliente)
    {
        _context.Clientes.Update(cliente);
    }
}