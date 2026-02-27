using Itau.CompraProgramada.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Itau.CompraProgramada.Infrastructure.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    // Mapeamento das Tabelas
    public DbSet<Cliente> Clientes => Set<Cliente>();
    public DbSet<ContaGrafica> ContasGraficas => Set<ContaGrafica>();
    public DbSet<Custodia> Custodias => Set<Custodia>();
    public DbSet<CestaRecomendacao> CestasRecomendacao => Set<CestaRecomendacao>();
    public DbSet<ItemCesta> ItensCesta => Set<ItemCesta>();
    public DbSet<OrdemCompra> OrdensCompra => Set<OrdemCompra>();
    public DbSet<Distribuicao> Distribuicoes => Set<Distribuicao>();
    public DbSet<EventoIR> EventosIR => Set<EventoIR>();
    public DbSet<Rebalanceamento> Rebalanceamentos => Set<Rebalanceamento>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configurações usando Fluent API para garantir integridade estrutural

        modelBuilder.Entity<Cliente>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Cpf).IsUnique(); // RN-002: CPF Único
            entity.Property(e => e.ValorMensal).HasPrecision(18, 2);
            
            // Relacionamento 1:1 entre Cliente e Conta Gráfica Filhote
            entity.HasOne(e => e.ContaGrafica)
                  .WithOne()
                  .HasForeignKey<ContaGrafica>(c => c.ClienteId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<ContaGrafica>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.NumeroConta).IsUnique();
            
            // Relacionamento 1:N entre Conta Gráfica e Custódias
            entity.HasMany(e => e.Custodias)
                  .WithOne()
                  .HasForeignKey(c => c.ContaGraficaId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Custodia>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.PrecoMedio).HasPrecision(18, 4); // 4 casas decimais para precisão de PM
        });

        modelBuilder.Entity<ItemCesta>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Percentual).HasPrecision(5, 2);
        });

        modelBuilder.Entity<OrdemCompra>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.PrecoUnitario).HasPrecision(18, 2);
        });

        modelBuilder.Entity<Distribuicao>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.PrecoUnitario).HasPrecision(18, 2);
        });

        modelBuilder.Entity<EventoIR>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.ValorBase).HasPrecision(18, 2);
            entity.Property(e => e.ValorIR).HasPrecision(18, 2);
        });

        modelBuilder.Entity<Rebalanceamento>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.ValorVenda).HasPrecision(18, 2);
        });
    }
}