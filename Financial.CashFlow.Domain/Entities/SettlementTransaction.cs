using System;
using System.Linq;
using Financial.CashFlow.Domain.Entities.Transactions;
using Financial.CashFlow.Domain.Enums;
using Financial.CashFlow.Domain.Rules;
using Financial.Shared.Abstractions.Currencies;

namespace Financial.CashFlow.Domain.Entities;

public class SettlementTransaction : IAccountTransaction
{
    public Guid Id { get; private set; }
    public Account Account { get; private set; } = null!;
    public DateOnly Date { get; private set; }
    public string Description { get; private set; } = string.Empty;
    public string Note { get; private set; } = string.Empty;
    public SettlementPayer PaidBy { get; private set; }
    public Currency Currency { get; private set; }
    public decimal Amount { get; private set; }
    public decimal? ConvertedValue { get; private set; }

    private SettlementTransaction() { }

    public static SettlementTransaction Create(
        Account account,
        DateOnly date,
        string description,
        string? note,
        SettlementPayer paidBy,
        Currency currency,
        decimal amount,
        decimal? convertedValue,
        DateOnly today)
    {
        EnsureSettlementAccount(account);
        TransactionDateRule.EnsureNotInFuture(date, today);
        EnsureAccountUses(account, currency);
        EnsureDescription(description);

        var entry = new SettlementTransaction
        {
            Id = Guid.NewGuid(),
            Account = account,
            Date = date,
            Description = description,
            Note = note ?? string.Empty,
            Currency = currency
        };
        entry.UpdateValues(paidBy, amount, convertedValue);
        return entry;
    }

    public void Update(
        DateOnly date, string description, string? note, SettlementPayer paidBy, decimal amount, decimal? convertedValue, DateOnly today)
    {
        TransactionDateRule.EnsureNotInFuture(date, today);
        EnsureDescription(description);
        UpdateValues(paidBy, amount, convertedValue);

        Date = date;
        Description = description;
        Note = note ?? string.Empty;
    }

    public void UpdateValues(SettlementPayer paidBy, decimal amount, decimal? convertedValue)
    {
        var roundedAmount = MoneyRounding.ToCents(amount);
        var roundedConvertedValue = convertedValue is { } value && !RoundsToZero(value) ? MoneyRounding.ToCents(value) : (decimal?)null;

        EnsurePositiveAmount(amount);
        EnsureConvertedValueFits(convertedValue);

        PaidBy = paidBy;
        Amount = roundedAmount;
        ConvertedValue = roundedConvertedValue;
    }

    public static bool RoundsToZero(decimal value) => MoneyRounding.ToCents(value) == 0;

    public static void EnsureSettlementAccount(Account account)
    {
        ArgumentNullException.ThrowIfNull(account);

        if (!account.Has(Capability.HasParticipants))
        {
            throw new ArgumentException($"{account.Name} is not a Settlement account.");
        }
    }

    public void EnsureConsistentWithAccount()
    {
        EnsureSettlementAccount(Account);
        EnsureAccountUses(Account, Currency);
        EnsurePositiveAmount(Amount);
        EnsureConvertedValueFits(ConvertedValue);
    }

    private void EnsureConvertedValueFits(decimal? convertedValue)
    {
        if (convertedValue.HasValue && ConvertedCurrencyOf(Account, Currency) is null)
        {
            throw new ArgumentException($"{Account.Name} has one currency, so an entry has no converted value.");
        }

        if (convertedValue is { } value && MoneyRounding.ToCents(value) < 0)
        {
            throw new ArgumentException("The converted value must be greater than zero.");
        }
    }

    private static void EnsureAccountUses(Account account, Currency currency)
    {
        if (!account.Currencies.Contains(currency))
        {
            throw new ArgumentException($"{account.Name} doesn't use {currency}.");
        }
    }

    private static void EnsurePositiveAmount(decimal amount)
    {
        if (MoneyRounding.ToCents(amount) <= 0)
        {
            throw new ArgumentException("Amount must be greater than zero.");
        }
    }

    public decimal ContributionTo(Account account)
    {
        ArgumentNullException.ThrowIfNull(account);
        return ContributionTo(account, account.Currency);
    }

    public decimal ContributionTo(Account account, Currency currency)
    {
        ArgumentNullException.ThrowIfNull(account);
        return account.Id == Account.Id ? SignedValueIn(currency) ?? 0m : 0m;
    }

    public decimal? SignedValueIn(Currency currency)
    {
        var sign = PaidBy == SettlementPayer.You ? 1m : -1m;

        if (currency == Currency)
        {
            return sign * Amount;
        }

        return currency == ConvertedCurrencyOf(Account, Currency) ? sign * ConvertedValue : null;
    }

    public static Currency? ConvertedCurrencyOf(Account account, Currency paidCurrency)
    {
        ArgumentNullException.ThrowIfNull(account);
        return account.Currencies.Where(currency => currency != paidCurrency).Select(currency => (Currency?)currency).FirstOrDefault();
    }

    public static decimal ConvertedValueAt(decimal amount, decimal rate) => MoneyRounding.ToCents(MoneyRounding.ToCents(amount) * rate);

    private static void EnsureDescription(string description)
    {
        if (string.IsNullOrWhiteSpace(description))
        {
            throw new ArgumentException("Description is required.");
        }
    }

    public static SettlementPayer PayerOf(decimal signedValue) =>
        signedValue > 0 ? SettlementPayer.You : SettlementPayer.Other;

    public static bool HaveOneSign(decimal signedValue, decimal? otherSignedValue) =>
        signedValue != 0 && (otherSignedValue is null || Math.Sign(otherSignedValue.Value) == Math.Sign(signedValue));
}
