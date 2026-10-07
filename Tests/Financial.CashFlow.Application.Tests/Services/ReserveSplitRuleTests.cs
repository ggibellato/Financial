using Financial.CashFlow.Application.Services;
using FluentAssertions;

namespace Financial.CashFlow.Application.Tests.Services;

[Trait("Category", "Unit")]
public class ReserveSplitRuleTests
{
    [Theory]
    [InlineData(100)]
    [InlineData(99.99)]
    [InlineData(100.01)]
    public void IsBalanced_WithinTheToleranceOf100_IsTrue(double total)
    {
        ReserveSplitRule.IsBalanced((decimal)total).Should().BeTrue();
    }

    [Theory]
    [InlineData(99.98)]
    [InlineData(100.02)]
    [InlineData(60)]
    [InlineData(0)]
    public void IsBalanced_OutsideTheToleranceOf100_IsFalse(double total)
    {
        ReserveSplitRule.IsBalanced((decimal)total).Should().BeFalse();
    }

    [Theory]
    [InlineData(60, "Active buckets currently sum to 60% — review your split percentages")]
    [InlineData(66.67, "Active buckets currently sum to 66.67% — review your split percentages")]
    [InlineData(99.5, "Active buckets currently sum to 99.5% — review your split percentages")]
    public void BuildWarning_NamesTheTotalWithAtMostTwoDecimals(double total, string expected)
    {
        ReserveSplitRule.BuildWarning((decimal)total).Should().Be(expected);
    }

    [Fact]
    public void BuildWarning_UsesAPointAsTheDecimalSeparatorWhateverTheCulture()
    {
        var previous = System.Globalization.CultureInfo.CurrentCulture;
        System.Globalization.CultureInfo.CurrentCulture = new System.Globalization.CultureInfo("pt-BR");
        try
        {
            ReserveSplitRule.BuildWarning(66.67m).Should().Contain("66.67%");
        }
        finally
        {
            System.Globalization.CultureInfo.CurrentCulture = previous;
        }
    }
}
