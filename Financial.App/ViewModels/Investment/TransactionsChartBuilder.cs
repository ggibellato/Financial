using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;

namespace Financial.Presentation.App.ViewModels.Investment;

internal static class TransactionsChartBuilder
{
    private const string TransactionsValueLabelTag = "TransactionsValueLabel";
    private const double BarWidth = 0.8;
    // Matches CreditsChartBuilder/PriceHistoryChartBuilder's OxyColors.SteelBlue
    // (docs/ui/forms-data-and-visualisations.md's "Series color" rule) — not a
    // neutral/grey, single-series charts are blue on both platforms.
    private static readonly OxyColor SeriesColor = OxyColors.SteelBlue;

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
        var categoryAxis = model.Axes.OfType<CategoryAxis>().FirstOrDefault();
        if (categoryAxis == null || categoryAxis.Labels.Count == 0) return;

        var step = OxyPlotChartBuilderHelpers.ComputeLabelStep(plotWidth, categoryAxis.Labels.Count);
        categoryAxis.MajorStep = step;
        categoryAxis.MinorStep = 1;
        UpdateValueLabels(model, step, months);
        model.InvalidatePlot(false);
    }

    private static (PlotModel model, CategoryAxis categoryAxis) CreateModelWithAxes()
    {
        var model = new PlotModel { Title = "Net Invested by Month" };
        var categoryAxis = OxyPlotChartBuilderHelpers.CreateCategoryAxis();
        var valueAxis = OxyPlotChartBuilderHelpers.CreateValueAxis();
        valueAxis.MinimumPadding = 0.1;
        model.Axes.Add(categoryAxis);
        model.Axes.Add(valueAxis);
        return (model, categoryAxis);
    }

    private static RectangleBarSeries BuildBarSeries(IReadOnlyList<TransactionMonthNet> months)
    {
        var series = new RectangleBarSeries
        {
            FillColor = SeriesColor,
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
            Color = SeriesColor,
            StrokeThickness = 2,
            MarkerType = MarkerType.Circle,
            MarkerSize = 3,
            MarkerFill = SeriesColor
        };

        for (var monthIndex = 0; monthIndex < months.Count; monthIndex++)
            series.Points.Add(new DataPoint(monthIndex, (double)months[monthIndex].NetInvested));

        return series;
    }

    private static void UpdateValueLabels(PlotModel model, int step, IReadOnlyList<TransactionMonthNet> months)
    {
        OxyPlotChartBuilderHelpers.RemoveAnnotationsByTag(model, TransactionsValueLabelTag);

        if (months.Count == 0) return;

        for (var monthIndex = 0; monthIndex < months.Count; monthIndex += step)
        {
            var value = months[monthIndex].NetInvested;
            if (value == 0) continue;
            AddValueLabel(model, monthIndex, value);
        }
    }

    private static void AddValueLabel(PlotModel model, double x, decimal value) =>
        OxyPlotChartBuilderHelpers.AddValueLabelAnnotation(model, x, (double)value, value, TransactionsValueLabelTag);
}
