using Itau.CompraProgramada.Application.DTOs;
using Itau.CompraProgramada.Domain.Interfaces;

namespace Itau.CompraProgramada.Application.UseCases;

public class ContaMasterUseCase : IContaMasterUseCase
{
    private readonly IClienteRepository _clienteRepository;

    public ContaMasterUseCase(IClienteRepository clienteRepository)
    {
        _clienteRepository = clienteRepository;
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

        var custoTotal = master.ContaGrafica.Custodias.Sum(c => c.Quantidade * c.PrecoMedio);
        
        var ativosDto = master.ContaGrafica.Custodias.Select(c => new CustodiaItemDto(
            c.Ticker, 
            c.Quantidade, 
            c.PrecoMedio, 
            c.Quantidade * c.PrecoMedio, 
            0m // Para simplificar, não calculamos P/L na master.
        )).ToList();

        return new CarteiraResponse(master.Id, master.Nome, master.Cpf, master.Ativo, master.ValorMensal, custoTotal, ativosDto);
    }
}
