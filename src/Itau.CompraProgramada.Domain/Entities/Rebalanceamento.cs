using System;
using Itau.CompraProgramada.Domain.Enums;

namespace Itau.CompraProgramada.Domain.Entities;

public class Rebalanceamento
{
    public long Id { get; private set; }
    public long ClienteId { get; private set; }
    public TipoRebalanceamento Tipo { get; private set; }
    public string TickerVendido { get; private set; } = string.Empty;
    public string TickerComprado { get; private set; } = string.Empty;
    public decimal ValorVenda { get; private set; }
    public int Quantidade { get; private set; }
    public decimal PrecoMedio { get; private set; }
    public decimal LucroLiquido { get; private set; }
    public DateTime DataRebalanceamento { get; private set; }

    protected Rebalanceamento() { }

    public Rebalanceamento(long clienteId, TipoRebalanceamento tipo, string tickerVendido, string tickerComprado, decimal valorVenda, int quantidade, decimal precoMedio, decimal lucroLiquido)
    {
        ClienteId = clienteId;
        Tipo = tipo;
        TickerVendido = tickerVendido;
        TickerComprado = tickerComprado;
        ValorVenda = valorVenda;
        Quantidade = quantidade;
        PrecoMedio = precoMedio;
        LucroLiquido = lucroLiquido;
        DataRebalanceamento = DateTime.UtcNow;
    }
}