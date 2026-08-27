using Financial.Presentation.App.ViewModels.Investment;
using FluentAssertions;
using OxyPlot;
using OxyPlot.Annotations;

namespace Financial.Presentation.Tests.ViewModels;

public class OxyPlotChartBuilderHelpersTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void ComputeLabelStep_PlotWidthNotYetKnown_ReturnsOne(double plotWidth)
    {
        OxyPlotChartBuilderHelpers.ComputeLabelStep(plotWidth, labelCount: 24).Should().Be(1);
    }

    [Fact]
    public void ComputeLabelStep_AllLabelsFit_ReturnsOne()
    {
        OxyPlotChartBuilderHelpers.ComputeLabelStep(plotWidth: 500, labelCount: 5).Should().Be(1);
    }

    [Fact]
    public void ComputeLabelStep_NarrowPlotManyLabels_ThinsProportionally()
    {
        // 300px / 52 MinLabelWidth => 5 max visible labels; 24 labels / 5 => ceil(4.8) = 5.
        OxyPlotChartBuilderHelpers.ComputeLabelStep(plotWidth: 300, labelCount: 24).Should().Be(5);
    }

    [Fact]
    public void RemoveAnnotationsByTag_RemovesOnlyMatchingTag()
    {
        var model = new PlotModel();
        model.Annotations.Add(new TextAnnotation { Tag = "Keep" });
        model.Annotations.Add(new TextAnnotation { Tag = "Remove" });
        model.Annotations.Add(new TextAnnotation { Tag = "Keep" });

        OxyPlotChartBuilderHelpers.RemoveAnnotationsByTag(model, "Remove");

        model.Annotations.Should().HaveCount(2);
        model.Annotations.Should().OnlyContain(a => (string)a.Tag == "Keep");
    }
}
