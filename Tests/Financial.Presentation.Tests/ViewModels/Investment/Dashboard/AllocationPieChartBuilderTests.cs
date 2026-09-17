using Financial.Presentation.App.ViewModels.Investment.Dashboard;
using FluentAssertions;
using OxyPlot.Series;

namespace Financial.Presentation.Tests.ViewModels.Investment.Dashboard;

public class AllocationPieChartBuilderTests
{
    [Fact]
    public void Build_EmptyInputProducesAnEmptyModel()
    {
        var model = AllocationPieChartBuilder.Build([]);

        model.Series.Should().BeEmpty();
    }

    [Fact]
    public void Build_AddsOneSlicePerEntryInTheGivenOrder()
    {
        var model = AllocationPieChartBuilder.Build(
        [
            ("Equity", 600m),
            ("Bond", 400m),
        ]);

        var slices = model.Series.OfType<PieSeries>().Single().Slices;
        slices.Select(slice => slice.Label).Should().Equal("Equity", "Bond");
        slices.Select(slice => slice.Value).Should().Equal(600d, 400d);
    }

    [Fact]
    public void Build_CyclesThePaletteBeyondItsEightColours()
    {
        var slices = Enumerable.Range(0, 9).Select(index => ($"Slice {index}", (decimal)(index + 1))).ToList();

        var model = AllocationPieChartBuilder.Build(slices);

        var built = model.Series.OfType<PieSeries>().Single().Slices;
        built.Should().HaveCount(9);
        built[8].Fill.Should().Be(built[0].Fill);
        built.Take(8).Select(slice => slice.Fill).Should().OnlyHaveUniqueItems();
    }
}
