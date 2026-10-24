using Financial.CashFlow.Domain.Entities;
using Financial.CashFlow.Domain.Rules;
using Financial.TestUtilities;
using FluentAssertions;

namespace Financial.CashFlow.Domain.Tests.Rules;

[Trait("Category", "Unit")]
public class ActiveAccountRuleTests
{
    private static Account Inactive(string name)
    {
        var account = TestAccounts.Bank(name);
        account.SetActive(false);
        return account;
    }

    [Fact]
    public void EnsureUsable_InactiveCandidateDifferentFromCurrent_Throws()
    {
        var act = () => ActiveAccountRule.EnsureUsable(TestAccounts.Bank("Barclays"), Inactive("Closed"));

        act.Should().Throw<ArgumentException>().WithMessage("Closed is inactive.");
    }

    [Fact]
    public void EnsureUsable_InactiveCandidateWithNoCurrent_Throws()
    {
        var act = () => ActiveAccountRule.EnsureUsable(null, Inactive("Closed"));

        act.Should().Throw<ArgumentException>().WithMessage("Closed is inactive.");
    }

    [Fact]
    public void EnsureUsable_InactiveCandidateThatIsTheCurrentAccount_Succeeds()
    {
        var closed = Inactive("Closed");

        var act = () => ActiveAccountRule.EnsureUsable(closed, closed);

        act.Should().NotThrow();
    }

    [Fact]
    public void EnsureUsable_ActiveCandidate_Succeeds()
    {
        var act = () => ActiveAccountRule.EnsureUsable(null, TestAccounts.Bank("Barclays"));

        act.Should().NotThrow();
    }

    [Fact]
    public void EnsureUsable_NoCandidate_Succeeds()
    {
        var act = () => ActiveAccountRule.EnsureUsable(Inactive("Closed"), null);

        act.Should().NotThrow();
    }
}
