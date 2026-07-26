using Financial.CashFlow.Domain.Entities;
using FluentAssertions;

namespace Financial.CashFlow.Domain.Tests;

public class InvestmentAccountTests
{
    [Fact]
    public void Create_WithValidName_AssignsAllFieldsAndANewId()
    {
        var account = InvestmentAccount.Create("ChaseSave", isActive: true, isLiability: false);

        account.Id.Should().NotBeEmpty();
        account.Name.Should().Be("ChaseSave");
        account.IsActive.Should().BeTrue();
        account.IsLiability.Should().BeFalse();
    }

    [Fact]
    public void Create_WithLiabilityFlag_SetsIsLiabilityTrue()
    {
        var account = InvestmentAccount.Create("PlatinumVisa8003", isActive: true, isLiability: true);

        account.IsLiability.Should().BeTrue();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithEmptyName_ThrowsArgumentException(string? name)
    {
        var act = () => InvestmentAccount.Create(name!, isActive: true, isLiability: false);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_TwoAccounts_HaveDifferentIds()
    {
        var first = InvestmentAccount.Create("ChaseSave", isActive: true, isLiability: false);
        var second = InvestmentAccount.Create("ChaseSave", isActive: true, isLiability: false);

        first.Id.Should().NotBe(second.Id);
    }
}
