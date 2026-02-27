using System.Collections.Generic;
using System.Threading.Tasks;
using Itau.CompraProgramada.Domain.Entities;

namespace Itau.CompraProgramada.Domain.Interfaces;

public interface IOrdemCompraRepository
{
    Task SalvarAsync(OrdemCompra ordemCompra);
    Task SalvarVariosAsync(IEnumerable<OrdemCompra> ordensCompra);
    Task<OrdemCompra?> ObterPorIdAsync(long id);
    Task<IEnumerable<OrdemCompra>> ObterTodasDaContaMasterAsync(long contaMasterId);
}
