using Financial.Investment.Application.Services;
using Financial.Investment.Domain.Entities;
using Financial.Shared.Abstractions.Currencies;
using FluentAssertions;

namespace Financial.Investment.Application.Tests.Services;

public class NavigationMapperTests
{
    [Fact]
    public void MapTransaction_WithFxRateSnapshot_MapsCurrencyAndSnapshot()
    {
        var retrievedAt = new DateTimeOffset(2026, 7, 1, 8, 0, 0, TimeSpan.Zero);
        var snapshot = FxRateSnapshot.Create(Currency.GBP, 0.146m, FxRateSource.Frankfurter, retrievedAt);
        var transaction = Transaction.Create(
            new DateTime(2026, 7, 1), Transaction.TransactionType.Buy, 10m, 9.99m, 0m,
            currency: Currency.BRL, fxRateSnapshot: snapshot);

        var dto = NavigationMapper.MapTransaction(transaction);

        dto.Currency.Should().Be("BRL");
        dto.FxRateSnapshot.Should().NotBeNull();
        dto.FxRateSnapshot!.ToCurrency.Should().Be("GBP");
        dto.FxRateSnapshot.Rate.Should().Be(0.146m);
        dto.FxRateSnapshot.Source.Should().Be("Frankfurter");
        dto.FxRateSnapshot.RetrievedAt.Should().Be(retrievedAt);
    }

    [Fact]
    public void MapTransaction_WithoutFxRateSnapshot_MapsNullSnapshot()
    {
        var transaction = Transaction.Create(new DateTime(2026, 7, 1), Transaction.TransactionType.Buy, 10m, 9.99m, 0m, currency: Currency.GBP);

        var dto = NavigationMapper.MapTransaction(transaction);

        dto.Currency.Should().Be("GBP");
        dto.FxRateSnapshot.Should().BeNull();
    }

    [Fact]
    public void MapCredit_WithFxRateSnapshot_MapsCurrencyAndSnapshot()
    {
        var retrievedAt = new DateTimeOffset(2026, 7, 1, 8, 0, 0, TimeSpan.Zero);
        var snapshot = FxRateSnapshot.Create(Currency.GBP, 0.146m, FxRateSource.Frankfurter, retrievedAt);
        var credit = Credit.Create(
            new DateTime(2026, 7, 1), Credit.CreditType.Dividend, 100m,
            currency: Currency.BRL, fxRateSnapshot: snapshot);

        var dto = NavigationMapper.MapCredit(credit);

        dto.Currency.Should().Be("BRL");
        dto.FxRateSnapshot.Should().NotBeNull();
        dto.FxRateSnapshot!.ToCurrency.Should().Be("GBP");
        dto.FxRateSnapshot.Rate.Should().Be(0.146m);
        dto.FxRateSnapshot.Source.Should().Be("Frankfurter");
        dto.FxRateSnapshot.RetrievedAt.Should().Be(retrievedAt);
    }

    [Fact]
    public void MapCredit_WithoutFxRateSnapshot_MapsNullSnapshot()
    {
        var credit = Credit.Create(new DateTime(2026, 7, 1), Credit.CreditType.Dividend, 100m, currency: Currency.GBP);

        var dto = NavigationMapper.MapCredit(credit);

        dto.Currency.Should().Be("GBP");
        dto.FxRateSnapshot.Should().BeNull();
    }
}
