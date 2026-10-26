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
    public DateOnly? DueDate { get; private set; }
    public Guid? PaymentTransferId { get; private set; }
    public bool IsPaidByTransfer => PaymentTransferId is not null;

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

    public bool IsAfter(int year, int month) => (Year, Month).CompareTo((year, month)) > 0;

    public void MarkPaidBy(Guid paymentTransferId)
    {
        IsPaid = true;
        PaymentTransferId = paymentTransferId;
    }

    public void MarkUnpaid()
    {
        IsPaid = false;
        PaymentTransferId = null;
    }

    public void SetDueDate(DateOnly? dueDate)
    {
        DueDate = dueDate;
    }
}
