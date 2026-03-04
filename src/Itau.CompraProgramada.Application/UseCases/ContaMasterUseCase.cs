using Itau.CompraProgramada.Application.DTOs;
using Itau.CompraProgramada.Domain.Entities;
using Itau.CompraProgramada.Domain.Enums;
using Itau.CompraProgramada.Domain.Interfaces;

namespace Itau.CompraProgramada.Application.UseCases;

public class ContaMasterUseCase : IContaMasterUseCase
{
    private readonly IClienteRepository _clienteRepository;
    private readonly ICotacaoB3Provider _cotacaoProvider;
    private readonly IUnitOfWork _unitOfWork;

    public ContaMasterUseCase(IClienteRepository clienteRepository, ICotacaoB3Provider cotacaoProvider, IUnitOfWork unitOfWork)
    {
        _clienteRepository = clienteRepository;
        _cotacaoProvider = cotacaoProvider;
        _unitOfWork = unitOfWork;
    }

    /// <summary>
    /// RN-043: Administradores e Robô do Rebalanceamento podem acessar a Custódia Master.
    /// Exibe os resíduos acumulados na conta Master.
    /// </summary>
    public async Task<CarteiraResponse> ObterCustodiaMasterAsync()
    {
        var master = await _clienteRepository.ObterClienteMasterAsync();
        
        if (master == null)
            throw new KeyNotFoundException("A Conta Gráfica Master não foi encontrada no sistema.");

        var cotacoes = _cotacaoProvider.ObterCotacoesDeFechamento().ToDictionary(c => c.Ticker, c => c.PrecoFechamento);

        var ativosDto = master.ContaGrafica.Custodias.Select(c => {
            decimal cotacao = cotacoes.TryGetValue(c.Ticker, out var preco) ? preco : 0m;
            decimal valorAtual = c.Quantidade * cotacao;
            decimal pl = valorAtual - (c.Quantidade * c.PrecoMedio);
            
            return new CustodiaItemDto(
                c.Ticker, 
                c.Quantidade, 
                c.PrecoMedio, 
                valorAtual, 
                pl
            );
        }).ToList();
        
        var custoTotal = ativosDto.Sum(c => c.ValorAtual);

        return new CarteiraResponse(master.Id, master.Nome, master.Cpf, master.Ativo, master.ValorMensal, custoTotal, ativosDto);
    }

    public async Task InjetarMasterAsync()
    {
        var master = await _clienteRepository.ObterClienteMasterAsync();
        if (master != null) 
            return; // Já existe

        var novaMaster = new Cliente("CONTA MASTER ITAÚ", "00000000000", "master@itau.com.br", 0m);
        novaMaster.VincularContaGrafica(new ContaGrafica(null, "MST-000001", TipoContaGrafica.Master));

        await _clienteRepository.AdicionarAsync(novaMaster);
        await _unitOfWork.CommitAsync();
    }
}
