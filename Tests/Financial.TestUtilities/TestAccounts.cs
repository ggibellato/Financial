using Financial.CashFlow.Domain.Entities;
using Financial.CashFlow.Domain.Enums;
using Financial.CashFlow.Domain.Rules;
using Financial.Shared.Abstractions.Currencies;

namespace Financial.TestUtilities;

public static class TestAccounts
{
    public static Account Bank(string name, bool roundUpEnabled = false, Currency currency = Currency.GBP)
    {
        var account = currency == Currency.GBP
            ? Account.Create(name, AccountTemplate.Bank, currency)
            : Account.CreateWithCapabilities(name, currency, AccountTemplates.CapabilitiesOf(AccountTemplate.Bank));
        account.SetRoundUp(roundUpEnabled);
        return account;
    }
}
