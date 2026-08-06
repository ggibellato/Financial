using System;

namespace Financial.CashFlow.Domain.Entities;

public class Income
{
    public Guid Id { get; private set; }
    public DateOnly Date { get; private set; }
    public string IncomeSource { get; private set; } = string.Empty;
    public decimal? GrossValue { get; private set; }
    public decimal NetValue { get; private set; }
    public string Bank { get; private set; } = string.Empty;

    private Income() { }

    public static Income Create(
        DateOnly date,
        string incomeSource,
        decimal? grossValue,
        decimal netValue,
        string bank)
    {
        ValidateValues(grossValue, netValue);
        ValidateIncomeSource(incomeSource);
        ValidateBank(bank);

        return new()
        {
            Id = Guid.NewGuid(),
            Date = date,
            IncomeSource = incomeSource,
            GrossValue = grossValue,
            NetValue = netValue,
            Bank = bank
        };
    }

    public void UpdateDetails(
        DateOnly date,
        string incomeSource,
        decimal? grossValue,
        decimal netValue,
        string bank)
    {
        ValidateValues(grossValue, netValue);
        ValidateIncomeSource(incomeSource);
        ValidateBank(bank);

        Date = date;
        IncomeSource = incomeSource;
        GrossValue = grossValue;
        NetValue = netValue;
        Bank = bank;
    }

    private static void ValidateValues(decimal? grossValue, decimal netValue)
    {
        if (netValue < 0)
        {
            throw new ArgumentException("Net value cannot be negative.");
        }
    }

    private static void ValidateIncomeSource(string incomeSource)
    {
        if (string.IsNullOrWhiteSpace(incomeSource))
        {
            throw new ArgumentException("Income source is required.");
        }
    }

    private static void ValidateBank(string bank)
    {
        if (string.IsNullOrWhiteSpace(bank))
        {
            throw new ArgumentException("Bank is required.");
        }
    }
}
