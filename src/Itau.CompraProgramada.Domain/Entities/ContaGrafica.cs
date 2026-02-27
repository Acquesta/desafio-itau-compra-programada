using System;
using System.Collections.Generic;
using Itau.CompraProgramada.Domain.Enums;

namespace Itau.CompraProgramada.Domain.Entities;

public class ContaGrafica
{
    public long Id { get; private set; }
    public long? ClienteId { get; private set; } // Null para a Conta Master
    public string NumeroConta { get; private set; } = string.Empty;
    public TipoContaGrafica Tipo { get; private set; }
    public DateTime DataCriacao { get; private set; } 

    private readonly List<Custodia> _custodias = new();
    public IReadOnlyCollection<Custodia> Custodias => _custodias.AsReadOnly();

    protected ContaGrafica() { }

    public ContaGrafica(long? clienteId, string numeroConta, TipoContaGrafica tipo)
    {
        ClienteId = clienteId;
        NumeroConta = numeroConta;
        Tipo = tipo;
        DataCriacao = DateTime.UtcNow;
    }

    public void AdicionarCustodia(Custodia custodia)
    {
        _custodias.Add(custodia);
    }
}