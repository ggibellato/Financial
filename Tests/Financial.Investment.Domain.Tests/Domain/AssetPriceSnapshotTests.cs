using Financial.Investment.Domain.Entities;
using FluentAssertions;

namespace Financial.Investment.Domain.Tests;

public class AssetPriceSnapshotTests
{
    [Fact]
    public void Create_WithPositivePrice_AssignsProperties()
    {
        var date = new DateOnly(2026, 8, 15);
        var retrievedAt = new DateTimeOffset(2026, 8, 15, 10, 0, 0, TimeSpan.Zero);

        var entry = AssetPriceSnapshot.Create(date, 1234.56m, ValuationMethod.MarketPrice, PriceSource.Manual, "GBP", "ref-1", retrievedAt);

        using (new FluentAssertions.Execution.AssertionScope())
        {
            entry.Date.Should().Be(date);
            entry.Price.Should().Be(1234.56m);
            entry.IsManual.Should().BeTrue();
            entry.Currency.Should().Be("GBP");
            entry.Source.Should().Be(PriceSource.Manual);
            entry.SourceReference.Should().Be("ref-1");
            entry.ValuationMethod.Should().Be(ValuationMethod.MarketPrice);
            entry.RetrievedAt.Should().Be(retrievedAt);
        }
    }

    [Fact]
    public void Create_WithZeroPrice_Throws()
    {
        Action act = () => Create(DateOnly.FromDateTime(DateTime.Today), 0m, ValuationMethod.MarketPrice);

        act.Should().Throw<ArgumentException>().WithMessage("Price must be greater than zero.");
    }

    [Fact]
    public void Create_WithNegativePrice_Throws()
    {
        Action act = () => Create(DateOnly.FromDateTime(DateTime.Today), -1m, ValuationMethod.MarketPrice);

        act.Should().Throw<ArgumentException>().WithMessage("Price must be greater than zero.");
    }

    [Theory]
    [InlineData(ValuationMethod.ProviderValue)]
    [InlineData(ValuationMethod.Manual)]
    public void Create_WithZeroPrice_ForValueBasedMethod_Succeeds(ValuationMethod valuationMethod)
    {
        var entry = Create(DateOnly.FromDateTime(DateTime.Today), 0m, valuationMethod);

        entry.Price.Should().Be(0m);
    }

    [Theory]
    [InlineData(ValuationMethod.ProviderValue)]
    [InlineData(ValuationMethod.Manual)]
    public void Create_WithNegativePrice_ForValueBasedMethod_Throws(ValuationMethod valuationMethod)
    {
        Action act = () => Create(DateOnly.FromDateTime(DateTime.Today), -1m, valuationMethod);

        act.Should().Throw<ArgumentException>().WithMessage("Price must not be negative.");
    }

    [Fact]
    public void Create_WithFutureDate_Throws()
    {
        var futureDate = DateOnly.FromDateTime(DateTime.Today).AddDays(1);

        Action act = () => Create(futureDate, 10m, ValuationMethod.MarketPrice);

        act.Should().Throw<ArgumentException>().WithMessage("Price date cannot be in the future.");
    }

    [Fact]
    public void Create_WithTodayDate_Succeeds()
    {
        var today = DateOnly.FromDateTime(DateTime.Today);

        var entry = Create(today, 10m, ValuationMethod.MarketPrice);

        entry.Date.Should().Be(today);
    }

    [Fact]
    public void IsManual_DerivesFromSource()
    {
        var today = DateOnly.FromDateTime(DateTime.Today);

        Create(today, 10m, ValuationMethod.MarketPrice, PriceSource.Manual).IsManual.Should().BeTrue();
        Create(today, 10m, ValuationMethod.MarketPrice, PriceSource.Google).IsManual.Should().BeFalse();
    }

    private static AssetPriceSnapshot Create(DateOnly date, decimal price, ValuationMethod valuationMethod, PriceSource source = PriceSource.Unknown) =>
        AssetPriceSnapshot.Create(date, price, valuationMethod, source, currency: string.Empty, sourceReference: null, DateTimeOffset.UtcNow);
}
