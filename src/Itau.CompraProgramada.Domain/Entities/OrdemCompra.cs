using System;
using Itau.CompraProgramada.Domain.Enums;

namespace Itau.CompraProgramada.Domain.Entities;

public class OrdemCompra
{
    public long Id { get; private set; }
    public long ContaMasterId { get; private set; }
    public string Ticker { get; private set; } = string.Empty;
    public int Quantidade { get; private set; }
    public decimal PrecoUnitario { get; private set; }
    public TipoMercado TipoMercado { get; private set; }
    public DateTime DataExecucao { get; private set; }
    
    private readonly List<Distribuicao> _distribuicoes = new();
    public IReadOnlyCollection<Distribuicao> Distribuicoes => _distribuicoes.AsReadOnly();

    protected OrdemCompra() { }

    public OrdemCompra(long contaMasterId, string ticker, int quantidade, decimal precoUnitario, TipoMercado tipoMercado)
    {
        ContaMasterId = contaMasterId;
        Ticker = ticker;
        Quantidade = quantidade;
        PrecoUnitario = precoUnitario;
        TipoMercado = tipoMercado;
        if (tipoMercado == TipoMercado.Fracionario && !ticker.EndsWith("F"))
            Ticker = ticker + "F";
            
        DataExecucao = DateTime.UtcNow;
    }
    
    public void AdicionarDistribuicao(Distribuicao distribuicao)
    {
        _distribuicoes.Add(distribuicao);
    }
}