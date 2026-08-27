using System.ComponentModel;
using Financial.Presentation.App.Behaviors;
using FluentAssertions;

namespace Financial.Presentation.Tests.Behaviors;

public class SortCycleTests
{
    [Fact]
    public void Next_NoCurrentColumn_ReturnsAscending()
    {
        SortCycle.Next(currentColumnPath: null, currentDirection: null, requestedColumnPath: "Value")
            .Should().Be(ListSortDirection.Ascending);
    }

    [Fact]
    public void Next_DifferentColumn_ReturnsAscending()
    {
        SortCycle.Next(currentColumnPath: "Date", currentDirection: ListSortDirection.Descending, requestedColumnPath: "Value")
            .Should().Be(ListSortDirection.Ascending);
    }

    [Fact]
    public void Next_SameColumnUnsorted_ReturnsAscending()
    {
        SortCycle.Next(currentColumnPath: "Value", currentDirection: null, requestedColumnPath: "Value")
            .Should().Be(ListSortDirection.Ascending);
    }

    [Fact]
    public void Next_SameColumnAscending_ReturnsDescending()
    {
        SortCycle.Next(currentColumnPath: "Value", currentDirection: ListSortDirection.Ascending, requestedColumnPath: "Value")
            .Should().Be(ListSortDirection.Descending);
    }

    [Fact]
    public void Next_SameColumnDescending_ReturnsNull()
    {
        SortCycle.Next(currentColumnPath: "Value", currentDirection: ListSortDirection.Descending, requestedColumnPath: "Value")
            .Should().BeNull();
    }
}
