using System;
using Itau.CompraProgramada.Domain.Enums;

namespace Itau.CompraProgramada.Domain.Entities;

public class EventoIR
{
    public long Id { get; private set; }
    public long ClienteId { get; private set; }
    public string Cpf { get; private set; } = string.Empty;
    public string Ticker { get; private set; } = string.Empty;
    public TipoEventoIR Tipo { get; private set; }
    public decimal ValorBase { get; private set; }
    public decimal ValorIR { get; private set; }
    public int Quantidade { get; private set; }
    public decimal PrecoUnitario { get; private set; }
    public bool PublicadoKafka { get; private set; }
    public DateTime DataEvento { get; private set; }

    protected EventoIR() { }

    /// <summary>
    /// Construtor legado (mantido para compatibilidade).
    /// </summary>
    public EventoIR(long clienteId, TipoEventoIR tipo, decimal valorBase, decimal valorIR)
    {
        ClienteId = clienteId;
        Tipo = tipo;
        ValorBase = valorBase;
        ValorIR = valorIR;
        PublicadoKafka = false;
        DataEvento = DateTime.UtcNow;
    }

    /// <summary>
    /// Construtor completo (RN-056) com CPF, Ticker, Quantidade e PrecoUnitario.
    /// </summary>
    public EventoIR(long clienteId, string cpf, string ticker, TipoEventoIR tipo,
        decimal valorBase, decimal valorIR, int quantidade, decimal precoUnitario)
    {
        ClienteId = clienteId;
        Cpf = cpf;
        Ticker = ticker;
        Tipo = tipo;
        ValorBase = valorBase;
        ValorIR = valorIR;
        Quantidade = quantidade;
        PrecoUnitario = precoUnitario;
        PublicadoKafka = false;
        DataEvento = DateTime.UtcNow;
    }

    public void MarcarComoPublicado()
    {
        PublicadoKafka = true;
    }
}