using System;
using System.Collections.Generic;

namespace Itau.CompraProgramada.Application.DTOs;

/// <summary>
/// RN-063 a RN-070: Response da tela de rentabilidade.
/// </summary>
public record RentabilidadeResponse(
    long ClienteId,
    string Nome,
    decimal ValorInvestidoTotal,
    decimal ValorAtualTotal,
    decimal PLTotal,
    decimal RentabilidadePercentual,
    List<RentabilidadeAtivoResponse> Ativos);

public record RentabilidadeAtivoResponse(
    string Ticker,
    int Quantidade,
    decimal PrecoMedio,
    decimal CotacaoAtual,
    decimal ValorAtual,
    decimal PL,
    decimal ComposicaoPercentual);
