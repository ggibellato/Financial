using System;
using Financial.CashFlow.Domain.Enums;

namespace Financial.CashFlow.Domain.Entities;

public class Expense
{
    public Guid Id { get; private set; }
    public DateOnly Date { get; private set; }
    public string Description { get; private set; } = string.Empty;
    public decimal Value { get; private set; }
    public Category Category { get; private set; }
    public PaymentSource PaymentSource { get; private set; }
    public CreditCard? CardTag { get; private set; }

    private Expense() { }

    public static Expense Create(
        DateOnly date,
        string description,
        decimal value,
        Category category,
        PaymentSource paymentSource,
        CreditCard? cardTag) =>
        new()
        {
            Id = Guid.NewGuid(),
            Date = date,
            Description = description,
            Value = value,
            Category = category,
            PaymentSource = paymentSource,
            CardTag = cardTag
        };

    public void UpdateDetails(
        DateOnly date,
        string description,
        decimal value,
        Category category,
        PaymentSource paymentSource,
        CreditCard? cardTag)
    {
        Date = date;
        Description = description;
        Value = value;
        Category = category;
        PaymentSource = paymentSource;
        CardTag = cardTag;
    }
}
