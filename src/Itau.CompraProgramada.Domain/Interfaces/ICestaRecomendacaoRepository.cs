using Itau.CompraProgramada.Domain.Entities;

namespace Itau.CompraProgramada.Domain.Interfaces;

public interface ICestaRecomendacaoRepository
{
    Task<CestaRecomendacao?> ObterAtivaAsync();
    Task<IEnumerable<CestaRecomendacao>> ObterHistoricoAsync();
    Task AdicionarAsync(CestaRecomendacao cesta);
    void Atualizar(CestaRecomendacao cesta);
}
