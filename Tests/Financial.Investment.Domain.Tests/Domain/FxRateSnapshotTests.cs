using Financial.Investment.Domain.Entities;
using Financial.Shared.Abstractions.Currencies;
using FluentAssertions;

namespace Financial.Investment.Domain.Tests;

public class FxRateSnapshotTests
{
    [Fact]
    public void Create_WithPositiveRate_AssignsProperties()
    {
        var retrievedAt = new DateTimeOffset(2026, 8, 15, 10, 0, 0, TimeSpan.Zero);

        var snapshot = FxRateSnapshot.Create(Currency.GBP, 0.146m, FxRateSource.Frankfurter, retrievedAt);

        using (new FluentAssertions.Execution.AssertionScope())
        {
            snapshot.ToCurrency.Should().Be(Currency.GBP);
            snapshot.Rate.Should().Be(0.146m);
            snapshot.Source.Should().Be(FxRateSource.Frankfurter);
            snapshot.RetrievedAt.Should().Be(retrievedAt);
        }
    }

    [Fact]
    public void Create_WithZeroRate_Throws()
    {
        Action act = () => FxRateSnapshot.Create(Currency.GBP, 0m, FxRateSource.Frankfurter, DateTimeOffset.UtcNow);

        act.Should().Throw<ArgumentException>().WithMessage("Rate must be greater than zero.*");
    }

    [Fact]
    public void Create_WithNegativeRate_Throws()
    {
        Action act = () => FxRateSnapshot.Create(Currency.GBP, -0.1m, FxRateSource.Frankfurter, DateTimeOffset.UtcNow);

        act.Should().Throw<ArgumentException>().WithMessage("Rate must be greater than zero.*");
    }
}
