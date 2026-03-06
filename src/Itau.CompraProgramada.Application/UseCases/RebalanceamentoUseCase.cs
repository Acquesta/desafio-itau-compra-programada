using System;
using System.Linq;
using System.Threading.Tasks;
using Itau.CompraProgramada.Domain.Entities;
using Itau.CompraProgramada.Domain.Enums;
using Itau.CompraProgramada.Domain.Interfaces;
using Itau.CompraProgramada.Domain.Services;

namespace Itau.CompraProgramada.Application.UseCases;

public class RebalanceamentoUseCase : IRebalanceamentoUseCase
{
    private readonly IClienteRepository _clienteRepository;
    private readonly IEventoIRPublisher _eventoIRPublisher;
    private readonly CalculoIRService _calculoIRService;
    private readonly IUnitOfWork _unitOfWork;

    public RebalanceamentoUseCase(
        IClienteRepository clienteRepository,
        IEventoIRPublisher eventoIRPublisher,
        CalculoIRService calculoIRService,
        IUnitOfWork unitOfWork)
    {
        _clienteRepository = clienteRepository;
        _eventoIRPublisher = eventoIRPublisher;
        _calculoIRService = calculoIRService;
        _unitOfWork = unitOfWork;
    }

    public async Task<string> ExecutarVendaRebalanceamentoAsync(long clienteId, string ticker, int quantidade, decimal precoVendaAtual)
    {
        var cliente = await _clienteRepository.ObterPorIdComCustodiaAsync(clienteId);
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

        // 3. RN-057 a RN-061: Verificação fiscal usando CalculoIRService
        decimal valorIR = _calculoIRService.CalcularIRSobreVendas(valorTotalVenda, lucro);

        if (valorIR > 0)
        {
            var eventoIR = new EventoIR(
                cliente.Id, cliente.Cpf, ticker, TipoEventoIR.Venda20Percent,
                lucro, valorIR, quantidade, precoVendaAtual);
            await _eventoIRPublisher.PublicarEventoAsync(eventoIR);

            mensagemRetorno += $" IR (20% sobre lucro) de R$ {valorIR:N2} publicado no Kafka.";
        }
        else if (lucro > 0)
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