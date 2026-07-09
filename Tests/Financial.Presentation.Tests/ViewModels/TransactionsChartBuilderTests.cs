using Financial.Presentation.App.ViewModels;
using FluentAssertions;
using OxyPlot.Series;

namespace Financial.Presentation.Tests.ViewModels;

public class TransactionsChartBuilderTests
{
    [Fact]
    public void Build_BarMode_ProducesOneRectangleBarSeries()
    {
        var months = new List<TransactionMonthNet>
        {
            new(new DateTime(2026, 6, 1), 500m),
            new(new DateTime(2026, 7, 1), -200m),
        };

        var model = TransactionsChartBuilder.Build(months, ChartTypeMode.Bar);

        model.Series.Should().ContainSingle().Which.Should().BeOfType<RectangleBarSeries>();
    }

    [Fact]
    public void Build_LineMode_ProducesOneLineSeriesWithMatchingPointCount()
    {
        var months = new List<TransactionMonthNet>
        {
            new(new DateTime(2026, 6, 1), 500m),
            new(new DateTime(2026, 7, 1), -200m),
        };

        var model = TransactionsChartBuilder.Build(months, ChartTypeMode.Line);

        var series = model.Series.Should().ContainSingle().Which.Should().BeOfType<LineSeries>().Subject;
        series.Points.Should().HaveCount(2);
    }

    [Fact]
    public void Build_EmptyMonths_ProducesNoSeries()
    {
        var model = TransactionsChartBuilder.Build(new List<TransactionMonthNet>(), ChartTypeMode.Bar);

        model.Series.Should().BeEmpty();
    }
}
