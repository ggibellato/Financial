using System;

namespace Financial.CashFlow.Domain.Entities;

public class Transfer
{
    public Guid Id { get; private set; }
    public DateOnly Date { get; private set; }
    public string SourceBank { get; private set; } = string.Empty;
    public string DestinationBank { get; private set; } = string.Empty;
    public decimal Amount { get; private set; }
    public string? Note { get; private set; }

    private Transfer() { }

    public static Transfer Create(
        DateOnly date,
        string sourceBank,
        string destinationBank,
        decimal amount,
        string? note)
    {
        Validate(sourceBank, destinationBank, amount);

        return new()
        {
            Id = Guid.NewGuid(),
            Date = date,
            SourceBank = sourceBank,
            DestinationBank = destinationBank,
            Amount = amount,
            Note = note
        };
    }

    public void UpdateDetails(
        DateOnly date,
        string sourceBank,
        string destinationBank,
        decimal amount,
        string? note)
    {
        Validate(sourceBank, destinationBank, amount);

        Date = date;
        SourceBank = sourceBank;
        DestinationBank = destinationBank;
        Amount = amount;
        Note = note;
    }

    private static void Validate(string sourceBank, string destinationBank, decimal amount)
    {
        if (string.Equals(sourceBank, destinationBank, StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException("A transfer must move money between two different banks.");
        }

        if (amount <= 0)
        {
            throw new ArgumentException("Transfer amount must be greater than zero.");
        }
    }
}
