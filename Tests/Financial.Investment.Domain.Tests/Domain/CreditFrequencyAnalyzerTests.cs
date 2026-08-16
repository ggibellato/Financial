using Financial.Investment.Domain.Entities;
using Financial.Investment.Domain.Rules;
using FluentAssertions;

namespace Financial.Investment.Domain.Tests;

public class CreditFrequencyAnalyzerTests
{
    [Fact]
    public void DetectFrequencyPerYear_WhenFewerThanTwoDistinctMonths_ReturnsNull()
    {
        var credits = new[] { Credit.Create(new DateTime(2026, 1, 15), Credit.CreditType.Dividend, 10m) };

        CreditFrequencyAnalyzer.DetectFrequencyPerYear(credits).Should().BeNull();
    }

    [Fact]
    public void DetectFrequencyPerYear_WhenPaidEveryMonth_ReturnsTwelve()
    {
        var credits = new[]
        {
            Credit.Create(new DateTime(2026, 1, 15), Credit.CreditType.Dividend, 10m),
            Credit.Create(new DateTime(2026, 2, 15), Credit.CreditType.Dividend, 10m),
            Credit.Create(new DateTime(2026, 3, 15), Credit.CreditType.Dividend, 10m)
        };

        CreditFrequencyAnalyzer.DetectFrequencyPerYear(credits).Should().Be(12);
    }

    [Fact]
    public void DetectFrequencyPerYear_WhenPaidEveryQuarter_ReturnsFour()
    {
        var credits = new[]
        {
            Credit.Create(new DateTime(2026, 1, 15), Credit.CreditType.Dividend, 10m),
            Credit.Create(new DateTime(2026, 4, 15), Credit.CreditType.Dividend, 10m),
            Credit.Create(new DateTime(2026, 7, 15), Credit.CreditType.Dividend, 10m)
        };

        CreditFrequencyAnalyzer.DetectFrequencyPerYear(credits).Should().Be(4);
    }

    [Fact]
    public void DetectFrequencyPerYear_WhenGapTooLarge_ReturnsNull()
    {
        var credits = new[]
        {
            Credit.Create(new DateTime(2024, 1, 15), Credit.CreditType.Dividend, 10m),
            Credit.Create(new DateTime(2026, 1, 15), Credit.CreditType.Dividend, 10m)
        };

        CreditFrequencyAnalyzer.DetectFrequencyPerYear(credits).Should().BeNull();
    }
}
