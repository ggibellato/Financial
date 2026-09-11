using System;
using Financial.Investment.Domain.Entities;
using Financial.Investment.Domain.Rules;
using FluentAssertions;

namespace Financial.Investment.Domain.Tests;

public class SaleCoverageRuleTests
{
    [Fact]
    public void FindFirstUncoveredSale_NoBreach_ReturnsNull()
    {
        var transactions = new[]
        {
            Transaction.Create(new DateTime(2024, 1, 1), Transaction.TransactionType.Buy, 10m, 100m, 0m),
            Transaction.Create(new DateTime(2024, 2, 1), Transaction.TransactionType.Sell, 5m, 110m, 0m),
        };

        SaleCoverageRule.FindFirstUncoveredSale(transactions).Should().BeNull();
    }

    [Fact]
    public void FindFirstUncoveredSale_SaleExceedsHeldQuantity_ReturnsViolationNamingHeldQuantityAndShortfall()
    {
        var sale = Transaction.Create(new DateTime(2024, 2, 1), Transaction.TransactionType.Sell, 15m, 110m, 0m);
        var transactions = new[]
        {
            Transaction.Create(new DateTime(2024, 1, 1), Transaction.TransactionType.Buy, 10m, 100m, 0m),
            sale,
        };

        var violation = SaleCoverageRule.FindFirstUncoveredSale(transactions);

        violation.Should().NotBeNull();
        violation!.OffendingSale.Should().BeSameAs(sale);
        violation.QuantityHeld.Should().Be(10m);
        violation.Shortfall.Should().Be(5m);
    }

    [Fact]
    public void FindFirstUncoveredSale_EditLeavesALaterSaleShort_NamesTheLaterSale()
    {
        var laterSale = Transaction.Create(new DateTime(2024, 6, 1), Transaction.TransactionType.Sell, 80m, 12m, 0m);
        var transactions = new[]
        {
            Transaction.Create(new DateTime(2024, 1, 1), Transaction.TransactionType.Buy, 100m, 10m, 0m),
            Transaction.Create(new DateTime(2024, 3, 1), Transaction.TransactionType.Sell, 50m, 11m, 0m),
            laterSale,
        };

        var violation = SaleCoverageRule.FindFirstUncoveredSale(transactions);

        violation.Should().NotBeNull();
        violation!.OffendingSale.Should().BeSameAs(laterSale);
        violation.QuantityHeld.Should().Be(50m);
        violation.Shortfall.Should().Be(30m);
    }

    [Fact]
    public void FindFirstUncoveredSale_SaleOfExactlyTheHeldQuantity_ReturnsNull()
    {
        var transactions = new[]
        {
            Transaction.Create(new DateTime(2024, 1, 1), Transaction.TransactionType.Buy, 10m, 100m, 0m),
            Transaction.Create(new DateTime(2024, 2, 1), Transaction.TransactionType.Sell, 10m, 110m, 0m),
        };

        SaleCoverageRule.FindFirstUncoveredSale(transactions).Should().BeNull();
    }

    [Fact]
    public void FindFirstUncoveredSale_HeldQuantityHasFullPrecision_PreservesIt()
    {
        var sale = Transaction.Create(new DateTime(2024, 2, 1), Transaction.TransactionType.Sell, 2128.38m, 12m, 0m);
        var transactions = new[]
        {
            Transaction.Create(new DateTime(2024, 1, 1), Transaction.TransactionType.Buy, 2128.37599271m, 10m, 0m),
            sale,
        };

        var violation = SaleCoverageRule.FindFirstUncoveredSale(transactions);

        violation!.QuantityHeld.Should().Be(2128.37599271m);
    }
}
