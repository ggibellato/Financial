using System;

namespace Financial.CashFlow.Domain.Entities;

public class Income
{
    public Guid Id { get; private set; }
    public DateOnly Date { get; private set; }
    public IncomeSource IncomeSource { get; private set; } = null!;
    public decimal? GrossValue { get; private set; }
    public decimal NetValue { get; private set; }
    public Bank Bank { get; private set; } = null!;

    private Income() { }

    public static Income Create(
        DateOnly date,
        IncomeSource incomeSource,
        decimal? grossValue,
        decimal netValue,
        Bank bank)
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
        IncomeSource incomeSource,
        decimal? grossValue,
        decimal netValue,
        Bank bank)
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

    private static void ValidateIncomeSource(IncomeSource incomeSource)
    {
        if (incomeSource is null)
        {
            throw new ArgumentException("Income source is required.");
        }
    }

    private static void ValidateBank(Bank bank)
    {
        if (bank is null)
        {
            throw new ArgumentException("Bank is required.");
        }
    }
}
