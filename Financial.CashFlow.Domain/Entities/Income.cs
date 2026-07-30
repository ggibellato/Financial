using System;
using Financial.CashFlow.Domain.Enums;
using Financial.CashFlow.Domain.Rules;

namespace Financial.CashFlow.Domain.Entities;

public class Income
{
    public Guid Id { get; private set; }
    public DateOnly Date { get; private set; }
    public IncomeSource IncomeSource { get; private set; }
    public decimal? GrossValue { get; private set; }
    public decimal NetValue { get; private set; }
    public string Bank { get; private set; } = string.Empty;

    public IncomeGroup Group => IncomeClassifier.Classify(IncomeSource);

    private Income() { }

    public static Income Create(
        DateOnly date,
        IncomeSource incomeSource,
        decimal? grossValue,
        decimal netValue,
        string bank)
    {
        ValidateValues(grossValue, netValue);
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
        string bank)
    {
        ValidateValues(grossValue, netValue);
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

    private static void ValidateBank(string bank)
    {
        if (string.IsNullOrWhiteSpace(bank))
        {
            throw new ArgumentException("Bank is required.");
        }
    }
}
