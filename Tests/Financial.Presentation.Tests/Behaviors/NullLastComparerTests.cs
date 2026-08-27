using System.ComponentModel;
using Financial.Presentation.App.Behaviors;
using FluentAssertions;

namespace Financial.Presentation.Tests.Behaviors;

public class NullLastComparerTests
{
    [Fact]
    public void Compare_NullVsNull_ReturnsZero()
    {
        NullLastComparer.Compare(null, null, ListSortDirection.Ascending).Should().Be(0);
    }

    [Fact]
    public void Compare_NullVsValue_Ascending_NullSortsLast()
    {
        NullLastComparer.Compare(null, 5m, ListSortDirection.Ascending).Should().BePositive();
        NullLastComparer.Compare(5m, null, ListSortDirection.Ascending).Should().BeNegative();
    }

    [Fact]
    public void Compare_NullVsValue_Descending_NullStillSortsLast()
    {
        NullLastComparer.Compare(null, 5m, ListSortDirection.Descending).Should().BePositive();
        NullLastComparer.Compare(5m, null, ListSortDirection.Descending).Should().BeNegative();
    }

    [Fact]
    public void Compare_Decimals_Ascending_OrdersNumerically()
    {
        NullLastComparer.Compare(9.99m, 42.5m, ListSortDirection.Ascending).Should().BeNegative();
        NullLastComparer.Compare(42.5m, 9.99m, ListSortDirection.Ascending).Should().BePositive();
    }

    [Fact]
    public void Compare_Decimals_Descending_ReversesOrder()
    {
        NullLastComparer.Compare(9.99m, 42.5m, ListSortDirection.Descending).Should().BePositive();
        NullLastComparer.Compare(42.5m, 9.99m, ListSortDirection.Descending).Should().BeNegative();
    }

    [Fact]
    public void Compare_DateTimes_Ascending_OrdersChronologically()
    {
        var earlier = new DateTime(2026, 1, 10);
        var later = new DateTime(2026, 3, 1);

        NullLastComparer.Compare(earlier, later, ListSortDirection.Ascending).Should().BeNegative();
    }

    [Fact]
    public void Compare_DateTimes_Descending_OrdersChronologicallyReversed()
    {
        var earlier = new DateTime(2026, 1, 10);
        var later = new DateTime(2026, 3, 1);

        NullLastComparer.Compare(earlier, later, ListSortDirection.Descending).Should().BePositive();
    }

    [Fact]
    public void Compare_Strings_Ascending_OrdersAlphabetically()
    {
        NullLastComparer.Compare("apple", "Banana", ListSortDirection.Ascending).Should().BeNegative();
    }

    [Fact]
    public void Compare_EqualValues_ReturnsZero()
    {
        NullLastComparer.Compare(10m, 10m, ListSortDirection.Ascending).Should().Be(0);
    }
}
