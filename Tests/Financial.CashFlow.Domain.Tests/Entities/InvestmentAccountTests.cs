using Financial.CashFlow.Domain.Entities;
using FluentAssertions;
using FluentAssertions.Execution;

namespace Financial.CashFlow.Domain.Tests;

public class InvestmentAccountTests
{
    [Fact]
    public void Create_WithValidName_AssignsAllFieldsAndANewId()
    {
        var account = InvestmentAccount.Create("ChaseSave", isActive: true, isLiability: false);

        using (new AssertionScope())
        {
            account.Id.Should().NotBeEmpty();
            account.Name.Should().Be("ChaseSave");
            account.IsActive.Should().BeTrue();
            account.IsLiability.Should().BeFalse();
        }
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

    [Fact]
    public void Update_ChangesNameActiveLiabilityAndReplacesAliases()
    {
        var account = InvestmentAccount.Create("ChaseSave", isActive: true, isLiability: false);
        account.AddAlias("Old alias");

        account.Update("ChaseSaveRenamed", isActive: false, isLiability: true, aliases: ["New alias"]);

        using (new AssertionScope())
        {
            account.Name.Should().Be("ChaseSaveRenamed");
            account.IsActive.Should().BeFalse();
            account.IsLiability.Should().BeTrue();
            account.Aliases.Should().ContainSingle().Which.Should().Be("New alias");
        }
    }

    [Fact]
    public void Update_WithEmptyAliasesList_ClearsExistingAliases()
    {
        var account = InvestmentAccount.Create("ChaseSave", isActive: true, isLiability: false);
        account.AddAlias("Old alias");

        account.Update("ChaseSave", isActive: true, isLiability: false, aliases: []);

        account.Aliases.Should().BeEmpty();
    }

    [Fact]
    public void Update_WithDuplicateCaseInsensitiveAliases_DedupsThem()
    {
        var account = InvestmentAccount.Create("ChaseSave", isActive: true, isLiability: false);

        account.Update("ChaseSave", isActive: true, isLiability: false, aliases: ["Chase save", "chase SAVE"]);

        account.Aliases.Should().ContainSingle();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Update_WithoutAName_ThrowsAndLeavesPriorValuesUntouched(string? name)
    {
        var account = InvestmentAccount.Create("ChaseSave", isActive: true, isLiability: false);
        account.AddAlias("Existing alias");

        var act = () => account.Update(name!, isActive: false, isLiability: true, aliases: ["New alias"]);

        using (new AssertionScope())
        {
            act.Should().Throw<ArgumentException>();
            account.Name.Should().Be("ChaseSave");
            account.IsActive.Should().BeTrue();
            account.IsLiability.Should().BeFalse();
            account.Aliases.Should().ContainSingle().Which.Should().Be("Existing alias");
        }
    }

    [Fact]
    public void Update_WithABlankAliasInTheList_ThrowsAndLeavesPriorAliasesUntouched()
    {
        var account = InvestmentAccount.Create("ChaseSave", isActive: true, isLiability: false);
        account.AddAlias("Existing alias");

        var act = () => account.Update("ChaseSave", isActive: true, isLiability: false, aliases: ["Valid", "   "]);

        using (new AssertionScope())
        {
            act.Should().Throw<ArgumentException>();
            account.Aliases.Should().ContainSingle().Which.Should().Be("Existing alias");
        }
    }
}
