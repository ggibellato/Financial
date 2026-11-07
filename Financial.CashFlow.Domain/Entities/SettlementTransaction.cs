using System;
using System.Linq;
using Financial.CashFlow.Domain.Enums;
using Financial.Shared.Abstractions.Currencies;

namespace Financial.CashFlow.Domain.Entities;

public class SettlementTransaction
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
        decimal? convertedValue)
    {
        ArgumentNullException.ThrowIfNull(account);

        if (!account.Has(Capability.HasParticipants))
        {
            throw new ArgumentException($"{account.Name} is not a Settlement account.");
        }

        if (!account.Currencies.Contains(currency))
        {
            throw new ArgumentException($"{account.Name} doesn't use {currency}.");
        }

        if (string.IsNullOrWhiteSpace(description))
        {
            throw new ArgumentException("Description is required.");
        }

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

    public void UpdateValues(SettlementPayer paidBy, decimal amount, decimal? convertedValue)
    {
        if (amount <= 0)
        {
            throw new ArgumentException("Amount must be greater than zero.");
        }

        if (convertedValue.HasValue && ConvertedCurrency() is null)
        {
            throw new ArgumentException($"{Account.Name} has one currency, so an entry has no converted value.");
        }

        if (convertedValue <= 0)
        {
            throw new ArgumentException("The converted value must be greater than zero.");
        }

        PaidBy = paidBy;
        Amount = amount;
        ConvertedValue = convertedValue;
    }

    public decimal? SignedValueIn(Currency currency)
    {
        var sign = PaidBy == SettlementPayer.You ? 1m : -1m;

        if (currency == Currency)
        {
            return sign * Amount;
        }

        return currency == ConvertedCurrency() ? sign * ConvertedValue : null;
    }

    private Currency? ConvertedCurrency() =>
        Account.Currencies.Where(currency => currency != Currency).Select(currency => (Currency?)currency).FirstOrDefault();
}
