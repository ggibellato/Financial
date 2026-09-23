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
        IReadOnlyList<SpecificLotAllocation>? allocation = null,
        IEnumerable<CorporateAction>? precedingCorporateActions = null)
    {
        var preceding = precedingTransactions as IReadOnlyList<Transaction> ?? precedingTransactions.ToList();
        var precedingActions = precedingCorporateActions?.ToList() ?? new List<CorporateAction>();

        var lotsConsumed = method switch
        {
            CostBasisMethod.AverageCost => BuildAverageCostLot(disposingTransaction, preceding, precedingActions),
            CostBasisMethod.FIFO => BuildAutoConsumedLots(disposingTransaction, preceding, precedingActions),
            CostBasisMethod.SpecificId => BuildSpecificIdLots(disposingTransaction, preceding, allocation, precedingActions),
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
        Transaction disposingTransaction, IReadOnlyList<Transaction> preceding, IReadOnlyList<CorporateAction> precedingCorporateActions)
    {
        var quantity = 0m;
        var averagePrice = 0m;

        foreach (var step in CorporateActionReplay.Merge(preceding, precedingCorporateActions))
        {
            switch (step)
            {
                case CorporateActionReplayStep(var corporateAction):
                    (quantity, averagePrice) = CorporateActionReplay.ApplyToPosition(quantity, averagePrice, corporateAction);
                    break;

                case TransactionReplayStep(var transaction):
                    if (TransactionTypeEffects.For(transaction.Type).Quantity == QuantityEffect.Increase)
                    {
                        averagePrice = AverageCostReplay.Apply(quantity, averagePrice, transaction);
                    }

                    quantity = CorporateActionReplay.ApplyTransactionToQuantity(quantity, transaction);
                    break;
            }
        }

        return new[] { new DisposalLotConsumption(null, disposingTransaction.Quantity, averagePrice) };
    }

    private static IReadOnlyList<DisposalLotConsumption> BuildAutoConsumedLots(
        Transaction disposingTransaction, IReadOnlyList<Transaction> preceding, IReadOnlyList<CorporateAction> precedingCorporateActions)
    {
        var openLots = OpenLotTracker.GetOpenLots(preceding, precedingCorporateActions);
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
        IReadOnlyList<SpecificLotAllocation>? allocation,
        IReadOnlyList<CorporateAction> precedingCorporateActions)
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

        var openLots = OpenLotTracker.GetOpenLots(preceding, precedingCorporateActions).ToDictionary(lot => lot.SourceTransactionId);
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
