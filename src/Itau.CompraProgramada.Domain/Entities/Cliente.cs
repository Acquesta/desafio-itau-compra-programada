using System;
using System.Collections.Generic;

namespace Itau.CompraProgramada.Domain.Entities;

public class Cliente
{
    public long Id { get; private set; }
    public string Nome { get; private set; } = string.Empty;
    public string Cpf { get; private set; } = string.Empty;
    public string Email { get; private set; } = string.Empty;
    public decimal ValorMensal { get; private set; }  
    public bool Ativo { get; private set; } 
    public DateTime DataAdesao { get; private set; }
    public DateTime? DataSaida { get; private set; } // RN-007

    // Propriedades de Navegação
    public ContaGrafica ContaGrafica { get; private set; } = null!;
    
    private readonly List<HistoricoValorMensal> _historicoValores = new();
    public IReadOnlyCollection<HistoricoValorMensal> HistoricoValores => _historicoValores.AsReadOnly();

    protected Cliente() { } // Construtor vazio para o EF Core

    public Cliente(string nome, string cpf, string email, decimal valorMensal)
    {
        if (string.IsNullOrWhiteSpace(nome))
            throw new ArgumentException("Nome é obrigatório.");

        if (string.IsNullOrWhiteSpace(cpf) || cpf.Length != 11)
            throw new ArgumentException("CPF deve conter 11 dígitos.");

        if (string.IsNullOrWhiteSpace(email))
            throw new ArgumentException("Email é obrigatório.");

        if (valorMensal < 100) 
            throw new ArgumentException("O valor mensal mínimo é de R$ 100,00."); // RN-003

        Nome = nome;
        Cpf = cpf;
        Email = email;
        ValorMensal = valorMensal;
        Ativo = true; // RN-005
        DataAdesao = DateTime.UtcNow; // RN-006
    }

    public void AlterarValorMensal(decimal novoValor)
    {
        if (novoValor < 100) 
            throw new ArgumentException("O valor mensal mínimo é de R$ 100,00.");

        if (!Ativo)
            throw new InvalidOperationException("Cliente inativo não pode alterar valor mensal.");

        // RN-013: Registrar histórico antes de alterar
        _historicoValores.Add(new HistoricoValorMensal(Id, ValorMensal, novoValor));
            
        ValorMensal = novoValor; // RN-011
    }

    public void SairDoProduto()
    {
        if (!Ativo)
            throw new InvalidOperationException("Cliente já está inativo.");

        Ativo = false; // RN-007
        DataSaida = DateTime.UtcNow;
    }

    public void VincularContaGrafica(ContaGrafica conta)
    {
        ContaGrafica = conta;
    }
}