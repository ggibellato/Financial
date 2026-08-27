using OxyPlot;
using OxyPlot.Annotations;
using OxyPlot.Axes;

namespace Financial.Presentation.App.ViewModels.Investment;

internal static class OxyPlotChartBuilderHelpers
{
    private const double MinLabelWidth = 52;

    internal static CategoryAxis CreateCategoryAxis() => new()
    {
        Position = AxisPosition.Bottom,
        GapWidth = 0.2,
        IsPanEnabled = false,
        IsZoomEnabled = false
    };

    internal static LinearAxis CreateValueAxis() => new()
    {
        Position = AxisPosition.Left,
        MajorGridlineStyle = LineStyle.Solid,
        MinorGridlineStyle = LineStyle.Dot,
        IsPanEnabled = false,
        IsZoomEnabled = false,
        MaximumPadding = 0.1
    };

    /// <summary>
    /// plotWidth is 0 until the PlotView's first SizeChanged fires, which can happen after this is
    /// first called from Build(). Every label shows (step=1) until a real width arrives to thin them
    /// by density, rather than showing none at all in the meantime.
    /// </summary>
    internal static int ComputeLabelStep(double plotWidth, int labelCount)
    {
        if (plotWidth <= 0)
        {
            return 1;
        }

        var maxVisibleLabels = Math.Max(1, (int)Math.Floor(plotWidth / MinLabelWidth));
        return Math.Max(1, (int)Math.Ceiling((double)labelCount / maxVisibleLabels));
    }

    internal static void RemoveAnnotationsByTag(PlotModel model, string tag)
    {
        for (var index = model.Annotations.Count - 1; index >= 0; index--)
        {
            if (model.Annotations[index].Tag is string existingTag && string.Equals(existingTag, tag, StringComparison.Ordinal))
            {
                model.Annotations.RemoveAt(index);
            }
        }
    }

    internal static void AddValueLabelAnnotation(PlotModel model, double x, double y, decimal displayValue, string tag) =>
        model.Annotations.Add(new TextAnnotation
        {
            Text = displayValue.ToString("N2"),
            TextPosition = new DataPoint(x, y),
            TextHorizontalAlignment = HorizontalAlignment.Center,
            TextVerticalAlignment = displayValue >= 0 ? VerticalAlignment.Bottom : VerticalAlignment.Top,
            Offset = displayValue >= 0 ? new ScreenVector(0, -6) : new ScreenVector(0, 6),
            TextColor = OxyColors.Black,
            Stroke = OxyColors.Undefined,
            Tag = tag,
            ClipByXAxis = true,
            ClipByYAxis = false
        });
}
