using Financial.Presentation.App.ViewModels;
using FluentAssertions;
using OxyPlot.Series;

namespace Financial.Presentation.Tests.ViewModels;

public class CreditsChartBuilderTests
{
    private static List<CreditsMonthTypeTotals> BuildMonths() => new()
    {
        new CreditsMonthTypeTotals(
            new DateTime(2026, 6, 1),
            new Dictionary<string, decimal> { ["Dividend"] = 100m, ["Rent"] = 50m }),
        new CreditsMonthTypeTotals(
            new DateTime(2026, 7, 1),
            new Dictionary<string, decimal> { ["Dividend"] = 200m, ["Rent"] = 30m }),
    };

    [Fact]
    public void Build_BarMode_ProducesOneSeriesPerCreditType()
    {
        var months = BuildMonths();
        var creditTypes = new List<string> { "Dividend", "Rent" };

        var model = CreditsChartBuilder.Build(months, creditTypes, CreditsTypeChartMode.Stacked, CreditsChartType.Bar);

        model.Series.Should().HaveCount(2);
        model.Series.Should().AllBeOfType<RectangleBarSeries>();
    }

    [Fact]
    public void Build_LineMode_Grouped_ProducesSingleSeriesOnTotal()
    {
        var months = BuildMonths();
        var creditTypes = new List<string> { "Dividend", "Rent" };

        var model = CreditsChartBuilder.Build(months, creditTypes, CreditsTypeChartMode.Grouped, CreditsChartType.Line);

        var series = model.Series.Should().ContainSingle().Which.Should().BeOfType<LineSeries>().Subject;
        series.Points.Should().HaveCount(2);
        series.Points[0].Y.Should().Be(150d);
        series.Points[1].Y.Should().Be(230d);
    }

    [Fact]
    public void Build_LineMode_Stacked_ProducesOneSeriesPerCreditType()
    {
        var months = BuildMonths();
        var creditTypes = new List<string> { "Dividend", "Rent" };

        var model = CreditsChartBuilder.Build(months, creditTypes, CreditsTypeChartMode.Stacked, CreditsChartType.Line);

        model.Series.Should().HaveCount(2);
        var lineSeries = model.Series.Cast<LineSeries>().ToList();
        var dividendSeries = lineSeries.Should().ContainSingle(s => s.Title == "Dividend").Which;
        dividendSeries.Points.Select(p => p.Y).Should().Equal(100d, 200d);
        var rentSeries = lineSeries.Should().ContainSingle(s => s.Title == "Rent").Which;
        rentSeries.Points.Select(p => p.Y).Should().Equal(50d, 30d);
    }

    [Fact]
    public void Build_EmptyMonths_ProducesNoSeries()
    {
        var creditTypes = new List<string> { "Dividend", "Rent" };

        var model = CreditsChartBuilder.Build(
            new List<CreditsMonthTypeTotals>(), creditTypes, CreditsTypeChartMode.Stacked, CreditsChartType.Line);

        model.Series.Should().BeEmpty();
    }
}
