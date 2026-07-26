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

    [Fact]
    public void Create_StartsWithNoAliases()
    {
        var account = InvestmentAccount.Create("ChaseSave", isActive: true, isLiability: false);

        account.Aliases.Should().BeEmpty();
    }

    [Fact]
    public void AddAlias_NewAlias_AddsIt()
    {
        var account = InvestmentAccount.Create("ChaseSave", isActive: true, isLiability: false);

        account.AddAlias("Chase save");

        account.Aliases.Should().ContainSingle().Which.Should().Be("Chase save");
    }

    [Fact]
    public void AddAlias_DuplicateCaseInsensitive_DoesNotAddTwice()
    {
        var account = InvestmentAccount.Create("ChaseSave", isActive: true, isLiability: false);
        account.AddAlias("Chase save");

        account.AddAlias("chase SAVE");

        account.Aliases.Should().ContainSingle();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void AddAlias_WithEmptyAlias_ThrowsArgumentException(string? alias)
    {
        var account = InvestmentAccount.Create("ChaseSave", isActive: true, isLiability: false);

        var act = () => account.AddAlias(alias!);

        act.Should().Throw<ArgumentException>();
    }
}
