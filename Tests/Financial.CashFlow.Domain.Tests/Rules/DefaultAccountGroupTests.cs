using Financial.CashFlow.Domain.Entities;
using Financial.CashFlow.Domain.Entities.Transactions;
using Financial.CashFlow.Domain.Enums;
using Financial.CashFlow.Domain.Rules;
using Financial.Shared.Abstractions.Currencies;
using Financial.TestUtilities;
using FluentAssertions;
using FluentAssertions.Execution;

namespace Financial.CashFlow.Domain.Tests.Rules;

[Trait("Category", "Unit")]
public class DefaultAccountGroupTests
{
    private static readonly DateOnly StartDate = new(2026, 1, 1);

    private readonly Category _groceries = Category.Create("Mercado");
    private readonly IncomeSource _salary = IncomeSource.Create("Salary", IncomeGroup.Salary);

    [Fact]
    public void MembersFor_ActiveBanks_AreOrderedByNameIgnoringCase()
    {
        var accounts = new[] { TestAccounts.Bank("chase"), TestAccounts.Bank("Itaú"), TestAccounts.Bank("Barclays") };

        var members = DefaultAccountGroup.MembersFor(accounts, 2026, 9, []);

        members.Select(member => member.Name).Should().Equal("Barclays", "chase", "Itaú");
    }

    [Fact]
    public void MembersFor_EqualNames_KeepFileOrder()
    {
        var first = TestAccounts.Bank("Chase");
        var second = TestAccounts.Bank("CHASE");

        var members = DefaultAccountGroup.MembersFor([first, second], 2026, 9, []);

        members.Should().Equal(first, second);
    }

    [Fact]
    [Trait("AC", "P60-F04-default-account-group-and-summary-query-01")]
    public void MembersFor_InactiveBankWithZeroBalanceAndNoRecords_IsDropped()
    {
        var active = TestAccounts.Bank("Barclays");
        var closed = InactiveBank("Closed", openingAmount: 0m);

        var members = DefaultAccountGroup.MembersFor([active, closed], 2026, 9, []);

        members.Should().Equal(active);
    }

    [Fact]
    [Trait("AC", "P60-F04-default-account-group-and-summary-query-01")]
    public void MembersFor_InactiveBankWithMoneyAtMonthEnd_IsIncluded()
    {
        var closed = InactiveBank("Closed Savings", openingAmount: 120m);

        var members = DefaultAccountGroup.MembersFor([closed], 2026, 9, []);

        members.Should().Equal(closed);
    }

    [Fact]
    [Trait("AC", "P60-F04-default-account-group-and-summary-query-01")]
    public void MembersFor_InactiveBankWithRecordInMonth_IsIncludedThatMonthOnly()
    {
        var closed = InactiveBank("Closed", openingAmount: 0m);
        var records = new IAccountTransaction[]
        {
            Income.Create(new DateOnly(2026, 9, 3), _salary, null, 40m, closed),
            Expense.Create(new DateOnly(2026, 9, 20), "Closing", 40m, _groceries, closed, creditCard: null)
        };

        using (new AssertionScope())
        {
            DefaultAccountGroup.MembersFor([closed], 2026, 9, records).Should().Equal(closed);
            DefaultAccountGroup.MembersFor([closed], 2026, 10, records).Should().BeEmpty();
        }
    }

    [Fact]
    public void MembersFor_InactiveBankWithRecordOfAnotherAccountInMonth_IsDropped()
    {
        var closed = InactiveBank("Closed", openingAmount: 0m);
        var other = TestAccounts.Bank("Barclays");
        var records = new IAccountTransaction[] { Income.Create(new DateOnly(2026, 9, 3), _salary, null, 40m, other) };

        DefaultAccountGroup.MembersFor([closed], 2026, 9, records).Should().BeEmpty();
    }

    [Fact]
    [Trait("AC", "P60-F04-default-account-group-and-summary-query-02")]
    public void MembersFor_AccountNotJoiningTheGroup_IsNeverIncluded()
    {
        var bank = TestAccounts.Bank("Barclays");
        var outside = Account.CreateWithCapabilities(
            "Pots", Currency.GBP, AccountTemplates.CapabilitiesOf(AccountTemplate.Bank), template: (AccountTemplate)99);
        outside.SetOpeningBalance(500m, StartDate);

        var members = DefaultAccountGroup.MembersFor([outside, bank], 2026, 9, []);

        members.Should().Equal(bank);
    }

    private static Account InactiveBank(string name, decimal openingAmount)
    {
        var account = Account.CreateWithCapabilities(name, Currency.GBP, AccountTemplates.CapabilitiesOf(AccountTemplate.Bank), isActive: false);
        account.SetOpeningBalance(openingAmount, StartDate);
        return account;
    }
}
