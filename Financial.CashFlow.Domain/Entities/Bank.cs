using System;

namespace Financial.CashFlow.Domain.Entities;

public class Bank
{
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
}
