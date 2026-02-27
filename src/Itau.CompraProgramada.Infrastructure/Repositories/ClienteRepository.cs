using System.Collections.Generic;
using System.Linq;
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

    public async Task<Cliente?> ObterPorIdAsync(long id)
    {
        return await _context.Clientes.FindAsync(id);
    }

    public async Task<Cliente?> ObterPorIdComCustodiaAsync(long id)
    {
        // O Include e ThenInclude garantem que trazemos as contas e as ações em custódia juntas!
        return await _context.Clientes
            .Include(c => c.ContaGrafica)
                .ThenInclude(cg => cg.Custodias)
            .FirstOrDefaultAsync(c => c.Id == id);
    }

    public async Task<Cliente?> ObterPorCpfAsync(string cpf)
    {
        return await _context.Clientes.FirstOrDefaultAsync(c => c.Cpf == cpf);
    }

    public async Task<IEnumerable<Cliente>> ObterClientesAtivosComCustodiaAsync()
    {
        return await _context.Clientes
            .Include(c => c.ContaGrafica)
                .ThenInclude(cg => cg.Custodias)
            .Where(c => c.Ativo)
            .ToListAsync();
    }

    public async Task<bool> ExisteCpfAsync(string cpf)
    {
        return await _context.Clientes.AnyAsync(c => c.Cpf == cpf);
    }

    public async Task AdicionarAsync(Cliente cliente)
    {
        await _context.Clientes.AddAsync(cliente);
    }

    public void Atualizar(Cliente cliente)
    {
        _context.Clientes.Update(cliente);
    }
}