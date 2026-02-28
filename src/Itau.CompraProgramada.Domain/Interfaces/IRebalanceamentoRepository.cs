using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Itau.CompraProgramada.Domain.Entities;

namespace Itau.CompraProgramada.Domain.Interfaces;

public interface IRebalanceamentoRepository
{
    Task AdicionarAsync(Rebalanceamento rebalanceamento, CancellationToken cancellationToken = default);
    Task<IEnumerable<Rebalanceamento>> ObterVendasMesCorrenteAsync(long clienteId, int mes, int ano, CancellationToken cancellationToken = default);
}
