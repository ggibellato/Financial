using Financial.CashFlow.Domain.Rules;
using Financial.TestUtilities;
using FluentAssertions;

namespace Financial.CashFlow.Domain.Tests.Rules;

[Trait("Category", "Unit")]
public class ActiveAccountRuleTests
{
    [Fact]
    public void EnsureUsable_InactiveCandidateDifferentFromCurrent_Throws()
    {
        var act = () => ActiveAccountRule.EnsureUsable(TestAccounts.Bank("Barclays"), TestAccounts.Bank("Closed", isActive: false));

        act.Should().Throw<ArgumentException>().WithMessage("Closed is inactive.");
    }

    [Fact]
    public void EnsureUsable_InactiveCandidateWithNoCurrent_Throws()
    {
        var act = () => ActiveAccountRule.EnsureUsable(null, TestAccounts.Bank("Closed", isActive: false));

        act.Should().Throw<ArgumentException>().WithMessage("Closed is inactive.");
    }

    [Fact]
    public void EnsureUsable_InactiveCandidateThatIsTheCurrentAccount_Succeeds()
    {
        var closed = TestAccounts.Bank("Closed", isActive: false);

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
        var act = () => ActiveAccountRule.EnsureUsable(TestAccounts.Bank("Closed", isActive: false), null);

        act.Should().NotThrow();
    }
}
