using OxyPlot;
using OxyPlot.Annotations;
using OxyPlot.Axes;
using OxyPlot.Series;

namespace Financial.Presentation.App.ViewModels;

internal static class TransactionsChartBuilder
{
    private const string TransactionsValueLabelTag = "TransactionsValueLabel";
    private const double BarWidth = 0.8;
    private const double MinLabelWidth = 52;
    private static readonly OxyColor NeutralColor = OxyColor.FromRgb(107, 114, 128);

    public static PlotModel Build(IReadOnlyList<TransactionMonthNet> months, ChartTypeMode mode)
    {
        var (model, categoryAxis) = CreateModelWithAxes();

        if (months.Count == 0)
            return model;

        foreach (var month in months)
            categoryAxis.Labels.Add(month.Month.ToString("MM/yyyy"));

        model.Series.Add(mode == ChartTypeMode.Bar
            ? BuildBarSeries(months)
            : BuildLineSeries(months));

        return model;
    }

    public static void ApplyLabelDensity(
        PlotModel model,
        double plotWidth,
        IReadOnlyList<TransactionMonthNet> months)
    {
        if (plotWidth <= 0) return;

        var categoryAxis = model.Axes.OfType<CategoryAxis>().FirstOrDefault();
        if (categoryAxis == null || categoryAxis.Labels.Count == 0) return;

        var maxVisibleLabels = Math.Max(1, (int)Math.Floor(plotWidth / MinLabelWidth));
        var step = Math.Max(1, (int)Math.Ceiling((double)categoryAxis.Labels.Count / maxVisibleLabels));
        categoryAxis.MajorStep = step;
        categoryAxis.MinorStep = 1;
        UpdateValueLabels(model, step, months);
        model.InvalidatePlot(false);
    }

    private static (PlotModel model, CategoryAxis categoryAxis) CreateModelWithAxes()
    {
        var model = new PlotModel { Title = "Net Invested by Month" };
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
            MaximumPadding = 0.1,
            MinimumPadding = 0.1
        };
        model.Axes.Add(categoryAxis);
        model.Axes.Add(valueAxis);
        return (model, categoryAxis);
    }

    private static RectangleBarSeries BuildBarSeries(IReadOnlyList<TransactionMonthNet> months)
    {
        var series = new RectangleBarSeries
        {
            FillColor = NeutralColor,
            StrokeColor = OxyColors.SlateGray,
            StrokeThickness = 1
        };

        var half = BarWidth / 2;
        for (var monthIndex = 0; monthIndex < months.Count; monthIndex++)
        {
            var value = (double)months[monthIndex].NetInvested;
            var x0 = monthIndex - half;
            var x1 = monthIndex + half;
            var y0 = Math.Min(0, value);
            var y1 = Math.Max(0, value);
            series.Items.Add(new RectangleBarItem(x0, y0, x1, y1));
        }

        return series;
    }

    private static LineSeries BuildLineSeries(IReadOnlyList<TransactionMonthNet> months)
    {
        var series = new LineSeries
        {
            Color = NeutralColor,
            StrokeThickness = 2,
            MarkerType = MarkerType.Circle,
            MarkerSize = 3,
            MarkerFill = NeutralColor
        };

        for (var monthIndex = 0; monthIndex < months.Count; monthIndex++)
            series.Points.Add(new DataPoint(monthIndex, (double)months[monthIndex].NetInvested));

        return series;
    }

    private static void UpdateValueLabels(PlotModel model, int step, IReadOnlyList<TransactionMonthNet> months)
    {
        for (var index = model.Annotations.Count - 1; index >= 0; index--)
        {
            if (model.Annotations[index].Tag is string tag &&
                string.Equals(tag, TransactionsValueLabelTag, StringComparison.Ordinal))
            {
                model.Annotations.RemoveAt(index);
            }
        }

        if (months.Count == 0) return;

        for (var monthIndex = 0; monthIndex < months.Count; monthIndex += step)
        {
            var value = months[monthIndex].NetInvested;
            if (value == 0) continue;
            AddValueLabel(model, monthIndex, value);
        }
    }

    private static void AddValueLabel(PlotModel model, double x, decimal value)
    {
        var y = (double)value;
        model.Annotations.Add(new TextAnnotation
        {
            Text = value.ToString("N2"),
            TextPosition = new DataPoint(x, y),
            TextHorizontalAlignment = OxyPlot.HorizontalAlignment.Center,
            TextVerticalAlignment = value >= 0 ? OxyPlot.VerticalAlignment.Bottom : OxyPlot.VerticalAlignment.Top,
            Offset = value >= 0 ? new ScreenVector(0, -6) : new ScreenVector(0, 6),
            TextColor = OxyColors.Black,
            Stroke = OxyColors.Undefined,
            Tag = TransactionsValueLabelTag,
            ClipByXAxis = true,
            ClipByYAxis = false
        });
    }
}
