using Financial.CashFlow.Domain.Entities;
using Financial.CashFlow.Domain.Enums;
using Financial.CashFlow.Domain.Rules;
using Financial.CashFlow.Domain.ValueObjects;
using Financial.Shared.Abstractions.Currencies;

namespace Financial.TestUtilities;

public static class TestAccounts
{
    public static Account Bank(
        string name, bool roundUpEnabled = false, Currency currency = Currency.GBP, bool isActive = true, decimal? cashbackRatePercent = null)
    {
        var account = currency == Currency.GBP
            ? Account.Create(name, AccountTemplate.Bank, currency)
            : Account.CreateWithCapabilities(name, currency, AccountTemplates.CapabilitiesOf(AccountTemplate.Bank));
        account.SetRoundUp(roundUpEnabled);
        if (cashbackRatePercent is { } rate)
        {
            account.SetCashback(CashbackSetting.On(rate));
        }

        account.SetActive(isActive);
        return account;
    }

    public static Account Pots(string name, bool isActive = true, Category? category = null)
    {
        var account = Account.Create(name, AccountTemplate.Pots, Currency.GBP);
        account.SetActive(isActive);
        if (category is not null)
        {
            account.SetCategory(category);
        }

        return account;
    }

    public static Account ReservaIn(CashFlowData data)
    {
        if (ReservaAccount.Find(data.Accounts) is { } reserva)
        {
            return reserva;
        }

        var created = Pots("Reserva");
        data.AddAccount(created);
        return created;
    }

    public static Account CreditCard(string name, bool isActive = true)
    {
        var account = Account.Create(name, AccountTemplate.CreditCard, Currency.GBP);
        account.SetActive(isActive);
        return account;
    }

    public static Account Settlement(string name, IEnumerable<Currency> currencies, string? otherParticipant = null)
    {
        var account = Account.Create(name, AccountTemplate.Settlement, Currency.BRL);
        account.SetCurrencies(currencies);
        if (otherParticipant is not null)
        {
            account.SetOtherParticipant(otherParticipant);
        }

        return account;
    }

    public static Account Savings(string name, SavingsTerms? terms = null) =>
        Account.Create(
            name,
            AccountTemplate.Savings,
            Currency.GBP,
            terms ?? SavingsTerms.Create(InterestFrequency.Monthly, 5, isTaxFree: false, [InterestRate.Create(4.5m, new DateOnly(2026, 9, 1))]));
}
