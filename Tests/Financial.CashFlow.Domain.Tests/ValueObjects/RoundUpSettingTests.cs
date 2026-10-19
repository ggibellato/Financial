using Financial.CashFlow.Domain.ValueObjects;
using FluentAssertions;

namespace Financial.CashFlow.Domain.Tests.ValueObjects;

[Trait("Category", "Unit")]
public class RoundUpSettingTests
{
    [Fact]
    public void Equality_SameEnabled_AreEqual()
    {
        new RoundUpSetting(true).Should().Be(new RoundUpSetting(true));
    }

    [Fact]
    public void Equality_DifferentEnabled_AreNotEqual()
    {
        new RoundUpSetting(true).Should().NotBe(new RoundUpSetting(false));
    }
}
