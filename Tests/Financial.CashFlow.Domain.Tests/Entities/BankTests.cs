using Financial.CashFlow.Domain.Entities;
using FluentAssertions;

namespace Financial.CashFlow.Domain.Tests;

public class BankTests
{
    [Fact]
    public void Create_AssignsNameAndRoundUpEnabled()
    {
        var bank = Bank.Create("Chase", roundUpEnabled: true);

        bank.Name.Should().Be("Chase");
        bank.RoundUpEnabled.Should().BeTrue();
    }

    [Fact]
    public void Create_DefaultsOpeningBalanceToZeroAndDateToDefault()
    {
        var bank = Bank.Create("Chase", roundUpEnabled: true);

        bank.OpeningBalance.Should().Be(0m);
        bank.OpeningBalanceDate.Should().Be(default(DateOnly));
    }

    [Fact]
    public void SetOpeningBalance_UpdatesBothFields()
    {
        var bank = Bank.Create("Chase", roundUpEnabled: true);
        var date = new DateOnly(2026, 7, 1);

        bank.SetOpeningBalance(1250.75m, date);

        bank.OpeningBalance.Should().Be(1250.75m);
        bank.OpeningBalanceDate.Should().Be(date);
    }

    [Fact]
    public void SetOpeningBalance_WithNegativeValue_ThrowsAndLeavesPriorValuesUntouched()
    {
        var bank = Bank.Create("Chase", roundUpEnabled: true);
        bank.SetOpeningBalance(100m, new DateOnly(2026, 7, 1));

        var act = () => bank.SetOpeningBalance(-1m, new DateOnly(2026, 8, 1));

        act.Should().Throw<ArgumentException>();
        bank.OpeningBalance.Should().Be(100m);
        bank.OpeningBalanceDate.Should().Be(new DateOnly(2026, 7, 1));
    }

    [Fact]
    public void Create_WithRoundUpDisabled_AssignsFalse()
    {
        var bank = Bank.Create("Barclays", roundUpEnabled: false);

        bank.RoundUpEnabled.Should().BeFalse();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithoutAName_Throws(string? name)
    {
        var act = () => Bank.Create(name!, roundUpEnabled: false);

        act.Should().Throw<ArgumentException>();
    }
}
