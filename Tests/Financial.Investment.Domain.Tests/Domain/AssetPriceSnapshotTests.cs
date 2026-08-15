using Financial.Investment.Domain.Entities;
using FluentAssertions;

namespace Financial.Investment.Domain.Tests;

public class AssetPriceSnapshotTests
{
    [Fact]
    public void Create_WithPositivePrice_AssignsProperties()
    {
        var date = new DateOnly(2026, 8, 15);

        var entry = AssetPriceSnapshot.Create(date, 1234.56m, isManual: true);

        using (new FluentAssertions.Execution.AssertionScope())
        {
            entry.Date.Should().Be(date);
            entry.Price.Should().Be(1234.56m);
            entry.IsManual.Should().BeTrue();
        }
    }

    [Fact]
    public void Create_WithZeroPrice_Throws()
    {
        Action act = () => AssetPriceSnapshot.Create(DateOnly.FromDateTime(DateTime.Today), 0m, isManual: true);

        act.Should().Throw<ArgumentException>().WithMessage("Price must be greater than zero.");
    }

    [Fact]
    public void Create_WithNegativePrice_Throws()
    {
        Action act = () => AssetPriceSnapshot.Create(DateOnly.FromDateTime(DateTime.Today), -1m, isManual: true);

        act.Should().Throw<ArgumentException>().WithMessage("Price must be greater than zero.");
    }

    [Fact]
    public void Create_WithFutureDate_Throws()
    {
        var futureDate = DateOnly.FromDateTime(DateTime.Today).AddDays(1);

        Action act = () => AssetPriceSnapshot.Create(futureDate, 10m, isManual: true);

        act.Should().Throw<ArgumentException>().WithMessage("Price date cannot be in the future.");
    }

    [Fact]
    public void Create_WithTodayDate_Succeeds()
    {
        var today = DateOnly.FromDateTime(DateTime.Today);

        var entry = AssetPriceSnapshot.Create(today, 10m, isManual: false);

        entry.Date.Should().Be(today);
    }
}
