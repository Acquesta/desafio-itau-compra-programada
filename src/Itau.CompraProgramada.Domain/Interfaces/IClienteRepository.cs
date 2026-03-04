using Itau.CompraProgramada.Domain.Entities;

namespace Itau.CompraProgramada.Domain.Interfaces;

public interface IClienteRepository
{
    Task<Cliente?> ObterPorIdAsync(long id, CancellationToken cancellationToken = default);
    Task<Cliente?> ObterPorIdComCustodiaAsync(long id, CancellationToken cancellationToken = default);
    Task<Cliente?> ObterPorCpfAsync(string cpf, CancellationToken cancellationToken = default);
    Task<IEnumerable<Cliente>> ObterClientesAtivosComCustodiaAsync(CancellationToken cancellationToken = default);
    
    // Conta Master
    Task<Cliente?> ObterClienteMasterAsync(CancellationToken cancellationToken = default);

    Task<bool> ExisteCpfAsync(string cpf, CancellationToken cancellationToken = default);
    Task<bool> ExisteEmailAsync(string email, CancellationToken cancellationToken = default);
    Task AdicionarAsync(Cliente cliente, CancellationToken cancellationToken = default);
    void Atualizar(Cliente cliente);
}