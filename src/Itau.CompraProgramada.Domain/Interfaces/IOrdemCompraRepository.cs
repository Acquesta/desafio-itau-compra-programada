using System.Collections.Generic;
using System.Threading.Tasks;
using Itau.CompraProgramada.Domain.Entities;

using System.Threading;

namespace Itau.CompraProgramada.Domain.Interfaces;

public interface IOrdemCompraRepository
{
    Task SalvarAsync(OrdemCompra ordemCompra, CancellationToken cancellationToken = default);
    Task SalvarVariosAsync(IEnumerable<OrdemCompra> ordensCompra, CancellationToken cancellationToken = default);
    Task<OrdemCompra?> ObterPorIdAsync(long id, CancellationToken cancellationToken = default);
    Task<IEnumerable<OrdemCompra>> ObterTodasDaContaMasterAsync(long contaMasterId, CancellationToken cancellationToken = default);
}
