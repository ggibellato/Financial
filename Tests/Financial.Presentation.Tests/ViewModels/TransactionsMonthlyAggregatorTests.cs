using Financial.Presentation.App.Helpers;
using Financial.Presentation.App.ViewModels;
using FluentAssertions;

namespace Financial.Presentation.Tests.ViewModels;

public class TransactionsMonthlyAggregatorTests
{
    private static readonly DateTime ReferenceDate = new(2026, 7, 15);

    [Fact]
    public void BuildMonthlyNetInvested_ZeroFillsMonthsWithNoTransactions()
    {
        var transactions = new[]
        {
            (Date: new DateTime(2026, 5, 10), Type: "Buy", TotalPrice: 500m),
        };

        var result = TransactionsMonthlyAggregator.BuildMonthlyNetInvested(
            transactions, PeriodFilter.Last3Months, ReferenceDate);

        result.Should().HaveCount(3);
        result.Select(m => m.Month).Should().Equal(
            new DateTime(2026, 5, 1), new DateTime(2026, 6, 1), new DateTime(2026, 7, 1));
        result[0].NetInvested.Should().Be(500m);
        result[1].NetInvested.Should().Be(0m);
        result[2].NetInvested.Should().Be(0m);
    }

    [Fact]
    public void BuildMonthlyNetInvested_ComputesBuyMinusSellPerMonth()
    {
        var transactions = new[]
        {
            (Date: new DateTime(2026, 7, 5), Type: "Buy", TotalPrice: 1000m),
            (Date: new DateTime(2026, 7, 20), Type: "Sell", TotalPrice: 300m),
        };

        var result = TransactionsMonthlyAggregator.BuildMonthlyNetInvested(
            transactions, PeriodFilter.ThisMonth, ReferenceDate);

        result.Should().ContainSingle();
        result[0].NetInvested.Should().Be(700m);
    }

    [Fact]
    public void BuildMonthlyNetInvested_AllTime_StartsFromEarliestTransaction()
    {
        var transactions = new[]
        {
            (Date: new DateTime(2023, 1, 15), Type: "Buy", TotalPrice: 100m),
            (Date: new DateTime(2026, 6, 1), Type: "Buy", TotalPrice: 200m),
        };

        var result = TransactionsMonthlyAggregator.BuildMonthlyNetInvested(
            transactions, PeriodFilter.AllTime, ReferenceDate);

        result.First().Month.Should().Be(new DateTime(2023, 1, 1));
        result.Last().Month.Should().Be(new DateTime(2026, 7, 1));
    }

    [Fact]
    public void BuildMonthlyNetInvested_EmptyInput_ReturnsSingleZeroMonth()
    {
        var result = TransactionsMonthlyAggregator.BuildMonthlyNetInvested(
            Array.Empty<(DateTime Date, string Type, decimal TotalPrice)>(),
            PeriodFilter.ThisMonth,
            ReferenceDate);

        result.Should().ContainSingle();
        result[0].Month.Should().Be(new DateTime(2026, 7, 1));
        result[0].NetInvested.Should().Be(0m);
    }

    [Fact]
    public void BuildMonthlyNetInvested_SellExceedingBuy_ProducesNegativeMonth()
    {
        var transactions = new[]
        {
            (Date: new DateTime(2026, 7, 1), Type: "Sell", TotalPrice: 400m),
        };

        var result = TransactionsMonthlyAggregator.BuildMonthlyNetInvested(
            transactions, PeriodFilter.ThisMonth, ReferenceDate);

        result[0].NetInvested.Should().Be(-400m);
    }
}
