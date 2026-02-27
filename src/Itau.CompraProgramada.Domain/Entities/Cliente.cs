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

    // Propriedade de Navegação
    public ContaGrafica ContaGrafica { get; private set; } = null!;

    protected Cliente() { } // Construtor vazio para o EF Core

    public Cliente(string nome, string cpf, string email, decimal valorMensal)
    {
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
            
        ValorMensal = novoValor; // RN-011
    }

    public void SairDoProduto()
    {
        Ativo = false; // RN-007
    }

    public void VincularContaGrafica(ContaGrafica conta)
    {
        ContaGrafica = conta;
    }
}