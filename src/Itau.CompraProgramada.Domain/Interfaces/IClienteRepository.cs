using System.Collections.Generic;
using System.Threading.Tasks;
using Itau.CompraProgramada.Domain.Entities;

namespace Itau.CompraProgramada.Domain.Interfaces;

public interface IClienteRepository
{
    Task<Cliente?> ObterPorIdAsync(long id);
    Task<Cliente?> ObterPorCpfAsync(string cpf);
    Task<IEnumerable<Cliente>> ObterClientesAtivosComCustodiaAsync();
    Task AdicionarAsync(Cliente cliente);
    void Atualizar(Cliente cliente);
}