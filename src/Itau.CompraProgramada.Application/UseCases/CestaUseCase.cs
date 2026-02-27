using Itau.CompraProgramada.Application.DTOs;
using Itau.CompraProgramada.Domain.Entities;
using Itau.CompraProgramada.Domain.Interfaces;

namespace Itau.CompraProgramada.Application.UseCases;

public class CestaUseCase : ICestaUseCase
{
    private readonly ICestaRecomendacaoRepository _cestaRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CestaUseCase(ICestaRecomendacaoRepository cestaRepository, IUnitOfWork unitOfWork)
    {
        _cestaRepository = cestaRepository;
        _unitOfWork = unitOfWork;
    }

    /// <summary>
    /// RN-014 a RN-018: Criar nova cesta e desativar a anterior.
    /// </summary>
    public async Task<CestaResponse> CriarCestaAsync(CestaRequest request)
    {
        // RN-014 e RN-015: Validações ocorrem no construtor da CestaRecomendacao
        var itens = request.Itens.Select(i => new ItemCesta(i.Ticker, i.Percentual)).ToList();
        var novaCesta = new CestaRecomendacao(request.Nome, itens);

        // RN-017: Desativar cesta anterior (se existir)
        var cestaAtual = await _cestaRepository.ObterAtivaAsync();
        if (cestaAtual != null)
        {
            // RN-018: Apenas uma cesta ativa por vez
            cestaAtual.Desativar();
            _cestaRepository.Atualizar(cestaAtual);

            // TODO: Iniciar processo de rebalanceamento assíncrono (Fase 7)
            // Aqui futuramente será publicado um evento no Kafka para que os rebalanceamentos
            // aconteçam de forma desvinculada dessa transação (evitando travamentos).
        }

        await _cestaRepository.AdicionarAsync(novaCesta);
        await _unitOfWork.CommitAsync();

        return MapearParaResponse(novaCesta);
    }

    /// <summary>
    /// RN-018: Obter a cesta atualmente vigente.
    /// </summary>
    public async Task<CestaResponse?> ObterCestaAtualAsync()
    {
        var cesta = await _cestaRepository.ObterAtivaAsync();
        if (cesta == null) return null;

        return MapearParaResponse(cesta);
    }

    /// <summary>
    /// RN-017: Obter o histórico de cestas (ativas e inativas).
    /// </summary>
    public async Task<IEnumerable<CestaResponse>> ObterHistoricoCestasAsync()
    {
        var historico = await _cestaRepository.ObterHistoricoAsync();
        return historico.Select(MapearParaResponse);
    }

    private static CestaResponse MapearParaResponse(CestaRecomendacao cesta)
    {
        return new CestaResponse(
            cesta.Id,
            cesta.Nome,
            cesta.Ativa,
            cesta.DataCriacao,
            cesta.DataDesativacao,
            cesta.Itens.Select(i => new CestaItemResponse(i.Ticker, i.Percentual)).ToList()
        );
    }
}
