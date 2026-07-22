using Financial.CashFlow.Domain.Entities;
using Financial.CashFlow.Domain.Enums;
using FluentAssertions;

namespace Financial.CashFlow.Domain.Tests;

public class InvestmentSnapshotTests
{
    [Fact]
    public void Create_AssignsAllFieldsAndANewId()
    {
        var snapshot = InvestmentSnapshot.Create(InvestmentAccount.PlatinumVisa8003, 2026, 7, 1250.00m);

        snapshot.Id.Should().NotBeEmpty();
        snapshot.Account.Should().Be(InvestmentAccount.PlatinumVisa8003);
        snapshot.Year.Should().Be(2026);
        snapshot.Month.Should().Be(7);
        snapshot.Value.Should().Be(1250.00m);
    }

    [Fact]
    public void Update_ChangesValueWithoutChangingIdentityFields()
    {
        var snapshot = InvestmentSnapshot.Create(InvestmentAccount.ChaseSave, 2026, 7, 0m);
        var originalId = snapshot.Id;

        snapshot.Update(500m);

        snapshot.Id.Should().Be(originalId);
        snapshot.Account.Should().Be(InvestmentAccount.ChaseSave);
        snapshot.Year.Should().Be(2026);
        snapshot.Month.Should().Be(7);
        snapshot.Value.Should().Be(500m);
    }

    [Fact]
    public void Create_TwoSnapshots_HaveDifferentIds()
    {
        var first = InvestmentSnapshot.Create(InvestmentAccount.ChaseSave, 2026, 7, 0m);
        var second = InvestmentSnapshot.Create(InvestmentAccount.ChaseSave, 2026, 7, 0m);

        first.Id.Should().NotBe(second.Id);
    }
}
