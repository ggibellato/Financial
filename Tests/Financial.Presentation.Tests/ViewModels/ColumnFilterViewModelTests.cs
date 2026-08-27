using Financial.Presentation.App.ViewModels;
using FluentAssertions;

namespace Financial.Presentation.Tests.ViewModels;

public class ColumnFilterViewModelTests
{
    private sealed record Row(string Id, string? Category, string? Card);

    private static ColumnFilterViewModel<Row> BuildCategoryFilter(Action? onChanged = null) =>
        new("Category", row => [row.Category], onChanged ?? (() => { }));

    [Fact]
    public void Refresh_ComputesDistinctSortedValues_ExcludingNull()
    {
        var filter = BuildCategoryFilter();
        var rows = new[]
        {
            new Row("r1", "Mercado", null),
            new Row("r2", "Casa", null),
            new Row("r3", "Mercado", null),
            new Row("r4", null, null),
        };

        filter.Refresh(rows);

        filter.Options.Select(o => o.Value).Should().Equal("Casa", "Mercado");
    }

    [Fact]
    public void Refresh_NewOptions_DefaultToChecked()
    {
        var filter = BuildCategoryFilter();
        filter.Refresh([new Row("r1", "Mercado", null)]);

        filter.Options.Should().OnlyContain(o => o.IsChecked);
        filter.IsFiltered.Should().BeFalse();
    }

    [Fact]
    public void ToggleValueCommand_UncheckingOneValue_MarksFilteredAndExcludesMatchingRows()
    {
        var filter = BuildCategoryFilter();
        var rows = new[] { new Row("r1", "Mercado", null), new Row("r2", "Casa", null) };
        filter.Refresh(rows);

        var mercado = filter.Options.Single(o => o.Value == "Mercado");
        filter.ToggleValueCommand.Execute(mercado);

        filter.IsFiltered.Should().BeTrue();
        filter.Matches(rows[0]).Should().BeFalse();
        filter.Matches(rows[1]).Should().BeTrue();
    }

    [Fact]
    public void ToggleAllCommand_OnFullyChecked_UnchecksEverythingAndExcludesAllRows()
    {
        var filter = BuildCategoryFilter();
        var rows = new[] { new Row("r1", "Mercado", null), new Row("r2", "Casa", null) };
        filter.Refresh(rows);

        filter.ToggleAllCommand.Execute(null);

        filter.Options.Should().OnlyContain(o => !o.IsChecked);
        filter.Matches(rows[0]).Should().BeFalse();
        filter.Matches(rows[1]).Should().BeFalse();
    }

    [Fact]
    public void ToggleAllCommand_OnPartiallyChecked_ChecksEverythingAndRevertsToUnfiltered()
    {
        var filter = BuildCategoryFilter();
        var rows = new[] { new Row("r1", "Mercado", null), new Row("r2", "Casa", null) };
        filter.Refresh(rows);
        filter.ToggleValueCommand.Execute(filter.Options.Single(o => o.Value == "Mercado"));

        filter.ToggleAllCommand.Execute(null);

        filter.IsFiltered.Should().BeFalse();
        filter.Matches(rows[0]).Should().BeTrue();
    }

    [Fact]
    public void Matches_MultiValueAccessor_TrueIfAnyCheckedValuePresent()
    {
        var filter = new ColumnFilterViewModel<Row>("Bank", row => [row.Category, row.Card], () => { });
        var transferLikeRow = new Row("r1", "Barclays", "Trading212");
        filter.Refresh([transferLikeRow, new Row("r2", "Barclays", null)]);

        // Uncheck Barclays, leaving only Trading212 checked.
        filter.ToggleValueCommand.Execute(filter.Options.Single(o => o.Value == "Barclays"));

        filter.Matches(transferLikeRow).Should().BeTrue(); // still matches via Trading212
        filter.Matches(new Row("r2", "Barclays", null)).Should().BeFalse(); // only had Barclays
    }

    [Fact]
    public void Refresh_PreservesCheckedState_ForValuesStillPresent()
    {
        var filter = BuildCategoryFilter();
        filter.Refresh([new Row("r1", "Mercado", null), new Row("r2", "Casa", null)]);
        filter.ToggleValueCommand.Execute(filter.Options.Single(o => o.Value == "Casa"));

        filter.Refresh([new Row("r1", "Mercado", null), new Row("r2", "Casa", null), new Row("r3", "Extras", null)]);

        filter.Options.Single(o => o.Value == "Casa").IsChecked.Should().BeFalse();
        filter.Options.Single(o => o.Value == "Extras").IsChecked.Should().BeTrue();
        filter.Options.Single(o => o.Value == "Mercado").IsChecked.Should().BeTrue();
    }

    [Fact]
    public void TogglingAValue_InvokesOnChangedCallback()
    {
        var changeCount = 0;
        var filter = BuildCategoryFilter(onChanged: () => changeCount++);
        filter.Refresh([new Row("r1", "Mercado", null)]);

        filter.ToggleValueCommand.Execute(filter.Options.Single());

        changeCount.Should().Be(1);
    }
}
