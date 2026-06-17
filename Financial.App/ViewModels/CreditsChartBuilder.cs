using OxyPlot;
using OxyPlot.Annotations;
using OxyPlot.Axes;
using OxyPlot.Series;

namespace Financial.Presentation.App.ViewModels;

internal static class CreditsChartBuilder
{
    private const string CreditsValueLabelTag = "CreditsValueLabel";
    private const double BarGroupWidth = 0.8;
    private const double MinLabelWidth = 52;

    public static PlotModel Build(
        IReadOnlyList<CreditsMonthTypeTotals> grouped,
        IReadOnlyList<string> creditTypes,
        CreditsTypeChartMode mode)
    {
        var (model, categoryAxis) = CreateModelWithAxes();

        if (grouped.Count == 0 || creditTypes.Count == 0)
            return model;

        var seriesByType = CreateBarSeries(creditTypes);
        PopulateBarItems(grouped, creditTypes, seriesByType, categoryAxis, mode);

        foreach (var series in seriesByType)
            model.Series.Add(series);

        return model;
    }

    public static void ApplyLabelDensity(
        PlotModel model,
        double plotWidth,
        IReadOnlyList<CreditsMonthTypeTotals> chartMonths,
        IReadOnlyList<string> chartTypes,
        CreditsTypeChartMode mode)
    {
        if (plotWidth <= 0) return;

        var categoryAxis = model.Axes.OfType<CategoryAxis>().FirstOrDefault();
        if (categoryAxis == null || categoryAxis.Labels.Count == 0) return;

        var maxVisibleLabels = Math.Max(1, (int)Math.Floor(plotWidth / MinLabelWidth));
        var step = Math.Max(1, (int)Math.Ceiling((double)categoryAxis.Labels.Count / maxVisibleLabels));
        categoryAxis.MajorStep = step;
        categoryAxis.MinorStep = 1;
        UpdateValueLabels(model, step, chartMonths, chartTypes, mode);
        model.InvalidatePlot(false);
    }

    private static (PlotModel model, CategoryAxis categoryAxis) CreateModelWithAxes()
    {
        var model = new PlotModel { Title = "Credits by Month" };
        var categoryAxis = new CategoryAxis
        {
            Position = AxisPosition.Bottom,
            GapWidth = 0.2,
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
        model.Axes.Add(categoryAxis);
        model.Axes.Add(valueAxis);
        return (model, categoryAxis);
    }

    private static List<RectangleBarSeries> CreateBarSeries(IReadOnlyList<string> creditTypes)
    {
        var palette = BuildBluePalette(creditTypes.Count);
        return creditTypes
            .Select((type, index) => new RectangleBarSeries
            {
                Title = type,
                FillColor = palette[index],
                StrokeColor = OxyColors.SlateGray,
                StrokeThickness = 1
            })
            .ToList();
    }

    private static void PopulateBarItems(
        IReadOnlyList<CreditsMonthTypeTotals> grouped,
        IReadOnlyList<string> creditTypes,
        List<RectangleBarSeries> seriesByType,
        CategoryAxis categoryAxis,
        CreditsTypeChartMode mode)
    {
        var halfGroupWidth = BarGroupWidth / 2;
        var barWidth = BarGroupWidth / creditTypes.Count;

        for (var monthIndex = 0; monthIndex < grouped.Count; monthIndex++)
        {
            var month = grouped[monthIndex];
            categoryAxis.Labels.Add(month.Month.ToString("MM/yyyy"));

            var positiveStack = 0.0;
            var negativeStack = 0.0;

            for (var typeIndex = 0; typeIndex < creditTypes.Count; typeIndex++)
            {
                var type = creditTypes[typeIndex];
                var value = month.TotalsByType.TryGetValue(type, out var total) ? (double)total : 0d;
                double x0, x1, y0, y1;

                if (mode == CreditsTypeChartMode.Stacked)
                {
                    x0 = monthIndex - halfGroupWidth;
                    x1 = monthIndex + halfGroupWidth;
                    if (value >= 0)
                    {
                        y0 = positiveStack;
                        y1 = positiveStack + value;
                        positiveStack = y1;
                    }
                    else
                    {
                        y1 = negativeStack;
                        y0 = negativeStack + value;
                        negativeStack = y0;
                    }
                }
                else
                {
                    x0 = monthIndex - halfGroupWidth + (barWidth * typeIndex);
                    x1 = x0 + barWidth;
                    y0 = Math.Min(0, value);
                    y1 = Math.Max(0, value);
                }

                seriesByType[typeIndex].Items.Add(new RectangleBarItem(x0, y0, x1, y1));
            }
        }
    }

    private static void UpdateValueLabels(
        PlotModel model,
        int step,
        IReadOnlyList<CreditsMonthTypeTotals> chartMonths,
        IReadOnlyList<string> chartTypes,
        CreditsTypeChartMode mode)
    {
        for (var index = model.Annotations.Count - 1; index >= 0; index--)
        {
            if (model.Annotations[index].Tag is string tag &&
                string.Equals(tag, CreditsValueLabelTag, StringComparison.Ordinal))
            {
                model.Annotations.RemoveAt(index);
            }
        }

        if (chartMonths.Count == 0 || chartTypes.Count == 0) return;

        var halfGroupWidth = BarGroupWidth / 2;
        var barWidth = BarGroupWidth / chartTypes.Count;

        for (var monthIndex = 0; monthIndex < chartMonths.Count; monthIndex += step)
        {
            var month = chartMonths[monthIndex];

            if (mode == CreditsTypeChartMode.Stacked)
            {
                AddValueLabel(model, monthIndex, month.Total, month.Total);
                continue;
            }

            for (var typeIndex = 0; typeIndex < chartTypes.Count; typeIndex++)
            {
                var type = chartTypes[typeIndex];
                var value = month.TotalsByType.TryGetValue(type, out var total) ? total : 0m;
                if (value == 0) continue;

                var x0 = monthIndex - halfGroupWidth + (barWidth * typeIndex);
                AddValueLabel(model, x0 + (barWidth / 2), value, value);
            }
        }
    }

    private static void AddValueLabel(PlotModel model, double x, decimal value, decimal labelYValue)
    {
        var y = (double)labelYValue;
        model.Annotations.Add(new TextAnnotation
        {
            Text = value.ToString("N2"),
            TextPosition = new DataPoint(x, y),
            TextHorizontalAlignment = OxyPlot.HorizontalAlignment.Center,
            TextVerticalAlignment = value >= 0 ? OxyPlot.VerticalAlignment.Bottom : OxyPlot.VerticalAlignment.Top,
            Offset = value >= 0 ? new ScreenVector(0, -6) : new ScreenVector(0, 6),
            TextColor = OxyColors.Black,
            Stroke = OxyColors.Undefined,
            Tag = CreditsValueLabelTag,
            ClipByXAxis = true,
            ClipByYAxis = false
        });
    }

    private static IReadOnlyList<OxyColor> BuildBluePalette(int count)
    {
        if (count <= 0) return Array.Empty<OxyColor>();
        if (count == 1) return new[] { OxyColors.SteelBlue };

        var start = OxyColor.FromRgb(173, 216, 230);
        var end = OxyColor.FromRgb(8, 81, 156);
        var colors = new List<OxyColor>(count);
        for (var i = 0; i < count; i++)
        {
            var t = (double)i / (count - 1);
            colors.Add(OxyColor.FromRgb(
                LerpByte(start.R, end.R, t),
                LerpByte(start.G, end.G, t),
                LerpByte(start.B, end.B, t)));
        }
        return colors;
    }

    private static byte LerpByte(byte from, byte to, double t) =>
        (byte)Math.Round(from + ((to - from) * t));
}
