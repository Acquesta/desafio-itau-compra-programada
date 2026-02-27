using System;

namespace Itau.CompraProgramada.Domain.Entities;

public class Custodia
{
    public long Id { get; private set; }
    public long ContaGraficaId { get; private set; }
    public string Ticker { get; private set; } = string.Empty;
    public int Quantidade { get; private set; }
    public decimal PrecoMedio { get; private set; }
    public DateTime DataUltimaAtualizacao { get; private set; }

    protected Custodia() { }

    public Custodia(long contaGraficaId, string ticker, int quantidadeInicial, decimal precoInicial)
    {
        ContaGraficaId = contaGraficaId;
        Ticker = ticker;
        Quantidade = quantidadeInicial;
        PrecoMedio = precoInicial;
        DataUltimaAtualizacao = DateTime.UtcNow;
    }

    // Regra RN-042 - Recalcular Preço Médio na Compra
    public void AdicionarCompra(int quantidadeNova, decimal precoNovo)
    {
        if (quantidadeNova <= 0) return;

        // PM = (Qtd Anterior x PM Anterior + Qtd Nova x Preco Nova) / (Qtd Anterior + Qtd Nova)
        var valorTotalAnterior = Quantidade * PrecoMedio;
        var valorTotalNovo = quantidadeNova * precoNovo;
        
        var novaQuantidadeTotal = Quantidade + quantidadeNova;
        
        PrecoMedio = (valorTotalAnterior + valorTotalNovo) / novaQuantidadeTotal;
        Quantidade = novaQuantidadeTotal;
        DataUltimaAtualizacao = DateTime.UtcNow;
    }

    public void AtualizarQuantidadeVenda(int quantidadeVendida)
    {
        Quantidade -= quantidadeVendida;
    }

    // Regra RN-043 - Em caso de venda, o preço médio NÃO se altera
    public void RemoverVenda(int quantidadeVenda)
    {
        if (quantidadeVenda > Quantidade)
            throw new InvalidOperationException("Quantidade de venda maior que o saldo em custódia.");

        Quantidade -= quantidadeVenda;
        DataUltimaAtualizacao = DateTime.UtcNow;
    }
}