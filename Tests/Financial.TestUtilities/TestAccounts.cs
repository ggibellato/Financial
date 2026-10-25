using Financial.CashFlow.Domain.Entities;
using Financial.CashFlow.Domain.Enums;
using Financial.CashFlow.Domain.Rules;
using Financial.Shared.Abstractions.Currencies;

namespace Financial.TestUtilities;

public static class TestAccounts
{
    public static Account Bank(string name, bool roundUpEnabled = false, Currency currency = Currency.GBP, bool isActive = true)
    {
        var account = currency == Currency.GBP
            ? Account.Create(name, AccountTemplate.Bank, currency)
            : Account.CreateWithCapabilities(name, currency, AccountTemplates.CapabilitiesOf(AccountTemplate.Bank));
        account.SetRoundUp(roundUpEnabled);
        account.SetActive(isActive);
        return account;
    }

    public static Account CreditCard(string name, bool isActive = true, DateOnly? nextInvoiceDueDate = null)
    {
        var account = Account.Create(name, AccountTemplate.CreditCard, Currency.GBP);
        account.SetActive(isActive);
        account.SetNextInvoiceDueDate(nextInvoiceDueDate);
        return account;
    }

    public static void UpdateCard(this Account card, string name, bool isActive, DateOnly? nextInvoiceDueDate)
    {
        card.Update(name);
        card.SetActive(isActive);
        card.SetNextInvoiceDueDate(nextInvoiceDueDate);
    }
}
