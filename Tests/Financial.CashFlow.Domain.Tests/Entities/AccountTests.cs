using Financial.CashFlow.Domain.Entities;
using Financial.CashFlow.Domain.Enums;
using Financial.CashFlow.Domain.ValueObjects;
using Financial.Shared.Abstractions.Currencies;
using FluentAssertions;
using FluentAssertions.Execution;

namespace Financial.CashFlow.Domain.Tests;

[Trait("Category", "Unit")]
public class AccountTests
{
    private static Account CreateBank() => Account.Create("Chase", AccountTemplate.Bank, Currency.GBP);

    private static Account CreateMinimal(params Capability[] capabilities) =>
        Account.CreateWithCapabilities("Minimal", Currency.GBP, capabilities.ToHashSet());

    [Fact]
    public void Create_WithBankTemplate_HasAllEightCapabilitiesAndIsActive()
    {
        var account = CreateBank();

        using (new AssertionScope())
        {
            Enum.GetValues<Capability>().Should().OnlyContain(capability => account.Has(capability));
            account.IsActive.Should().BeTrue();
            account.OpeningBalance.Should().Be(OpeningBalance.Create(0m, new DateOnly(1, 1, 1)));
            account.RoundUp.Should().Be(new RoundUpSetting(false));
        }
    }

    [Fact]
    public void Create_AssignsIdNameTemplateAndCurrency()
    {
        var account = Account.Create("Chase", AccountTemplate.Bank, Currency.GBP);

        using (new AssertionScope())
        {
            account.Id.Should().NotBeEmpty();
            account.Name.Should().Be("Chase");
            account.Template.Should().Be(AccountTemplate.Bank);
            account.Currency.Should().Be(Currency.GBP);
        }
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithoutAName_Throws(string? name)
    {
        var act = () => Account.Create(name!, AccountTemplate.Bank, Currency.GBP);

        act.Should().Throw<ArgumentException>().WithMessage("Account name is required.");
    }

    [Fact]
    public void Create_WithUndefinedTemplate_Throws()
    {
        var act = () => Account.Create("Chase", (AccountTemplate)99, Currency.GBP);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void CreateWithCapabilities_WithoutOpeningBalanceOrRoundUp_LeavesBothNull()
    {
        var account = CreateMinimal(Capability.ReceivesIncome);

        using (new AssertionScope())
        {
            account.OpeningBalance.Should().BeNull();
            account.RoundUp.Should().BeNull();
            account.Has(Capability.ReceivesIncome).Should().BeTrue();
            account.Has(Capability.HasCategories).Should().BeFalse();
        }
    }

    [Fact]
    public void SetOpeningBalance_ReplacesAmountAndStartDate()
    {
        var account = CreateBank();

        account.SetOpeningBalance(1250.75m, new DateOnly(2026, 7, 1));

        account.OpeningBalance.Should().Be(OpeningBalance.Create(1250.75m, new DateOnly(2026, 7, 1)));
    }

    [Fact]
    public void SetOpeningBalance_Negative_ThrowsArgumentException()
    {
        var account = CreateBank();
        account.SetOpeningBalance(100m, new DateOnly(2026, 7, 1));

        var act = () => account.SetOpeningBalance(-1m, new DateOnly(2026, 8, 1));

        using (new AssertionScope())
        {
            act.Should().Throw<ArgumentException>().WithMessage("Opening balance cannot be negative.");
            account.OpeningBalance.Should().Be(OpeningBalance.Create(100m, new DateOnly(2026, 7, 1)));
        }
    }

    [Fact]
    public void SetOpeningBalance_WithoutCapability_ThrowsArgumentException()
    {
        var account = CreateMinimal(Capability.SupportsRoundUp);

        var act = () => account.SetOpeningBalance(100m, new DateOnly(2026, 7, 1));

        using (new AssertionScope())
        {
            act.Should().Throw<ArgumentException>().WithMessage("Minimal has no opening balance.");
            account.OpeningBalance.Should().BeNull();
        }
    }

    [Fact]
    public void SetOpeningBalance_WithOnlyThatCapability_Succeeds()
    {
        var account = CreateMinimal(Capability.HasOpeningBalance);

        account.SetOpeningBalance(10m, new DateOnly(2026, 9, 1));

        account.OpeningBalance.Should().Be(OpeningBalance.Create(10m, new DateOnly(2026, 9, 1)));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void SetRoundUp_ReplacesTheSetting(bool enabled)
    {
        var account = CreateBank();

        account.SetRoundUp(enabled);

        account.RoundUp.Should().Be(new RoundUpSetting(enabled));
    }

    [Fact]
    public void SetRoundUp_WithoutCapability_ThrowsArgumentException()
    {
        var account = CreateMinimal(Capability.HasOpeningBalance);

        var act = () => account.SetRoundUp(true);

        using (new AssertionScope())
        {
            act.Should().Throw<ArgumentException>().WithMessage("Minimal does not support round-up.");
            account.RoundUp.Should().BeNull();
        }
    }

    [Fact]
    public void SetRoundUp_WithOnlyThatCapability_Succeeds()
    {
        var account = CreateMinimal(Capability.SupportsRoundUp);

        account.SetRoundUp(true);

        account.RoundUp.Should().Be(new RoundUpSetting(true));
    }

    [Fact]
    public void Update_ChangesNameAndKeepsOpeningBalanceAndRoundUp()
    {
        var account = CreateBank();
        account.SetOpeningBalance(100m, new DateOnly(2026, 7, 1));
        account.SetRoundUp(true);

        account.Update("Barclays");

        using (new AssertionScope())
        {
            account.Name.Should().Be("Barclays");
            account.OpeningBalance.Should().Be(OpeningBalance.Create(100m, new DateOnly(2026, 7, 1)));
            account.RoundUp.Should().Be(new RoundUpSetting(true));
        }
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Update_WithoutAName_ThrowsAndLeavesNameUntouched(string? name)
    {
        var account = CreateBank();

        var act = () => account.Update(name!);

        using (new AssertionScope())
        {
            act.Should().Throw<ArgumentException>().WithMessage("Account name is required.");
            account.Name.Should().Be("Chase");
        }
    }
}
