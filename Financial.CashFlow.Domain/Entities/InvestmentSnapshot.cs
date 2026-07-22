using System;
using Financial.CashFlow.Domain.Enums;

namespace Financial.CashFlow.Domain.Entities;

public class InvestmentSnapshot
{
    public Guid Id { get; private set; }
    public InvestmentAccount Account { get; private set; }
    public int Year { get; private set; }
    public int Month { get; private set; }
    public decimal Value { get; private set; }

    private InvestmentSnapshot() { }

    public static InvestmentSnapshot Create(InvestmentAccount account, int year, int month, decimal value) =>
        new()
        {
            Id = Guid.NewGuid(),
            Account = account,
            Year = year,
            Month = month,
            Value = value
        };

    public void Update(decimal value)
    {
        Value = value;
    }
}
