using System;
using System.Collections.Generic;
using System.Linq;
using Financial.Investment.Domain.Entities;
using Financial.Investment.Domain.Exceptions;

namespace Financial.Investment.Domain.Rules;

public static class DisposalRecordCalculator
{
    public static DisposalRecord Calculate(
        Transaction disposingTransaction,
        IEnumerable<Transaction> precedingTransactions,
        CostBasisMethod method,
        string brokerCurrency,
        IReadOnlyList<SpecificLotAllocation>? allocation = null)
    {
        var preceding = precedingTransactions as IReadOnlyList<Transaction> ?? precedingTransactions.ToList();

        var lotsConsumed = method switch
        {
            CostBasisMethod.AverageCost => BuildAverageCostLot(disposingTransaction, preceding),
            CostBasisMethod.FIFO => BuildAutoConsumedLots(disposingTransaction, preceding),
            CostBasisMethod.SpecificId => BuildSpecificIdLots(disposingTransaction, preceding, allocation),
            _ => throw new ArgumentOutOfRangeException(nameof(method), method, "Unknown cost basis method.")
        };

        var taxYear = TaxYearCalculator.Calculate(disposingTransaction.Date, brokerCurrency);

        return DisposalRecord.Create(
            disposingTransaction.Id,
            disposingTransaction.Date,
            method,
            lotsConsumed,
            disposingTransaction.Quantity,
            disposingTransaction.NetCash,
            disposingTransaction.Currency,
            taxYear);
    }

    private static IReadOnlyList<DisposalLotConsumption> BuildAverageCostLot(
        Transaction disposingTransaction, IReadOnlyList<Transaction> preceding)
    {
        var quantity = 0m;
        var averagePrice = 0m;

        foreach (var transaction in TransactionReplayOrder.Sort(preceding))
        {
            var effect = TransactionTypeEffects.For(transaction.Type);
            switch (effect.Quantity)
            {
                case QuantityEffect.Increase:
                    averagePrice = AverageCostReplay.Apply(quantity, averagePrice, transaction);
                    quantity += transaction.Quantity;
                    break;
                case QuantityEffect.Decrease:
                    quantity -= transaction.Quantity;
                    break;
            }
        }

        return new[] { new DisposalLotConsumption(null, disposingTransaction.Quantity, averagePrice) };
    }

    private static IReadOnlyList<DisposalLotConsumption> BuildAutoConsumedLots(
        Transaction disposingTransaction, IReadOnlyList<Transaction> preceding)
    {
        var openLots = OpenLotTracker.GetOpenLots(preceding);
        var remaining = disposingTransaction.Quantity;
        var consumed = new List<DisposalLotConsumption>();

        foreach (var lot in openLots)
        {
            if (remaining <= 0)
            {
                break;
            }

            var take = Math.Min(lot.RemainingQuantity, remaining);
            consumed.Add(new DisposalLotConsumption(lot.SourceTransactionId, take, lot.UnitCost));
            remaining -= take;
        }

        return consumed;
    }

    private static IReadOnlyList<DisposalLotConsumption> BuildSpecificIdLots(
        Transaction disposingTransaction,
        IReadOnlyList<Transaction> preceding,
        IReadOnlyList<SpecificLotAllocation>? allocation)
    {
        if (allocation is null || allocation.Count == 0)
        {
            throw new InvestmentRuleViolationException("A SpecificId sale requires at least one lot allocation.");
        }

        var allocatedQuantity = allocation.Sum(entry => entry.Quantity);
        if (allocatedQuantity != disposingTransaction.Quantity)
        {
            throw new InvestmentRuleViolationException(
                $"Allocated lot quantity ({allocatedQuantity}) does not match the sale quantity ({disposingTransaction.Quantity}).");
        }

        var openLots = OpenLotTracker.GetOpenLots(preceding).ToDictionary(lot => lot.SourceTransactionId);
        var consumed = new List<DisposalLotConsumption>();

        foreach (var entry in allocation)
        {
            if (!openLots.TryGetValue(entry.SourceTransactionId, out var lot))
            {
                throw new InvestmentRuleViolationException(
                    $"Lot {entry.SourceTransactionId} is not an open lot for this holding.");
            }

            if (entry.Quantity > lot.RemainingQuantity)
            {
                throw new InvestmentRuleViolationException(
                    $"Lot {entry.SourceTransactionId} has only {lot.RemainingQuantity} units open, but {entry.Quantity} were requested.");
            }

            consumed.Add(new DisposalLotConsumption(lot.SourceTransactionId, entry.Quantity, lot.UnitCost));
        }

        return consumed;
    }
}
