using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Itau.CompraProgramada.Domain.Entities;
using Itau.CompraProgramada.Domain.Interfaces;

namespace Itau.CompraProgramada.Application.UseCases;

public class RebalanceamentoPorMudancaCestaUseCase : IRebalanceamentoPorMudancaCestaUseCase
{
    private readonly IClienteRepository _clienteRepository;
    private readonly ICotacaoB3Provider _cotacaoProvider;
    private readonly IEventoIRPublisher _eventoIRPublisher;
    private readonly IUnitOfWork _unitOfWork;

    public RebalanceamentoPorMudancaCestaUseCase(
        IClienteRepository clienteRepository,
        ICotacaoB3Provider cotacaoProvider,
        IEventoIRPublisher eventoIRPublisher,
        IUnitOfWork unitOfWork)
    {
        _clienteRepository = clienteRepository;
        _cotacaoProvider = cotacaoProvider;
        _eventoIRPublisher = eventoIRPublisher;
        _unitOfWork = unitOfWork;
    }

    public async Task ExecutarRebalanceamentoAsync(CestaRecomendacao cestaAntiga, CestaRecomendacao cestaNova)
    {
        var clientesAtivos = await _clienteRepository.ObterClientesAtivosComCustodiaAsync();
        var cotacoes = _cotacaoProvider.ObterCotacoesDeFechamento().ToDictionary(c => c.Ticker, c => c.PrecoFechamento);

        foreach (var cliente in clientesAtivos)
        {
            if (cliente.ContaGrafica == null) continue;

            decimal totalCarteira = 0;
            foreach (var custodia in cliente.ContaGrafica.Custodias)
            {
                if (cotacoes.TryGetValue(custodia.Ticker, out var preco))
                    totalCarteira += custodia.Quantidade * preco;
            }

            if (totalCarteira == 0) continue;

            // 1. Vender ativos sobre-alocados ou que saíram
            var custodiasAtuais = cliente.ContaGrafica.Custodias.ToList();
            foreach (var custodia in custodiasAtuais)
            {
                var itemNovaCesta = cestaNova.Itens.FirstOrDefault(i => i.Ticker == custodia.Ticker);
                decimal percentualAlvo = itemNovaCesta?.Percentual ?? 0m;

                decimal valorAlvo = totalCarteira * (percentualAlvo / 100m);
                decimal valorAtual = 0;
                
                if (cotacoes.TryGetValue(custodia.Ticker, out var preco))
                    valorAtual = custodia.Quantidade * preco;

                if (valorAtual > valorAlvo && preco > 0)
                {
                    int qtdAlvo = (int)Math.Truncate(valorAlvo / preco);
                    int qtdParaVender = custodia.Quantidade - qtdAlvo;

                    if (qtdParaVender > 0)
                        custodia.RemoverVenda(qtdParaVender);
                }
            }

            // 2. Comprar ativos sub-alocados
            foreach (var itemNovo in cestaNova.Itens)
            {
                var custodia = cliente.ContaGrafica.Custodias.FirstOrDefault(c => c.Ticker == itemNovo.Ticker);
                decimal valorAlvo = totalCarteira * (itemNovo.Percentual / 100m);
                decimal valorAtual = 0;
                decimal preco = cotacoes.TryGetValue(itemNovo.Ticker, out var p) ? p : 0m;
                
                if (custodia != null && preco > 0)
                    valorAtual = custodia.Quantidade * preco;

                if (valorAlvo > valorAtual && preco > 0)
                {
                    int qtdAlvo = (int)Math.Truncate(valorAlvo / preco);
                    int qtdAtual = custodia?.Quantidade ?? 0;
                    int qtdParaComprar = qtdAlvo - qtdAtual;

                    if (qtdParaComprar > 0)
                    {
                        if (custodia == null)
                        {
                            custodia = new Custodia(cliente.ContaGrafica.Id, itemNovo.Ticker, 0, preco);
                            cliente.ContaGrafica.AdicionarCustodia(custodia);
                        }
                        custodia.AdicionarCompra(qtdParaComprar, preco);
                    }
                }
            }
            
            _clienteRepository.Atualizar(cliente);
        }

        await _unitOfWork.CommitAsync();
    }
}
