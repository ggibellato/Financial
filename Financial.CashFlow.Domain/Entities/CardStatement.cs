using System;

namespace Financial.CashFlow.Domain.Entities;

public class CardStatement
{
    private Enums.CreditCard card;

    public Guid Id { get; private set; }
    public Enums.CreditCard Card { get => card; private set => card = value; }
    public int Year { get; private set; }
    public int Month { get; private set; }
    public bool IsPaid { get; private set; }

    private CardStatement() { }

    public static CardStatement Create(Enums.CreditCard card, int year, int month) =>
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

    public void MarkUnpaid()
    {
        IsPaid = false;
    }
}
