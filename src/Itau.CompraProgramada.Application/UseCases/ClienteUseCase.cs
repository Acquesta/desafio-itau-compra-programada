using Itau.CompraProgramada.Application.DTOs;
using Itau.CompraProgramada.Domain.Entities;
using Itau.CompraProgramada.Domain.Enums;
using Itau.CompraProgramada.Domain.Interfaces;

namespace Itau.CompraProgramada.Application.UseCases;

public class ClienteUseCase : IClienteUseCase
{
    private readonly IClienteRepository _clienteRepository;
    private readonly IUnitOfWork _unitOfWork;

    public ClienteUseCase(IClienteRepository clienteRepository, IUnitOfWork unitOfWork)
    {
        _clienteRepository = clienteRepository;
        _unitOfWork = unitOfWork;
    }

    /// <summary>
    /// RN-001 a RN-006: Adesão do cliente ao produto.
    /// </summary>
    public async Task<AdesaoResponse> AderirAsync(AdesaoRequest request)
    {
        // RN-002: CPF único
        if (await _clienteRepository.ExisteCpfAsync(request.Cpf))
            throw new InvalidOperationException("CLIENTE_CPF_DUPLICADO");

        // RN-001 + RN-003: Criação do cliente (validações no construtor da entidade)
        var cliente = new Cliente(request.Nome, request.Cpf, request.Email, request.ValorMensal);

        // RN-004: Criar Conta Gráfica Filhote automaticamente
        var numeroConta = $"FLH-{DateTime.UtcNow:yyyyMMddHHmmss}-{request.Cpf[..4]}";
        var contaGrafica = new ContaGrafica(null, numeroConta, TipoContaGrafica.Filhote);
        cliente.VincularContaGrafica(contaGrafica);

        await _clienteRepository.AdicionarAsync(cliente);
        await _unitOfWork.CommitAsync();

        return new AdesaoResponse(
            cliente.Id,
            cliente.Nome,
            cliente.Cpf,
            cliente.Email,
            cliente.ValorMensal,
            cliente.Ativo,
            cliente.DataAdesao,
            new ContaGraficaDto(contaGrafica.Id, contaGrafica.NumeroConta, contaGrafica.Tipo.ToString())
        );
    }

    /// <summary>
    /// RN-007 a RN-010: Saída do cliente do produto.
    /// </summary>
    public async Task SairAsync(long clienteId)
    {
        var cliente = await _clienteRepository.ObterPorIdAsync(clienteId)
            ?? throw new KeyNotFoundException("CLIENTE_NAO_ENCONTRADO");

        cliente.SairDoProduto(); // RN-007: Ativo = false + DataSaida

        _clienteRepository.Atualizar(cliente);
        await _unitOfWork.CommitAsync();
    }

    /// <summary>
    /// RN-011 a RN-013: Alteração do valor mensal com histórico.
    /// </summary>
    public async Task AlterarValorMensalAsync(long clienteId, AlterarValorRequest request)
    {
        var cliente = await _clienteRepository.ObterPorIdAsync(clienteId)
            ?? throw new KeyNotFoundException("CLIENTE_NAO_ENCONTRADO");

        cliente.AlterarValorMensal(request.NovoValorMensal); // RN-011 + RN-013

        _clienteRepository.Atualizar(cliente);
        await _unitOfWork.CommitAsync();
    }

    /// <summary>
    /// RN-010 + preview RN-063 a RN-070: Consulta da carteira do cliente.
    /// </summary>
    public async Task<CarteiraResponse> ObterCarteiraAsync(long clienteId)
    {
        var cliente = await _clienteRepository.ObterPorIdComCustodiaAsync(clienteId)
            ?? throw new KeyNotFoundException("CLIENTE_NAO_ENCONTRADO");

        var ativos = new List<CustodiaItemDto>();
        decimal saldoTotal = 0;

        if (cliente.ContaGrafica?.Custodias != null)
        {
            foreach (var custodia in cliente.ContaGrafica.Custodias)
            {
                // TODO: Buscar cotação atual do COTAHIST para calcular P/L real
                // Por agora, usar preço médio como valor atual (P/L = 0)
                var valorAtual = custodia.PrecoMedio;
                var pl = (valorAtual - custodia.PrecoMedio) * custodia.Quantidade;
                saldoTotal += valorAtual * custodia.Quantidade;

                ativos.Add(new CustodiaItemDto(
                    custodia.Ticker,
                    custodia.Quantidade,
                    custodia.PrecoMedio,
                    valorAtual,
                    pl
                ));
            }
        }

        return new CarteiraResponse(
            cliente.Id,
            cliente.Nome,
            cliente.Cpf,
            cliente.Ativo,
            cliente.ValorMensal,
            saldoTotal,
            ativos
        );
    }
}
