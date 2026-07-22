using System;
using Financial.CashFlow.Domain.Enums;

namespace Financial.CashFlow.Domain.Entities;

public class CardStatement
{
    public Guid Id { get; private set; }
    public CreditCard Card { get; private set; }
    public int Year { get; private set; }
    public int Month { get; private set; }
    public bool IsPaid { get; private set; }

    private CardStatement() { }

    public static CardStatement Create(CreditCard card, int year, int month) =>
        new()
        {
            Id = Guid.NewGuid(),
            Card = card,
            Year = year,
            Month = month,
            IsPaid = false
        };

    public void MarkPaid()
    {
        IsPaid = true;
    }
}
