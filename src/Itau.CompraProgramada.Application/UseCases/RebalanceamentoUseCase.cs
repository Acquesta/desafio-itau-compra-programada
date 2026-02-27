using System;
using System.Linq;
using System.Threading.Tasks;
using Itau.CompraProgramada.Domain.Entities;
using Itau.CompraProgramada.Domain.Enums;
using Itau.CompraProgramada.Domain.Interfaces;

namespace Itau.CompraProgramada.Application.UseCases;

public class RebalanceamentoUseCase : IRebalanceamentoUseCase
{
    private readonly IClienteRepository _clienteRepository;
    private readonly IEventoIRPublisher _eventoIRPublisher;
    private readonly IUnitOfWork _unitOfWork;

    public RebalanceamentoUseCase(
        IClienteRepository clienteRepository,
        IEventoIRPublisher eventoIRPublisher,
        IUnitOfWork unitOfWork)
    {
        _clienteRepository = clienteRepository;
        _eventoIRPublisher = eventoIRPublisher;
        _unitOfWork = unitOfWork;
    }

    public async Task<string> ExecutarVendaRebalanceamentoAsync(long clienteId, string ticker, int quantidade, decimal precoVendaAtual)
    {
        var cliente = await _clienteRepository.ObterPorIdAsync(clienteId);
        if (cliente == null) throw new InvalidOperationException("Cliente não encontrado.");

        var custodia = cliente.ContaGrafica.Custodias.FirstOrDefault(c => c.Ticker == ticker);
        if (custodia == null || custodia.Quantidade < quantidade)
            throw new InvalidOperationException($"Saldo insuficiente na custódia de {ticker}.");

        // 1. Cálculos Financeiros da Operação
        decimal valorTotalVenda = quantidade * precoVendaAtual;
        
        // Lucro = (Preço de Venda - Preço Médio de Compra) * Quantidade
        decimal lucro = (precoVendaAtual - custodia.PrecoMedio) * quantidade;

        // 2. Atualizar a Custódia (Subtrair as ações vendidas)
        // O Preço Médio NÃO muda na venda, apenas a quantidade!
        custodia.RemoverVenda(quantidade);

        string mensagemRetorno = $"Venda de {quantidade} {ticker} executada. Valor total: R$ {valorTotalVenda:N2}.";

        // 3. Regra de Negócio: Verificação da Isenção de R$ 20.000,00
        // Em um cenário real, somaríamos todas as vendas do mês. Aqui avaliamos a operação.
        if (valorTotalVenda > 20000m && lucro > 0)
        {
            // O cliente lucrou e passou do limite de isenção! Imposto de 20% sobre o LUCRO.
            decimal valorImposto = Math.Round(lucro * 0.20m, 2);
            
            var eventoIR = new EventoIR(cliente.Id, TipoEventoIR.Venda20Percent, lucro, valorImposto);
            await _eventoIRPublisher.PublicarEventoAsync(eventoIR);

            mensagemRetorno += $" Venda ultrapassou limite de isenção. Evento de IR (20% sobre lucro) gerado no valor de R$ {valorImposto:N2}.";
        }
        else if (valorTotalVenda <= 20000m && lucro > 0)
        {
            mensagemRetorno += " Venda isenta de IR (abaixo de R$ 20.000,00 no mês).";
        }
        else
        {
            mensagemRetorno += " Venda executada com prejuízo. Nenhum IR retido.";
        }

        // 4. Salvar tudo
        _clienteRepository.Atualizar(cliente);
        await _unitOfWork.CommitAsync();

        return mensagemRetorno;
    }
}