using System;
using FluentAssertions;
using Financial.Domain.Entities;

namespace Financial.Domain.Tests;

public class AssetTests
{
    [Fact]
    public void Create_SetsProperties()
    {
        var asset = Asset.Create("Asset A", "ISIN123", "NYSE", "AAA");

        asset.Name.Should().Be("Asset A");
        asset.ISIN.Should().Be("ISIN123");
        asset.Exchange.Should().Be("NYSE");
        asset.Ticker.Should().Be("AAA");
    }

    [Fact]
    public void AddOperation_Buy_UpdatesAveragePriceAndQuantity()
    {
        var asset = Asset.Create("Asset A", "ISIN123", "NYSE", "AAA");
        var first = Operation.Create(new DateTime(2024, 1, 1), Operation.OperationType.Buy, 10m, 5m, 0m);
        var second = Operation.Create(new DateTime(2024, 1, 2), Operation.OperationType.Buy, 10m, 7m, 0m);

        asset.AddOperation(first);
        asset.AddOperation(second);

        asset.Quantity.Should().Be(20m);
        asset.AvargePrice.Should().Be(6m);
        asset.Active.Should().BeTrue();
    }

    [Fact]
    public void AddOperation_Sell_DecreasesQuantityAndKeepsAveragePrice()
    {
        var asset = Asset.Create("Asset A", "ISIN123", "NYSE", "AAA");
        asset.AddOperation(Operation.Create(new DateTime(2024, 1, 1), Operation.OperationType.Buy, 5m, 10m, 0m));

        asset.AddOperation(Operation.Create(new DateTime(2024, 1, 2), Operation.OperationType.Sell, 5m, 12m, 0m));

        asset.Quantity.Should().Be(0m);
        asset.AvargePrice.Should().Be(10m);
        asset.Active.Should().BeFalse();
    }

    [Fact]
    public void UpdateOperation_RebuildsOperationsAndRecalculates()
    {
        var asset = Asset.Create("Asset A", "ISIN123", "NYSE", "AAA");
        var op1Id = Guid.NewGuid();
        var op1 = Operation.CreateWithId(op1Id, new DateTime(2024, 1, 1), Operation.OperationType.Buy, 10m, 5m, 0m);
        var op2 = Operation.CreateWithId(Guid.NewGuid(), new DateTime(2024, 1, 2), Operation.OperationType.Buy, 10m, 7m, 0m);
        asset.AddOperation(op1);
        asset.AddOperation(op2);

        var updated = Operation.CreateWithId(op1Id, op1.Date, op1.Type, 20m, 5m, 0m);
        var result = asset.UpdateOperation(updated);

        result.Should().BeTrue();
        asset.Quantity.Should().Be(30m);
        var expected = (20m * 5m + 10m * 7m) / 30m;
        asset.AvargePrice.Should().Be(expected);
    }

    [Fact]
    public void UpdateOperation_UnknownId_ReturnsFalse()
    {
        var asset = Asset.Create("Asset A", "ISIN123", "NYSE", "AAA");

        var result = asset.UpdateOperation(Operation.CreateWithId(Guid.NewGuid(), new DateTime(2024, 1, 1), Operation.OperationType.Buy, 1m, 1m, 0m));

        result.Should().BeFalse();
    }

    [Fact]
    public void RemoveOperation_UnknownId_ReturnsFalse()
    {
        var asset = Asset.Create("Asset A", "ISIN123", "NYSE", "AAA");

        asset.RemoveOperation(Guid.NewGuid()).Should().BeFalse();
    }

    [Fact]
    public void RemoveOperation_EmptyId_Throws()
    {
        var asset = Asset.Create("Asset A", "ISIN123", "NYSE", "AAA");

        Action act = () => asset.RemoveOperation(Guid.Empty);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void AddCredit_AssignsIdWhenEmpty()
    {
        var asset = Asset.Create("Asset A", "ISIN123", "NYSE", "AAA");
        var credit = Credit.CreateWithId(Guid.Empty, new DateTime(2024, 2, 1), Credit.CreditType.Dividend, 10m);

        asset.AddCredit(credit);

        credit.Id.Should().NotBe(Guid.Empty);
        asset.Credits.Should().ContainSingle();
    }

    [Fact]
    public void UpdateCredit_UnknownId_ReturnsFalse()
    {
        var asset = Asset.Create("Asset A", "ISIN123", "NYSE", "AAA");

        var result = asset.UpdateCredit(Credit.CreateWithId(Guid.NewGuid(), new DateTime(2024, 2, 1), Credit.CreditType.Dividend, 10m));

        result.Should().BeFalse();
    }

    [Fact]
    public void RemoveCredit_UnknownId_ReturnsFalse()
    {
        var asset = Asset.Create("Asset A", "ISIN123", "NYSE", "AAA");

        asset.RemoveCredit(Guid.NewGuid()).Should().BeFalse();
    }
}
