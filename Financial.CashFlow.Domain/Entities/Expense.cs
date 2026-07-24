using System;
using Financial.CashFlow.Domain.Enums;

namespace Financial.CashFlow.Domain.Entities;

public class Expense
{
    private const decimal MinRoundUpAmount = 0.00m;
    private const decimal MaxRoundUpAmount = 0.99m;

    public Guid Id { get; private set; }
    public DateOnly Date { get; private set; }
    public string Description { get; private set; } = string.Empty;
    public decimal Value { get; private set; }
    public Category Category { get; private set; }
    public string? PaymentSource { get; private set; }
    public CreditCard? CardTag { get; private set; }
    public DateOnly? SettledAt { get; private set; }
    public decimal? RoundUpAmount { get; private set; }

    public ExpensePaymentStatus PaymentStatus =>
        CardTag is null ? ExpensePaymentStatus.ImmediatePayment
        : PaymentSource is null ? ExpensePaymentStatus.CreditCardCharge
        : ExpensePaymentStatus.CreditCardSettled;

    public decimal RoundUpSuggestion => Math.Ceiling(Value) - Value;

    private Expense() { }

    public static Expense Create(
        DateOnly date,
        string description,
        decimal value,
        Category category,
        string? paymentSource,
        CreditCard? cardTag)
    {
        ValidatePaymentShape(paymentSource, cardTag);

        return new()
        {
            Id = Guid.NewGuid(),
            Date = date,
            Description = description,
            Value = value,
            Category = category,
            PaymentSource = paymentSource,
            CardTag = cardTag
        };
    }

    public void UpdateDetails(
        DateOnly date,
        string description,
        decimal value,
        Category category,
        string? paymentSource,
        CreditCard? cardTag)
    {
        if (PaymentStatus == ExpensePaymentStatus.CreditCardSettled)
        {
            if (paymentSource != PaymentSource || cardTag != CardTag)
            {
                throw new ArgumentException(
                    "A settled expense's payment fields cannot be changed; unmark its card statement paid first.");
            }
        }
        else
        {
            ValidatePaymentShape(paymentSource, cardTag);
            PaymentSource = paymentSource;
            CardTag = cardTag;
        }

        Date = date;
        Description = description;
        Value = value;
        Category = category;
    }

    public void Settle(string paymentSource, DateOnly settledAt)
    {
        if (PaymentStatus != ExpensePaymentStatus.CreditCardCharge)
        {
            throw new ArgumentException("Only an unsettled credit card charge can be settled.");
        }

        PaymentSource = paymentSource;
        SettledAt = settledAt;
    }

    public void Unsettle()
    {
        if (PaymentStatus != ExpensePaymentStatus.CreditCardSettled)
        {
            throw new ArgumentException("Only a settled credit card expense can be unsettled.");
        }

        PaymentSource = null;
        SettledAt = null;
    }

    public void SetRoundUpAmount(decimal? amount)
    {
        if (amount is null)
        {
            RoundUpAmount = null;
            return;
        }

        if (PaymentStatus != ExpensePaymentStatus.ImmediatePayment)
        {
            throw new ArgumentException(
                "Round-up only applies to an expense paid directly from a bank, not a credit-card charge.");
        }

        if (amount < MinRoundUpAmount || amount > MaxRoundUpAmount)
        {
            throw new ArgumentException(
                $"Round-up amount must be between £{MinRoundUpAmount:F2} and £{MaxRoundUpAmount:F2}.");
        }

        RoundUpAmount = amount;
    }

    private static void ValidatePaymentShape(string? paymentSource, CreditCard? cardTag)
    {
        if (paymentSource is null && cardTag is null)
        {
            throw new ArgumentException("An expense requires either a payment source or a card tag.");
        }

        if (paymentSource is not null && cardTag is not null)
        {
            throw new ArgumentException(
                "An expense cannot have both a payment source and a card tag; a settled expense is only produced by marking its card statement paid.");
        }
    }
}
