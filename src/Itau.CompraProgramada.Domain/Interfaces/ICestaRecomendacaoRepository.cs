using Itau.CompraProgramada.Domain.Entities;

using System.Threading;

namespace Itau.CompraProgramada.Domain.Interfaces;

public interface ICestaRecomendacaoRepository
{
    Task<CestaRecomendacao?> ObterAtivaAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<CestaRecomendacao>> ObterHistoricoAsync(CancellationToken cancellationToken = default);
    Task AdicionarAsync(CestaRecomendacao cesta, CancellationToken cancellationToken = default);
    void Atualizar(CestaRecomendacao cesta);
}
