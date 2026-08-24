using Financial.CashFlow.Domain.Rules;
using FluentAssertions;

namespace Financial.CashFlow.Domain.Tests.Rules;

public class TitheRuleTests
{
    [Fact]
    public void CalculateTithe_ReturnsTenPercentOfTheAmount()
    {
        TitheRule.CalculateTithe(1000m).Should().Be(100m);
    }

    [Fact]
    public void NetOfTithe_ReturnsNinetyPercentOfTheAmount()
    {
        TitheRule.NetOfTithe(1000m).Should().Be(900m);
    }

    [Fact]
    public void NetOfTithe_PlusCalculateTithe_EqualsTheOriginalAmount()
    {
        var amount = 2450.00m;

        (TitheRule.NetOfTithe(amount) + TitheRule.CalculateTithe(amount)).Should().Be(amount);
    }
}
