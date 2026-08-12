using System;

namespace Financial.CashFlow.Domain.Entities;

public class CardStatement
{
    public Guid Id { get; private set; }
    public CreditCard CreditCard { get; private set; } = null!;
    public int Year { get; private set; }
    public int Month { get; private set; }
    public bool IsPaid { get; private set; }

    private CardStatement() { }

    public static CardStatement Create(CreditCard creditCard, int year, int month) =>
        new()
        {
            Id = Guid.NewGuid(),
            CreditCard = creditCard,
            Year = year,
            Month = month,
            IsPaid = false
        };

    public void MarkPaid()
    {
        IsPaid = true;
    }

    public void MarkUnpaid()
    {
        IsPaid = false;
    }
}
