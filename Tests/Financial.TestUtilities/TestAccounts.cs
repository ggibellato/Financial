using Financial.CashFlow.Domain.Entities;
using Financial.CashFlow.Domain.Enums;
using Financial.Shared.Abstractions.Currencies;

namespace Financial.TestUtilities;

public static class TestAccounts
{
    public static Account Bank(string name, bool roundUpEnabled = false, Currency currency = Currency.GBP)
    {
        var account = Account.Create(name, AccountTemplate.Bank, currency);
        account.SetRoundUp(roundUpEnabled);
        return account;
    }
}
