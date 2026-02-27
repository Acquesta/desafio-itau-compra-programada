namespace Itau.CompraProgramada.Domain.Enums;

public enum TipoContaGrafica
{
    Master,
    Filhote
}

public enum TipoMercado
{
    Lote,
    Fracionario
}

public enum TipoEventoIR
{
    DedoDuro = 1,          // 0,005% sobre o valor da compra
    Venda20Percent = 2     // 20% sobre o lucro em vendas acima de 20 mil
}

public enum TipoRebalanceamento
{
    MudancaCesta,
    Desvio
}