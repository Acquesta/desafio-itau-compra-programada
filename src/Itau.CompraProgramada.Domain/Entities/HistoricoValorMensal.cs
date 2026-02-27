namespace Itau.CompraProgramada.Domain.Entities;

/// <summary>
/// Entidade para rastreamento de mudanças no valor de aporte mensal (RN-013).
/// </summary>
public class HistoricoValorMensal
{
    public long Id { get; private set; }
    public long ClienteId { get; private set; }
    public decimal ValorAnterior { get; private set; }
    public decimal ValorNovo { get; private set; }
    public DateTime DataAlteracao { get; private set; }

    protected HistoricoValorMensal() { } // EF Core

    public HistoricoValorMensal(long clienteId, decimal valorAnterior, decimal valorNovo)
    {
        ClienteId = clienteId;
        ValorAnterior = valorAnterior;
        ValorNovo = valorNovo;
        DataAlteracao = DateTime.UtcNow;
    }
}
