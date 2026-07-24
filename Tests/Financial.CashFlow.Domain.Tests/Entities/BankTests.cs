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
