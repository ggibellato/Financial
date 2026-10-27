using System;
using Financial.CashFlow.Domain.Enums;
using Financial.CashFlow.Domain.Rules;

namespace Financial.CashFlow.Domain.Entities.Transactions;

public class Expense : IAccountTransaction
{
    public const decimal MinRoundUpAmount = 0.00m;
    public const decimal MaxRoundUpAmount = 0.99m;
    public const string OnlyUnpaidChargeMovesMessage = "Only an unpaid card charge can move to another statement.";
    public const string LaterUnpaidStatementMessage = "Charges can only move to a later, unpaid statement.";

    public Guid Id { get; private set; }
    public DateOnly Date { get; private set; }
    public string Description { get; private set; } = string.Empty;
    public decimal Value { get; private set; }
    public Category Category { get; private set; } = null!;
    public Account? PaymentSourceAccount { get; private set; }
    public Account? CreditCard { get; private set; }
    public DateOnly? ChargeDate { get; private set; }
    public DateOnly? InvoiceDate { get; private set; }
    public decimal? RoundUpAmount { get; private set; }
    public bool CountsAsTithe { get; private set; } = true;

    public ExpensePaymentStatus PaymentStatus =>
        CreditCard is null ? ExpensePaymentStatus.ImmediatePayment
        : PaymentSourceAccount is null ? ExpensePaymentStatus.CreditCardCharge
        : ExpensePaymentStatus.CreditCardSettled;

    public decimal RoundUpSuggestion => ComputeRoundUpSuggestion(Value);

    public static decimal ComputeRoundUpSuggestion(decimal value) =>
        value <= 0 ? 0m : Math.Round(Math.Ceiling(value) - value, 2, MidpointRounding.AwayFromZero);

    /// <summary>
    /// <see cref="RoundUpSuggestion"/>, but only when it is actually offerable: no round-up already
    /// set, paid directly from a bank (not a credit-card charge), a positive value, and that bank
    /// itself supports round-up.
    /// </summary>
    public decimal? SuggestedRoundUpAmount =>
        RoundUpAmount is not null
        || PaymentStatus != ExpensePaymentStatus.ImmediatePayment
        || Value <= 0
        || PaymentSourceAccount?.RoundsUp != true
            ? null
            : RoundUpSuggestion;

    public bool IsInvestment => Category.IsInvestment;

    public DateOnly ReportingDate =>
        CreditCard is not null && InvoiceDate is not null
            ? InvoiceDate.Value
            : Date;

    public DateOnly ListingDate => InvoiceDate ?? ChargeDate ?? Date;

    private Expense() { }

    public static Expense Create(
        DateOnly date,
        string description,
        decimal value,
        Category category,
        Account? paymentSourceAccount,
        Account? creditCard,
        DateOnly? invoiceDate = null,
        bool countsAsTithe = true)
    {
        ValidateFields(description, value);
        ValidatePaymentShape(paymentSourceAccount, creditCard);
        ActiveAccountRule.EnsureUsable(null, paymentSourceAccount);

        return new()
        {
            Id = Guid.NewGuid(),
            Date = date,
            Description = description,
            Value = value,
            Category = category,
            PaymentSourceAccount = paymentSourceAccount,
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
        Account? paymentSourceAccount,
        Account? creditCard,
        bool countsAsTithe = true)
    {
        ValidateFields(description, value);

        if (PaymentStatus == ExpensePaymentStatus.CreditCardSettled)
        {
            if (paymentSourceAccount?.Id != PaymentSourceAccount?.Id || creditCard?.Id != CreditCard?.Id)
            {
                throw new ArgumentException(
                    "A settled expense's payment fields cannot be changed; unmark its card statement paid first.");
            }
        }
        else
        {
            ValidatePaymentShape(paymentSourceAccount, creditCard);
            ActiveAccountRule.EnsureUsable(PaymentSourceAccount, paymentSourceAccount);

            InvoiceDate = InvoiceDateAfterUpdate(date, creditCard);
            ChargeDate = creditCard is not null ? date : null;
            PaymentSourceAccount = paymentSourceAccount;
            CreditCard = creditCard;
        }

        Date = date;
        Description = description;
        Value = value;
        Category = category;
        CountsAsTithe = countsAsTithe;
    }

    public DateOnly? InvoiceDateAfterUpdate(DateOnly date, Account? creditCard, DateOnly? requestedInvoiceDate = null)
    {
        if (creditCard is not null && requestedInvoiceDate is { } requested)
        {
            return FirstOfMonth(requested);
        }

        if (PaymentStatus == ExpensePaymentStatus.CreditCardSettled)
        {
            return InvoiceDate;
        }

        if (creditCard is null)
        {
            return null;
        }

        var invoiceMonthFollowsDate = PaymentStatus != ExpensePaymentStatus.CreditCardCharge || InvoiceDate == FirstOfMonth(Date);
        return invoiceMonthFollowsDate ? FirstOfMonth(date) : InvoiceDate;
    }

    public bool ChangesLockedFields(
        DateOnly date, decimal value, Account? paymentSourceAccount, Account? creditCard, DateOnly? requestedInvoiceDate, decimal? roundUpAmount) =>
        date != Date
        || value != Value
        || paymentSourceAccount?.Id != PaymentSourceAccount?.Id
        || creditCard?.Id != CreditCard?.Id
        || InvoiceDateAfterUpdate(date, creditCard, requestedInvoiceDate) != InvoiceDate
        || roundUpAmount != RoundUpAmount;

    public void Settle(Account paymentSourceAccount, DateOnly paymentDate)
    {
        if (PaymentStatus != ExpensePaymentStatus.CreditCardCharge)
        {
            throw new ArgumentException("Only an unsettled credit card charge can be settled.");
        }

        paymentSourceAccount.EnsureCanPay();
        ActiveAccountRule.EnsureUsable(null, paymentSourceAccount);
        PaymentSourceAccount = paymentSourceAccount;
        Date = paymentDate;
    }

    public void Unsettle()
    {
        if (PaymentStatus != ExpensePaymentStatus.CreditCardSettled)
        {
            throw new ArgumentException("Only a settled credit card expense can be unsettled.");
        }

        PaymentSourceAccount = null;
        Date = ChargeDate!.Value;
    }

    public void EnsureCanMoveToStatement(DateOnly invoiceDate)
    {
        if (PaymentStatus != ExpensePaymentStatus.CreditCardCharge)
        {
            throw new ArgumentException(OnlyUnpaidChargeMovesMessage);
        }

        if (FirstOfMonth(invoiceDate) <= InvoiceDate)
        {
            throw new ArgumentException(LaterUnpaidStatementMessage);
        }

        EnsureCardIsActive(CreditCard!);
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
    /// One-time backfill for a pre-F01 record migrated by ExpenseChargeDateMigrator, for a record
    /// that predates ChargeDate/InvoiceDate existing at all. Only usable once, while ChargeDate is
    /// still unset - <see cref="UpdateDetails"/> is the ordinary way they change afterward.
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

        if (!PaymentSourceAccount!.RoundsUp)
        {
            throw new ArgumentException($"Bank '{PaymentSourceAccount!.Name}' does not support round-up.");
        }

        if (Value <= 0)
        {
            throw new ArgumentException("Round-up does not apply to a negative (reimbursement) expense.");
        }

        if (amount < MinRoundUpAmount || amount > MaxRoundUpAmount)
        {
            throw new ArgumentException(
                FormattableString.Invariant($"Round-up amount must be between £{MinRoundUpAmount:F2} and £{MaxRoundUpAmount:F2}."));
        }

        RoundUpAmount = amount;
    }

    public bool IsUnpaidChargeOn(Account card) =>
        PaymentStatus == ExpensePaymentStatus.CreditCardCharge && CreditCard!.Id == card.Id;

    public decimal ContributionTo(Account account) =>
        PaymentSourceAccount?.Id == account.Id ? -(Value + (RoundUpAmount ?? 0))
        : IsUnpaidChargeOn(account) ? -Value
        : 0m;

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

    private static void ValidatePaymentShape(Account? paymentSourceAccount, Account? creditCard)
    {
        if (paymentSourceAccount is null && creditCard is null)
        {
            throw new ArgumentException("An expense requires either a payment source or a card tag.");
        }

        if (paymentSourceAccount is not null && creditCard is not null)
        {
            throw new ArgumentException(
                "An expense cannot have both a payment source and a card tag; a settled expense is only produced by marking its card statement paid.");
        }

        if (creditCard is not null)
        {
            CardAccountRule.EnsureIsCard(creditCard);
            EnsureCardIsActive(creditCard);
        }

        paymentSourceAccount?.EnsureCanPay();
    }

    private static void EnsureCardIsActive(Account creditCard)
    {
        if (!creditCard.IsActive)
        {
            throw new ArgumentException(
                $"Credit card '{creditCard.Name}' is inactive and cannot be used for new entries.");
        }
    }
}
