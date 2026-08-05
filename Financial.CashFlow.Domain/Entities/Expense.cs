using System;
using Financial.CashFlow.Domain.Enums;
using Financial.CashFlow.Domain.Rules;

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
    public DateOnly? ChargeDate { get; private set; }
    public DateOnly? InvoiceDate { get; private set; }
    public decimal? RoundUpAmount { get; private set; }

    public ExpensePaymentStatus PaymentStatus =>
        CardTag is null ? ExpensePaymentStatus.ImmediatePayment
        : PaymentSource is null ? ExpensePaymentStatus.CreditCardCharge
        : ExpensePaymentStatus.CreditCardSettled;

    public decimal RoundUpSuggestion => Value <= 0 ? 0m : Math.Ceiling(Value) - Value;

    public bool IsInvestment => Category.IsInvestment();

    private Expense() { }

    public static Expense Create(
        DateOnly date,
        string description,
        decimal value,
        Category category,
        string? paymentSource,
        CreditCard? cardTag,
        DateOnly? invoiceDate = null)
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
            CardTag = cardTag,
            ChargeDate = cardTag is not null ? date : null,
            InvoiceDate = cardTag is not null ? FirstOfMonth(invoiceDate ?? date) : null
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

    public void Settle(string paymentSource, DateOnly paymentDate)
    {
        if (PaymentStatus != ExpensePaymentStatus.CreditCardCharge)
        {
            throw new ArgumentException("Only an unsettled credit card charge can be settled.");
        }

        PaymentSource = paymentSource;
        Date = paymentDate;
    }

    public void Unsettle()
    {
        if (PaymentStatus != ExpensePaymentStatus.CreditCardSettled)
        {
            throw new ArgumentException("Only a settled credit card expense can be unsettled.");
        }

        PaymentSource = null;
        Date = ChargeDate!.Value;
    }

    public void SetInvoiceDate(DateOnly invoiceDate)
    {
        if (PaymentStatus != ExpensePaymentStatus.CreditCardCharge)
        {
            throw new ArgumentException(
                "Invoice date can only be changed on an unsettled credit card charge.");
        }

        InvoiceDate = FirstOfMonth(invoiceDate);
    }

    /// <summary>
    /// One-time backfill for a pre-F01 record migrated by ExpenseChargeDateMigrator.
    /// ChargeDate/InvoiceDate are otherwise only ever set at creation; this is the sole other
    /// entry point, and only usable once (while ChargeDate is still unset).
    /// </summary>
    public void MigrateLegacyDates(DateOnly chargeDate, DateOnly invoiceDate, DateOnly? settledDate)
    {
        if (CardTag is null)
        {
            throw new ArgumentException("Legacy date migration only applies to a credit card expense.");
        }

        if (ChargeDate is not null)
        {
            throw new ArgumentException("This expense has already been migrated.");
        }

        if (settledDate is not null && PaymentStatus != ExpensePaymentStatus.CreditCardSettled)
        {
            throw new ArgumentException("A settled date can only be backfilled for an already-settled expense.");
        }

        ChargeDate = chargeDate;
        InvoiceDate = FirstOfMonth(invoiceDate);

        if (settledDate is not null)
        {
            Date = settledDate.Value;
        }
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

        if (Value <= 0)
        {
            throw new ArgumentException("Round-up does not apply to a negative (reimbursement) expense.");
        }

        if (amount < MinRoundUpAmount || amount > MaxRoundUpAmount)
        {
            throw new ArgumentException(
                $"Round-up amount must be between £{MinRoundUpAmount:F2} and £{MaxRoundUpAmount:F2}.");
        }

        RoundUpAmount = amount;
    }

    private static DateOnly FirstOfMonth(DateOnly date) => new(date.Year, date.Month, 1);

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
