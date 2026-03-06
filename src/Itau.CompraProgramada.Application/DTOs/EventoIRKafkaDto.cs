using System;

namespace Itau.CompraProgramada.Application.DTOs;

/// <summary>
/// RN-056: Formato exato da mensagem Kafka para eventos de IR.
/// </summary>
public record EventoIRKafkaDto(
    long ClienteId,
    string Cpf,
    string Ticker,
    string TipoEvento,
    decimal ValorOperacao,
    decimal ValorIR,
    int Quantidade,
    decimal PrecoUnitario,
    DateTime DataEvento);
