using System;
using System.Collections.Generic;
using Financial.Investment.Domain.Entities;
using Financial.Investment.Domain.Rules;
using FluentAssertions;

namespace Financial.Investment.Domain.Tests;

public class DividendAttributionCalculatorTests
{
    [Fact]
    public void Calculate_NoSharesForDividend_ReturnsNull()
    {
        var credit = Credit.Create(new DateTime(2024, 6, 1), Credit.CreditType.Dividend, 400m);
        var transactions = new[] { Buy(new DateTime(2024, 1, 1), 1000m, 9m) };

        var result = DividendAttributionCalculator.Calculate(credit, transactions, priceOnDate: null);

        result.Should().BeNull();
    }

    [Fact]
    public void Calculate_NoOpenLotsAsOfDate_ReturnsNull()
    {
        var credit = Credit.Create(new DateTime(2024, 6, 1), Credit.CreditType.Dividend, 400m, sharesForDividend: 800m);
        var transactions = new[] { Buy(new DateTime(2024, 7, 1), 1000m, 9m) };

        var result = DividendAttributionCalculator.Calculate(credit, transactions, priceOnDate: null);

        result.Should().BeNull();
    }

    [Fact]
    public void Calculate_UsesSharesForDividendNotFullPosition()
    {
        var credit = Credit.Create(new DateTime(2024, 6, 1), Credit.CreditType.Dividend, 400m, sharesForDividend: 800m);
        var transactions = new[]
        {
            Buy(new DateTime(2024, 1, 1), 1000m, 9m),
            Buy(new DateTime(2024, 8, 1), 200m, 20m) // bought after the dividend date - excluded
        };

        var result = DividendAttributionCalculator.Calculate(credit, transactions, priceOnDate: null);

        result.Should().NotBeNull();
        result!.SharesForDividend.Should().Be(800m);
        result.AverageCostPerShare.Should().Be(9m);
        result.InvestedAmount.Should().Be(7200m);
        result.YieldOnInvested.Should().BeApproximately(5.5556m, 0.0001m);
    }

    [Fact]
    public void Calculate_WithMarketPrice_ComputesMarketValueAndYield()
    {
        var credit = Credit.Create(new DateTime(2024, 6, 1), Credit.CreditType.Dividend, 400m, sharesForDividend: 800m);
        var transactions = new[] { Buy(new DateTime(2024, 1, 1), 1000m, 9m) };
        var price = AssetPriceSnapshot.Create(new DateOnly(2024, 6, 1), 10m, ValuationMethod.MarketPrice, PriceSource.Unknown, string.Empty, null, DateTimeOffset.UtcNow);

        var result = DividendAttributionCalculator.Calculate(credit, transactions, price);

        result.Should().NotBeNull();
        result!.MarketValueOnDate.Should().Be(8000m);
        result.YieldOnMarket.Should().Be(5m);
    }

    [Fact]
    public void Calculate_NoPriceOnDate_MarketValueAndYieldOnMarketAreNull()
    {
        var credit = Credit.Create(new DateTime(2024, 6, 1), Credit.CreditType.Dividend, 400m, sharesForDividend: 800m);
        var transactions = new[] { Buy(new DateTime(2024, 1, 1), 1000m, 9m) };

        var result = DividendAttributionCalculator.Calculate(credit, transactions, priceOnDate: null);

        result.Should().NotBeNull();
        result!.MarketValueOnDate.Should().BeNull();
        result.YieldOnMarket.Should().BeNull();
    }

    [Fact]
    public void Calculate_ProviderValueMethod_MarketValueIsProRatedToSharesForDividend()
    {
        var credit = Credit.Create(new DateTime(2024, 6, 1), Credit.CreditType.Dividend, 400m, sharesForDividend: 800m);
        var transactions = new[] { Buy(new DateTime(2024, 1, 1), 1000m, 9m) };
        var price = AssetPriceSnapshot.Create(new DateOnly(2024, 6, 1), 5000m, ValuationMethod.ProviderValue, PriceSource.Unknown, string.Empty, null, DateTimeOffset.UtcNow);

        var result = DividendAttributionCalculator.Calculate(credit, transactions, price);

        result!.MarketValueOnDate.Should().Be(4000m, "5000 is the whole 1000-unit position's worth, so 800 attributed shares are 80% of it");
    }

    [Fact]
    public void Calculate_ProviderValueMethod_FullPositionAttributed_MarketValueEqualsRecordedFigure()
    {
        var credit = Credit.Create(new DateTime(2024, 6, 1), Credit.CreditType.Dividend, 400m, sharesForDividend: 1000m);
        var transactions = new[] { Buy(new DateTime(2024, 1, 1), 1000m, 9m) };
        var price = AssetPriceSnapshot.Create(new DateOnly(2024, 6, 1), 5000m, ValuationMethod.ProviderValue, PriceSource.Unknown, string.Empty, null, DateTimeOffset.UtcNow);

        var result = DividendAttributionCalculator.Calculate(credit, transactions, price);

        result!.MarketValueOnDate.Should().Be(5000m);
    }

    private static Transaction Buy(DateTime date, decimal quantity, decimal unitPrice) =>
        Transaction.Create(date, Transaction.TransactionType.Buy, quantity, unitPrice, 0m);
}
