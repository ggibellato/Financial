using OxyPlot;
using OxyPlot.Series;

namespace Financial.Presentation.App.ViewModels.Investment;

internal static class BrokerBreakdownChartBuilder
{
    // Same validated categorical palette used by the web equivalent (F07),
    // for cross-platform visual consistency.
    private static readonly OxyColor[] Palette =
    [
        OxyColor.Parse("#2a78d6"),
        OxyColor.Parse("#1baf7a"),
        OxyColor.Parse("#eda100"),
        OxyColor.Parse("#008300"),
        OxyColor.Parse("#4a3aa7"),
        OxyColor.Parse("#e34948"),
        OxyColor.Parse("#e87ba4"),
        OxyColor.Parse("#eb6834"),
    ];

    public static PlotModel Build(IReadOnlyList<(string Name, decimal Value)> slices)
    {
        var model = new PlotModel();

        if (slices.Count == 0)
            return model;

        var series = new PieSeries
        {
            StrokeThickness = 1.0,
            AngleSpan = 360,
            StartAngle = 0,
            TrackerFormatString = "{1}\n{2:N2}\n{3:P1}",
        };

        for (var i = 0; i < slices.Count; i++)
        {
            var (name, value) = slices[i];
            series.Slices.Add(new PieSlice(name, (double)value) { Fill = Palette[i % Palette.Length] });
        }

        model.Series.Add(series);
        return model;
    }
}
