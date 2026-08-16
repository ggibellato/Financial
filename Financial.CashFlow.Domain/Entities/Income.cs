using System;

namespace Financial.CashFlow.Domain.Entities;

public class Income
{
    public Guid Id { get; private set; }
    public DateOnly Date { get; private set; }
    public IncomeSource IncomeSource { get; private set; } = null!;
    public decimal? GrossValue { get; private set; }
    public decimal NetValue { get; private set; }
    public Bank? Bank { get; private set; }
    public string? Description { get; private set; }

    private Income() { }

    public static Income Create(
        DateOnly date,
        IncomeSource incomeSource,
        decimal? grossValue,
        decimal netValue,
        Bank? bank,
        string? description = null)
    {
        ValidateValues(grossValue, netValue);
        ValidateIncomeSource(incomeSource);

        return new()
        {
            Id = Guid.NewGuid(),
            Date = date,
            IncomeSource = incomeSource,
            GrossValue = grossValue,
            NetValue = netValue,
            Bank = bank,
            Description = NormalizeDescription(description)
        };
    }

    public void UpdateDetails(
        DateOnly date,
        IncomeSource incomeSource,
        decimal? grossValue,
        decimal netValue,
        Bank? bank,
        string? description = null)
    {
        ValidateValues(grossValue, netValue);
        ValidateIncomeSource(incomeSource);

        Date = date;
        IncomeSource = incomeSource;
        GrossValue = grossValue;
        NetValue = netValue;
        Bank = bank;
        Description = NormalizeDescription(description);
    }

    private static void ValidateValues(decimal? grossValue, decimal netValue)
    {
        if (netValue < 0)
        {
            throw new ArgumentException("Net value cannot be negative.");
        }
    }

    private static void ValidateIncomeSource(IncomeSource incomeSource)
    {
        if (incomeSource is null)
        {
            throw new ArgumentException("Income source is required.");
        }
    }

    private static string? NormalizeDescription(string? description) =>
        string.IsNullOrWhiteSpace(description) ? null : description;
}
