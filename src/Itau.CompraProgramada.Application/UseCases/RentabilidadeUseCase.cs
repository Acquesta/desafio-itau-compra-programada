using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Itau.CompraProgramada.Application.DTOs;
using Itau.CompraProgramada.Domain.Interfaces;

namespace Itau.CompraProgramada.Application.UseCases;

/// <summary>
/// RN-063 a RN-070: Calcula a rentabilidade da carteira de um cliente.
/// </summary>
public class RentabilidadeUseCase : IRentabilidadeUseCase
{
    private readonly IClienteRepository _clienteRepository;
    private readonly ICotacaoB3Provider _cotacaoProvider;

    public RentabilidadeUseCase(
        IClienteRepository clienteRepository,
        ICotacaoB3Provider cotacaoProvider)
    {
        _clienteRepository = clienteRepository;
        _cotacaoProvider = cotacaoProvider;
    }

    public async Task<RentabilidadeResponse> ObterRentabilidadeAsync(long clienteId)
    {
        var cliente = await _clienteRepository.ObterPorIdComCustodiaAsync(clienteId);
        if (cliente == null)
            throw new KeyNotFoundException($"Cliente {clienteId} não encontrado.");

        if (cliente.ContaGrafica == null || !cliente.ContaGrafica.Custodias.Any())
            return new RentabilidadeResponse(cliente.Id, cliente.Nome, 0, 0, 0, 0, new List<RentabilidadeAtivoResponse>());

        var cotacoes = _cotacaoProvider.ObterCotacoesDeFechamento()
            .ToDictionary(c => c.Ticker, c => c.PrecoFechamento);

        var ativos = new List<RentabilidadeAtivoResponse>();
        decimal valorAtualTotal = 0;
        decimal valorInvestidoTotal = 0;

        foreach (var custodia in cliente.ContaGrafica.Custodias)
        {
            if (custodia.Quantidade == 0) continue;

            decimal cotacaoAtual = cotacoes.TryGetValue(custodia.Ticker, out var preco) ? preco : 0m;
            decimal valorAtual = custodia.Quantidade * cotacaoAtual;
            decimal valorInvestido = custodia.Quantidade * custodia.PrecoMedio;

            // RN-064: P/L por ativo = (Cotacao Atual - Preco Medio) x Quantidade
            decimal plAtivo = (cotacaoAtual - custodia.PrecoMedio) * custodia.Quantidade;

            valorAtualTotal += valorAtual;
            valorInvestidoTotal += valorInvestido;

            ativos.Add(new RentabilidadeAtivoResponse(
                custodia.Ticker,
                custodia.Quantidade,
                custodia.PrecoMedio,
                cotacaoAtual,
                valorAtual,
                plAtivo,
                0 // placeholder, será calculado abaixo
            ));
        }

        // RN-070: Composição percentual de cada ativo
        if (valorAtualTotal > 0)
        {
            ativos = ativos.Select(a => a with
            {
                ComposicaoPercentual = Math.Round((a.ValorAtual / valorAtualTotal) * 100m, 2)
            }).ToList();
        }

        // RN-065: P/L total
        decimal plTotal = valorAtualTotal - valorInvestidoTotal;

        // RN-066: Rentabilidade percentual
        decimal rentabilidade = valorInvestidoTotal > 0
            ? Math.Round(((valorAtualTotal - valorInvestidoTotal) / valorInvestidoTotal) * 100m, 2)
            : 0m;

        return new RentabilidadeResponse(
            cliente.Id,
            cliente.Nome,
            valorInvestidoTotal,
            valorAtualTotal,
            plTotal,
            rentabilidade,
            ativos);
    }
}
