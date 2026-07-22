using System;
using Financial.CashFlow.Domain.Enums;

namespace Financial.CashFlow.Domain.Entities;

public class MaeLedgerEntry
{
    public Guid Id { get; private set; }
    public DateOnly Date { get; private set; }
    public string Description { get; private set; } = string.Empty;
    public string Note { get; private set; } = string.Empty;
    public Currency SourceCurrency { get; private set; }
    public decimal? BrlValue { get; private set; }
    public decimal? GbpValue { get; private set; }

    private MaeLedgerEntry() { }

    public static MaeLedgerEntry Create(
        DateOnly date,
        string description,
        string note,
        Currency sourceCurrency,
        decimal? brlValue,
        decimal? gbpValue) =>
        new()
        {
            Id = Guid.NewGuid(),
            Date = date,
            Description = description,
            Note = note,
            SourceCurrency = sourceCurrency,
            BrlValue = brlValue,
            GbpValue = gbpValue
        };

    public void UpdateValues(decimal? brlValue, decimal? gbpValue)
    {
        BrlValue = brlValue;
        GbpValue = gbpValue;
    }
}
