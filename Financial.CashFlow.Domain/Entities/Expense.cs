using System;
using Financial.CashFlow.Domain.Enums;

namespace Financial.CashFlow.Domain.Entities;

public class Expense
{
    public const decimal MinRoundUpAmount = 0.00m;
    public const decimal MaxRoundUpAmount = 0.99m;

    public Guid Id { get; private set; }
    public DateOnly Date { get; private set; }
    public string Description { get; private set; } = string.Empty;
    public decimal Value { get; private set; }
    public Category Category { get; private set; } = null!;
    public Bank? PaymentSourceBank { get; private set; }
    public CreditCard? CreditCard { get; private set; }
    public DateOnly? ChargeDate { get; private set; }
    public DateOnly? InvoiceDate { get; private set; }
    public decimal? RoundUpAmount { get; private set; }
    public bool CountsAsTithe { get; private set; } = true;

    public ExpensePaymentStatus PaymentStatus =>
        CreditCard is null ? ExpensePaymentStatus.ImmediatePayment
        : PaymentSourceBank is null ? ExpensePaymentStatus.CreditCardCharge
        : ExpensePaymentStatus.CreditCardSettled;

    public decimal RoundUpSuggestion => Value <= 0 ? 0m : Math.Ceiling(Value) - Value;

    public bool IsInvestment => Category.IsInvestment;

    /// <summary>
    /// The month/year this expense's value counts toward in reporting: an unpaid credit card
    /// charge counts toward its assigned invoice period, while a settled charge or a bank
    /// expense counts toward its (post-settlement, for a charge) <see cref="Date"/>. Falls back to
    /// <see cref="Date"/> for a legacy pre-migration record that hasn't had its invoice date
    /// backfilled yet (see <see cref="MigrateLegacyDates"/>).
    /// </summary>
    public DateOnly ReportingDate =>
        PaymentStatus == ExpensePaymentStatus.CreditCardCharge && InvoiceDate is not null
            ? InvoiceDate.Value
            : Date;

    private Expense() { }

    public static Expense Create(
        DateOnly date,
        string description,
        decimal value,
        Category category,
        Bank? paymentSourceBank,
        CreditCard? creditCard,
        DateOnly? invoiceDate = null,
        bool countsAsTithe = true)
    {
        ValidateFields(description, value);
        ValidatePaymentShape(paymentSourceBank, creditCard);

        return new()
        {
            Id = Guid.NewGuid(),
            Date = date,
            Description = description,
            Value = value,
            Category = category,
            PaymentSourceBank = paymentSourceBank,
            CreditCard = creditCard,
            ChargeDate = creditCard is not null ? date : null,
            InvoiceDate = creditCard is not null ? FirstOfMonth(invoiceDate ?? date) : null,
            CountsAsTithe = countsAsTithe
        };
    }

    public void UpdateDetails(
        DateOnly date,
        string description,
        decimal value,
        Category category,
        Bank? paymentSourceBank,
        CreditCard? creditCard,
        bool countsAsTithe = true)
    {
        ValidateFields(description, value);

        if (PaymentStatus == ExpensePaymentStatus.CreditCardSettled)
        {
            if (paymentSourceBank?.Id != PaymentSourceBank?.Id || creditCard?.Id != CreditCard?.Id)
            {
                throw new ArgumentException(
                    "A settled expense's payment fields cannot be changed; unmark its card statement paid first.");
            }
        }
        else
        {
            ValidatePaymentShape(paymentSourceBank, creditCard);
            PaymentSourceBank = paymentSourceBank;
            CreditCard = creditCard;
        }

        Date = date;
        Description = description;
        Value = value;
        Category = category;
        CountsAsTithe = countsAsTithe;
    }

    public void Settle(Bank paymentSourceBank, DateOnly paymentDate)
    {
        if (PaymentStatus != ExpensePaymentStatus.CreditCardCharge)
        {
            throw new ArgumentException("Only an unsettled credit card charge can be settled.");
        }

        PaymentSourceBank = paymentSourceBank;
        Date = paymentDate;
    }

    public void Unsettle()
    {
        if (PaymentStatus != ExpensePaymentStatus.CreditCardSettled)
        {
            throw new ArgumentException("Only a settled credit card expense can be unsettled.");
        }

        PaymentSourceBank = null;
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
        if (CreditCard is null)
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

        if (!PaymentSourceBank!.RoundUpEnabled)
        {
            throw new ArgumentException($"Bank '{PaymentSourceBank.Name}' does not support round-up.");
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

    private static void ValidateFields(string description, decimal value)
    {
        if (string.IsNullOrWhiteSpace(description))
        {
            throw new ArgumentException("Description is required.");
        }

        if (value == 0)
        {
            throw new ArgumentException("Value must not be zero.");
        }
    }

    private static void ValidatePaymentShape(Bank? paymentSourceBank, CreditCard? creditCard)
    {
        if (paymentSourceBank is null && creditCard is null)
        {
            throw new ArgumentException("An expense requires either a payment source or a card tag.");
        }

        if (paymentSourceBank is not null && creditCard is not null)
        {
            throw new ArgumentException(
                "An expense cannot have both a payment source and a card tag; a settled expense is only produced by marking its card statement paid.");
        }
    }
}
