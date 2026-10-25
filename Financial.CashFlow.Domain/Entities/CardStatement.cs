using System;
using Financial.CashFlow.Domain.Rules;

namespace Financial.CashFlow.Domain.Entities;

public class CardStatement
{
    public Guid Id { get; private set; }
    public Account CreditCard { get; private set; } = null!;
    public int Year { get; private set; }
    public int Month { get; private set; }
    public bool IsPaid { get; private set; }

    private CardStatement() { }

    public static CardStatement Create(Account creditCard, int year, int month)
    {
        CardAccountRule.EnsureIsCard(creditCard);
        return new()
        {
            Id = Guid.NewGuid(),
            CreditCard = creditCard,
            Year = year,
            Month = month,
            IsPaid = false
        };
    }

    public bool IsFor(Guid creditCardId, int year, int month) =>
        CreditCard.Id == creditCardId && Year == year && Month == month;

    public void MarkPaid()
    {
        IsPaid = true;
    }

    public void MarkUnpaid()
    {
        IsPaid = false;
    }
}
