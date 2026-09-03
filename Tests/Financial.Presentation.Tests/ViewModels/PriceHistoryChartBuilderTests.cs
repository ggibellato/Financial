using Financial.Investment.Application.DTOs;
using Financial.Presentation.App.ViewModels.Investment;
using FluentAssertions;
using OxyPlot.Series;

namespace Financial.Presentation.Tests.ViewModels;

public class PriceHistoryChartBuilderTests
{
    [Fact]
    public void Build_NoEntriesOrTransactions_ReturnsModelWithNoSeries()
    {
        var model = PriceHistoryChartBuilder.Build([], []);

        model.Series.Should().BeEmpty();
    }

    [Fact]
    public void Build_WithEntries_ProducesLineSeriesOrderedByDate()
    {
        var entries = new List<AssetPriceSnapshotDTO>
        {
            new() { Date = new DateOnly(2026, 8, 15), Price = 110m, IsManual = true },
            new() { Date = new DateOnly(2026, 8, 14), Price = 100m, IsManual = false },
        };

        var model = PriceHistoryChartBuilder.Build(entries, []);

        var line = model.Series.OfType<LineSeries>().Should().ContainSingle().Subject;
        line.Points.Should().HaveCount(2);
        line.Points[0].Y.Should().Be(100d);
        line.Points[1].Y.Should().Be(110d);
    }

    [Fact]
    public void Build_SeparatesManualAndAutomaticPointsIntoDistinctSeries()
    {
        var entries = new List<AssetPriceSnapshotDTO>
        {
            new() { Date = new DateOnly(2026, 8, 14), Price = 100m, IsManual = false },
            new() { Date = new DateOnly(2026, 8, 15), Price = 105m, IsManual = true },
        };

        var model = PriceHistoryChartBuilder.Build(entries, []);

        var scatterSeries = model.Series.OfType<ScatterSeries>().ToList();
        scatterSeries.Should().HaveCount(2);
        scatterSeries.Should().Contain(s => s.Title == "Manual" && s.Points.Count == 1);
        scatterSeries.Should().Contain(s => s.Title == "Automatic" && s.Points.Count == 1);
    }

    [Fact]
    public void Build_NoTransactions_ProducesNoBuyOrSellSeries()
    {
        var entries = new List<AssetPriceSnapshotDTO>
        {
            new() { Date = new DateOnly(2026, 8, 14), Price = 100m, IsManual = false },
        };

        var model = PriceHistoryChartBuilder.Build(entries, []);

        model.Series.OfType<ScatterSeries>().Should().NotContain(s => s.Title == "Buy" || s.Title == "Sell");
    }

    [Fact]
    public void Build_SeparatesBuyAndSellPointsIntoDistinctSeriesOrderedByDate()
    {
        var transactions = new List<TransactionDTO>
        {
            new() { Id = Guid.NewGuid(), Date = new DateTime(2026, 8, 15), Type = "Sell", Quantity = 5m, UnitPrice = 120m, Fees = 1m, TotalPrice = 599m },
            new() { Id = Guid.NewGuid(), Date = new DateTime(2026, 8, 14), Type = "Buy", Quantity = 10m, UnitPrice = 100m, Fees = 1m, TotalPrice = 1001m },
        };

        var model = PriceHistoryChartBuilder.Build([], transactions);

        var scatterSeries = model.Series.OfType<ScatterSeries>().ToList();
        scatterSeries.Should().HaveCount(2);
        var buy = scatterSeries.Should().ContainSingle(s => s.Title == "Buy").Subject;
        buy.Points.Should().ContainSingle(p => p.Y == 100d);
        var sell = scatterSeries.Should().ContainSingle(s => s.Title == "Sell").Subject;
        sell.Points.Should().ContainSingle(p => p.Y == 120d);
    }

    [Fact]
    public void Build_CombinesEntriesAndTransactionsIntoOneLineSeriesOrderedByDate()
    {
        var entries = new List<AssetPriceSnapshotDTO>
        {
            new() { Date = new DateOnly(2026, 8, 20), Price = 130m, IsManual = false },
        };
        var transactions = new List<TransactionDTO>
        {
            new() { Id = Guid.NewGuid(), Date = new DateTime(2026, 8, 15), Type = "Sell", Quantity = 5m, UnitPrice = 120m, Fees = 1m, TotalPrice = 599m },
            new() { Id = Guid.NewGuid(), Date = new DateTime(2026, 8, 14), Type = "Buy", Quantity = 10m, UnitPrice = 100m, Fees = 1m, TotalPrice = 1001m },
        };

        var model = PriceHistoryChartBuilder.Build(entries, transactions);

        var line = model.Series.OfType<LineSeries>().Should().ContainSingle().Subject;
        line.Points.Select(p => p.Y).Should().Equal(100d, 120d, 130d);
    }
}
