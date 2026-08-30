using System;

namespace Financial.CashFlow.Domain.Entities;

public class Bank
{
    public Guid Id { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public bool RoundUpEnabled { get; private set; }
    public decimal OpeningBalance { get; private set; }
    public DateOnly OpeningBalanceDate { get; private set; }

    private Bank() { }

    public static Bank Create(string name, bool roundUpEnabled)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Bank name is required.");
        }

        return new()
        {
            Id = Guid.NewGuid(),
            Name = name,
            RoundUpEnabled = roundUpEnabled
        };
    }

    public void SetOpeningBalance(decimal openingBalance, DateOnly openingBalanceDate)
    {
        if (openingBalance < 0)
        {
            throw new ArgumentException("Opening balance cannot be negative.");
        }

        OpeningBalance = openingBalance;
        OpeningBalanceDate = openingBalanceDate;
    }

    /// <summary>Updates this bank's identity fields. Callers own uniqueness checks, since only the
    /// repository can see across every bank.</summary>
    public void Update(string name, bool roundUpEnabled)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Bank name is required.");
        }

        Name = name;
        RoundUpEnabled = roundUpEnabled;
    }
}
