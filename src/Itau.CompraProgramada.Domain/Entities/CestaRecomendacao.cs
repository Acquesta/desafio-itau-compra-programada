using System;
using System.Collections.Generic;
using System.Linq;

namespace Itau.CompraProgramada.Domain.Entities;

public class CestaRecomendacao
{
    public long Id { get; private set; }
    public string Nome { get; private set; } = string.Empty;
    public bool Ativa { get; private set; } 
    public DateTime DataCriacao { get; private set; }
    public DateTime? DataDesativacao { get; private set; }

    private readonly List<ItemCesta> _itens = new();
    public IReadOnlyCollection<ItemCesta> Itens => _itens.AsReadOnly();

    protected CestaRecomendacao() { }

    public CestaRecomendacao(string nome, List<ItemCesta> itens)
    {
        if (itens == null || itens.Count != 5) // RN-014
            throw new ArgumentException("A cesta deve conter exatamente 5 ações.");
            
        if (itens.Sum(i => i.Percentual) != 100m) // RN-015
            throw new ArgumentException("A soma dos percentuais deve ser exatamente 100%.");

        Nome = nome;
        Ativa = true;
        DataCriacao = DateTime.UtcNow;
        _itens = itens;
    }

    public void Desativar()
    {
        Ativa = false;
        DataDesativacao = DateTime.UtcNow; // RN-017
    }
}