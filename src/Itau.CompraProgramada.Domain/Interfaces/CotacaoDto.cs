namespace Itau.CompraProgramada.Domain.Interfaces;

/// <summary>
/// DTO simples para transitar os dados de cotação da B3.
/// Definido no Domain junto à interface que o utiliza.
/// </summary>
public class CotacaoDto
{
    public string Ticker { get; set; } = string.Empty;
    public decimal PrecoFechamento { get; set; }
}
