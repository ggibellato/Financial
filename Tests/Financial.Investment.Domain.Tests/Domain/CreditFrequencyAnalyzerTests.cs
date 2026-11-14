using Financial.Investment.Domain.Entities;
using Financial.Investment.Domain.Rules;
using FluentAssertions;

namespace Financial.Investment.Domain.Tests;

[Trait("Category", "Unit")]
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

    [Theory]
    [InlineData(new[] { 0, 1 }, 12)]
    [InlineData(new[] { 0, 1, 3 }, 12)]
    [InlineData(new[] { 0, 3, 7 }, 4)]
    [InlineData(new[] { 0, 4, 8 }, 3)]
    [InlineData(new[] { 0, 5 }, 3)]
    public void DetectFrequencyPerYear_AverageGapUpToEachLimit_FallsInThatBand(int[] monthOffsets, int expected)
    {
        var credits = monthOffsets
            .Select(offset => Credit.Create(new DateTime(2025, 1, 15).AddMonths(offset), Credit.CreditType.Dividend, 10m))
            .ToArray();

        CreditFrequencyAnalyzer.DetectFrequencyPerYear(credits).Should().Be(expected);
    }

    [Fact]
    public void DetectFrequencyPerYear_MonthlyPaymentsAcrossAYearEnd_ReturnsTwelve()
    {
        var credits = new[]
        {
            Credit.Create(new DateTime(2025, 11, 15), Credit.CreditType.Dividend, 10m),
            Credit.Create(new DateTime(2025, 12, 15), Credit.CreditType.Dividend, 10m),
            Credit.Create(new DateTime(2026, 1, 15), Credit.CreditType.Dividend, 10m)
        };

        CreditFrequencyAnalyzer.DetectFrequencyPerYear(credits).Should().Be(12);
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
