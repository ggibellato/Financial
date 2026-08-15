using Financial.Investment.Application.DTOs;
using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;

namespace Financial.Presentation.App.ViewModels.Investment;

internal static class PriceHistoryChartBuilder
{
    public static PlotModel Build(IReadOnlyList<AssetPriceSnapshotDTO> entries)
    {
        var model = CreateModelWithAxes();

        if (entries.Count == 0)
            return model;

        var ordered = entries.OrderBy(entry => entry.Date).ToList();

        var line = new LineSeries
        {
            Title = "Price",
            Color = OxyColors.SteelBlue,
            StrokeThickness = 2
        };
        foreach (var entry in ordered)
            line.Points.Add(new DataPoint(ToPlotDate(entry), (double)entry.Price));
        model.Series.Add(line);

        var manualPoints = new ScatterSeries
        {
            Title = "Manual",
            MarkerType = MarkerType.Triangle,
            MarkerFill = OxyColors.OrangeRed,
            MarkerSize = 5
        };
        var automaticPoints = new ScatterSeries
        {
            Title = "Automatic",
            MarkerType = MarkerType.Circle,
            MarkerFill = OxyColors.SteelBlue,
            MarkerSize = 4
        };
        foreach (var entry in ordered)
        {
            var target = entry.IsManual ? manualPoints : automaticPoints;
            target.Points.Add(new ScatterPoint(ToPlotDate(entry), (double)entry.Price));
        }
        model.Series.Add(manualPoints);
        model.Series.Add(automaticPoints);

        return model;
    }

    private static double ToPlotDate(AssetPriceSnapshotDTO entry) =>
        DateTimeAxis.ToDouble(entry.Date.ToDateTime(TimeOnly.MinValue));

    private static PlotModel CreateModelWithAxes()
    {
        var model = new PlotModel { Title = "Price History" };
        var dateAxis = new DateTimeAxis
        {
            Position = AxisPosition.Bottom,
            StringFormat = "MM/yyyy",
            IsPanEnabled = false,
            IsZoomEnabled = false
        };
        var valueAxis = new LinearAxis
        {
            Position = AxisPosition.Left,
            MajorGridlineStyle = LineStyle.Solid,
            MinorGridlineStyle = LineStyle.Dot,
            IsPanEnabled = false,
            IsZoomEnabled = false,
            MaximumPadding = 0.1
        };
        model.Axes.Add(dateAxis);
        model.Axes.Add(valueAxis);
        return model;
    }
}
