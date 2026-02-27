using Itau.CompraProgramada.Domain.Entities;

namespace Itau.CompraProgramada.Domain.Interfaces;

public interface IClienteRepository
{
    Task<Cliente?> ObterPorIdAsync(long id);
    Task<Cliente?> ObterPorIdComCustodiaAsync(long id);
    Task<Cliente?> ObterPorCpfAsync(string cpf);
    Task<IEnumerable<Cliente>> ObterClientesAtivosComCustodiaAsync();
    Task<bool> ExisteCpfAsync(string cpf);
    Task AdicionarAsync(Cliente cliente);
    void Atualizar(Cliente cliente);
}