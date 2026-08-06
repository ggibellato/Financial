using Financial.CashFlow.Domain.Entities;
using FluentAssertions;
using FluentAssertions.Execution;

namespace Financial.CashFlow.Domain.Tests;

public class InvestmentSnapshotTests
{
    private static readonly InvestmentAccount PlatinumVisa8003 =
        InvestmentAccount.Create("PlatinumVisa8003", isActive: true, isLiability: true);
    private static readonly InvestmentAccount ChaseSave =
        InvestmentAccount.Create("ChaseSave", isActive: true, isLiability: false);

    [Fact]
    public void Create_AssignsAllFieldsAndANewId()
    {
        var snapshot = InvestmentSnapshot.Create(PlatinumVisa8003, 2026, 7, 1250.00m);

        using (new AssertionScope())
        {
            snapshot.Id.Should().NotBeEmpty();
            snapshot.Account.Should().Be(PlatinumVisa8003);
            snapshot.Year.Should().Be(2026);
            snapshot.Month.Should().Be(7);
            snapshot.Value.Should().Be(1250.00m);
        }
    }

    [Fact]
    public void Create_WithoutAnAccount_Throws()
    {
        var act = () => InvestmentSnapshot.Create(null!, 2026, 7, 0m);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Update_ChangesValueWithoutChangingIdentityFields()
    {
        var snapshot = InvestmentSnapshot.Create(ChaseSave, 2026, 7, 0m);
        var originalId = snapshot.Id;

        snapshot.Update(500m);

        using (new AssertionScope())
        {
            snapshot.Id.Should().Be(originalId);
            snapshot.Account.Should().Be(ChaseSave);
            snapshot.Year.Should().Be(2026);
            snapshot.Month.Should().Be(7);
            snapshot.Value.Should().Be(500m);
        }
    }

    [Fact]
    public void Create_TwoSnapshots_HaveDifferentIds()
    {
        var first = InvestmentSnapshot.Create(ChaseSave, 2026, 7, 0m);
        var second = InvestmentSnapshot.Create(ChaseSave, 2026, 7, 0m);

        first.Id.Should().NotBe(second.Id);
    }
}
