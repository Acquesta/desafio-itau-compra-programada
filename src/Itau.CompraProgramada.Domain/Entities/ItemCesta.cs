using System;
using System.Collections.Generic;
using System.Linq;

namespace Itau.CompraProgramada.Domain.Entities;

public class ItemCesta
{
    public long Id { get; private set; }
    public long CestaId { get; private set; }
    public string Ticker { get; private set; } = string.Empty;
    public decimal Percentual { get; private set; }

    protected ItemCesta() { }

    public ItemCesta(string ticker, decimal percentual)
    {
        if (percentual <= 0) // RN-016
            throw new ArgumentException("O percentual deve ser maior que 0%.");

        Ticker = ticker;
        Percentual = percentual;
    }
}