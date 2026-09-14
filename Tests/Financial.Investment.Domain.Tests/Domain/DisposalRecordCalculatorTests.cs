using System;
using System.Collections.Generic;
using Financial.Investment.Domain.Entities;
using Financial.Investment.Domain.Exceptions;
using Financial.Investment.Domain.Rules;
using Financial.Shared.Abstractions.Currencies;
using FluentAssertions;

namespace Financial.Investment.Domain.Tests;

public class DisposalRecordCalculatorTests
{
    [Fact]
    public void Calculate_Sell_UnderAverageCost_ProducesOneActiveRecordWithSyntheticLot()
    {
        var buy = Transaction.Create(new DateTime(2026, 1, 1), Transaction.TransactionType.Buy, 10m, 5m, 0m);
        var sell = Transaction.Create(new DateTime(2026, 2, 1), Transaction.TransactionType.Sell, 4m, 8m, 0m);

        var record = DisposalRecordCalculator.Calculate(sell, new[] { buy }, CostBasisMethod.AverageCost, "GBP");

        record.Status.Should().Be(DisposalRecordStatus.Active);
        record.LotsConsumed.Should().ContainSingle();
        record.LotsConsumed[0].SourceTransactionId.Should().BeNull();
        record.LotsConsumed[0].UnitCost.Should().Be(5m);
        record.LotsConsumed[0].Quantity.Should().Be(4m);
    }

    [Fact]
    public void Calculate_Proceeds_EqualsNetCash_AndGainLossEqualsProceedsMinusCostBasis()
    {
        var buy = Transaction.Create(new DateTime(2026, 1, 1), Transaction.TransactionType.Buy, 10m, 5m, 0m);
        var sell = Transaction.Create(new DateTime(2026, 2, 1), Transaction.TransactionType.Sell, 4m, 8m, 1m);

        var record = DisposalRecordCalculator.Calculate(sell, new[] { buy }, CostBasisMethod.AverageCost, "GBP");

        record.Proceeds.Should().Be(sell.NetCash);
        record.GainLoss.Should().Be(record.Proceeds - record.CostBasis);
    }

    [Fact]
    public void Calculate_UnderFifo_ConsumesOldestOpenLotsFirst()
    {
        var firstBuy = Transaction.Create(new DateTime(2026, 1, 1), Transaction.TransactionType.Buy, 5m, 5m, 0m);
        var secondBuy = Transaction.Create(new DateTime(2026, 1, 15), Transaction.TransactionType.Buy, 5m, 6m, 0m);
        var sell = Transaction.Create(new DateTime(2026, 2, 1), Transaction.TransactionType.Sell, 8m, 7m, 0m);

        var record = DisposalRecordCalculator.Calculate(sell, new[] { firstBuy, secondBuy }, CostBasisMethod.FIFO, "GBP");

        record.LotsConsumed.Should().HaveCount(2);
        record.LotsConsumed[0].SourceTransactionId.Should().Be(firstBuy.Id);
        record.LotsConsumed[0].Quantity.Should().Be(5m);
        record.LotsConsumed[1].SourceTransactionId.Should().Be(secondBuy.Id);
        record.LotsConsumed[1].Quantity.Should().Be(3m);
    }

    [Fact]
    public void Calculate_UnderSpecificId_ConsumesExactlyTheChosenLots()
    {
        var firstBuy = Transaction.Create(new DateTime(2026, 1, 1), Transaction.TransactionType.Buy, 5m, 5m, 0m);
        var secondBuy = Transaction.Create(new DateTime(2026, 1, 15), Transaction.TransactionType.Buy, 5m, 6m, 0m);
        var sell = Transaction.Create(new DateTime(2026, 2, 1), Transaction.TransactionType.Sell, 4m, 7m, 0m);
        var allocation = new[] { new SpecificLotAllocation(secondBuy.Id, 4m) };

        var record = DisposalRecordCalculator.Calculate(
            sell, new[] { firstBuy, secondBuy }, CostBasisMethod.SpecificId, "GBP", allocation);

        record.LotsConsumed.Should().ContainSingle();
        record.LotsConsumed[0].SourceTransactionId.Should().Be(secondBuy.Id);
        record.LotsConsumed[0].UnitCost.Should().Be(6m);
    }

    [Fact]
    public void Calculate_SpecificId_AllocationDoesNotSumToSaleQuantity_Throws()
    {
        var buy = Transaction.Create(new DateTime(2026, 1, 1), Transaction.TransactionType.Buy, 10m, 5m, 0m);
        var sell = Transaction.Create(new DateTime(2026, 2, 1), Transaction.TransactionType.Sell, 4m, 7m, 0m);
        var allocation = new[] { new SpecificLotAllocation(buy.Id, 3m) };

        Action act = () => DisposalRecordCalculator.Calculate(sell, new[] { buy }, CostBasisMethod.SpecificId, "GBP", allocation);

        act.Should().Throw<InvestmentRuleViolationException>();
    }

    [Fact]
    public void Calculate_SpecificId_AllocationExceedsLotRemainingQuantity_Throws()
    {
        var buy = Transaction.Create(new DateTime(2026, 1, 1), Transaction.TransactionType.Buy, 3m, 5m, 0m);
        var sell = Transaction.Create(new DateTime(2026, 2, 1), Transaction.TransactionType.Sell, 4m, 7m, 0m);
        var allocation = new[] { new SpecificLotAllocation(buy.Id, 4m) };

        Action act = () => DisposalRecordCalculator.Calculate(sell, new[] { buy }, CostBasisMethod.SpecificId, "GBP", allocation);

        act.Should().Throw<InvestmentRuleViolationException>();
    }

    [Fact]
    public void Calculate_SpecificId_ReferencesALotThatIsNotOpen_Throws()
    {
        var buy = Transaction.Create(new DateTime(2026, 1, 1), Transaction.TransactionType.Buy, 4m, 5m, 0m);
        var sell = Transaction.Create(new DateTime(2026, 2, 1), Transaction.TransactionType.Sell, 4m, 7m, 0m);
        var allocation = new[] { new SpecificLotAllocation(Guid.NewGuid(), 4m) };

        Action act = () => DisposalRecordCalculator.Calculate(sell, new[] { buy }, CostBasisMethod.SpecificId, "GBP", allocation);

        act.Should().Throw<InvestmentRuleViolationException>();
    }

    [Fact]
    public void Calculate_SpecificId_NoAllocationProvided_Throws()
    {
        var buy = Transaction.Create(new DateTime(2026, 1, 1), Transaction.TransactionType.Buy, 4m, 5m, 0m);
        var sell = Transaction.Create(new DateTime(2026, 2, 1), Transaction.TransactionType.Sell, 4m, 7m, 0m);

        Action act = () => DisposalRecordCalculator.Calculate(sell, new[] { buy }, CostBasisMethod.SpecificId, "GBP", null);

        act.Should().Throw<InvestmentRuleViolationException>();
    }

    [Fact]
    public void Calculate_TaxYear_BrlCurrency_UsesCalendarYear()
    {
        var buy = Transaction.Create(new DateTime(2026, 1, 1), Transaction.TransactionType.Buy, 4m, 5m, 0m);
        var sell = Transaction.Create(new DateTime(2026, 6, 1), Transaction.TransactionType.Sell, 4m, 7m, 0m);

        var record = DisposalRecordCalculator.Calculate(sell, new[] { buy }, CostBasisMethod.AverageCost, "BRL");

        record.TaxYear.Should().Be("2026");
    }

    [Fact]
    public void Calculate_TaxYear_OtherCurrency_UsesUkTaxYear()
    {
        var buy = Transaction.Create(new DateTime(2026, 1, 1), Transaction.TransactionType.Buy, 4m, 5m, 0m);
        var sell = Transaction.Create(new DateTime(2026, 1, 10), Transaction.TransactionType.Sell, 4m, 7m, 0m);

        var record = DisposalRecordCalculator.Calculate(sell, new[] { buy }, CostBasisMethod.AverageCost, "GBP");

        record.TaxYear.Should().Be("2025/26");
    }

    [Fact]
    public void Calculate_Redemption_IsTreatedAsADisposalLikeASell()
    {
        var buy = Transaction.Create(new DateTime(2026, 1, 1), Transaction.TransactionType.Buy, 10m, 5m, 0m);
        var redemption = Transaction.Create(new DateTime(2026, 2, 1), Transaction.TransactionType.Redemption, 10m, 6m, 0m);

        var record = DisposalRecordCalculator.Calculate(redemption, new[] { buy }, CostBasisMethod.AverageCost, "GBP");

        record.QuantityDisposed.Should().Be(10m);
        record.Proceeds.Should().Be(redemption.NetCash);
    }
}
