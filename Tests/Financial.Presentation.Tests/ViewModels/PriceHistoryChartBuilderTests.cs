using Financial.Investment.Application.DTOs;
using Financial.Presentation.App.ViewModels.Investment;
using FluentAssertions;
using OxyPlot.Series;

namespace Financial.Presentation.Tests.ViewModels;

public class PriceHistoryChartBuilderTests
{
    [Fact]
    public void Build_NoEntries_ReturnsModelWithNoSeries()
    {
        var model = PriceHistoryChartBuilder.Build([]);

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

        var model = PriceHistoryChartBuilder.Build(entries);

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

        var model = PriceHistoryChartBuilder.Build(entries);

        var scatterSeries = model.Series.OfType<ScatterSeries>().ToList();
        scatterSeries.Should().HaveCount(2);
        scatterSeries.Should().Contain(s => s.Title == "Manual" && s.Points.Count == 1);
        scatterSeries.Should().Contain(s => s.Title == "Automatic" && s.Points.Count == 1);
    }
}
