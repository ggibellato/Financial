using System;
using System.Collections.Generic;
using System.Linq;
using Financial.Investment.Domain.Entities;
using Financial.Investment.Domain.Exceptions;

namespace Financial.Investment.Domain.Rules;

public static class RealisedGainCalculator
{
    public static RealisedGain Calculate(
        Transaction sellingTransaction,
        IEnumerable<Transaction> precedingTransactions,
        CostBasisMethod method,
        string brokerCurrency,
        DateTimeOffset createdAt,
        IReadOnlyList<SpecificLotAllocation>? allocation = null,
        IEnumerable<CorporateAction>? precedingCorporateActions = null)
    {
        var preceding = precedingTransactions as IReadOnlyList<Transaction> ?? precedingTransactions.ToList();
        var precedingActions = precedingCorporateActions?.ToList() ?? new List<CorporateAction>();

        var lotsConsumed = method switch
        {
            CostBasisMethod.AverageCost => BuildAverageCostLot(sellingTransaction, preceding, precedingActions),
            CostBasisMethod.FIFO => BuildAutoConsumedLots(sellingTransaction, preceding, precedingActions),
            CostBasisMethod.SpecificId => BuildSpecificIdLots(sellingTransaction, preceding, allocation, precedingActions),
            _ => throw new ArgumentOutOfRangeException(nameof(method), method, "Unknown cost basis method.")
        };

        var taxYear = TaxYearCalculator.Calculate(sellingTransaction.Date, brokerCurrency);

        return RealisedGain.Create(
            sellingTransaction.Id,
            sellingTransaction.Date,
            method,
            lotsConsumed,
            sellingTransaction.Quantity,
            sellingTransaction.NetCash,
            sellingTransaction.Currency,
            taxYear,
            createdAt);
    }

    private static IReadOnlyList<RealisedGainLot> BuildAverageCostLot(
        Transaction sellingTransaction, IReadOnlyList<Transaction> preceding, IReadOnlyList<CorporateAction> precedingCorporateActions)
    {
        var (_, averagePrice) = CorporateActionReplay.ReplayAveragePosition(preceding, precedingCorporateActions);

        return new[] { new RealisedGainLot(null, sellingTransaction.Quantity, averagePrice) };
    }

    private static IReadOnlyList<RealisedGainLot> BuildAutoConsumedLots(
        Transaction sellingTransaction, IReadOnlyList<Transaction> preceding, IReadOnlyList<CorporateAction> precedingCorporateActions)
    {
        var openLots = OpenLotTracker.GetOpenLots(preceding, precedingCorporateActions);
        var remaining = sellingTransaction.Quantity;
        var consumed = new List<RealisedGainLot>();

        foreach (var lot in openLots)
        {
            if (remaining <= 0)
            {
                break;
            }

            var take = Math.Min(lot.RemainingQuantity, remaining);
            consumed.Add(new RealisedGainLot(lot.SourceTransactionId, take, lot.UnitCost));
            remaining -= take;
        }

        return consumed;
    }

    private static IReadOnlyList<RealisedGainLot> BuildSpecificIdLots(
        Transaction sellingTransaction,
        IReadOnlyList<Transaction> preceding,
        IReadOnlyList<SpecificLotAllocation>? allocation,
        IReadOnlyList<CorporateAction> precedingCorporateActions)
    {
        if (allocation is null || allocation.Count == 0)
        {
            throw new InvestmentRuleViolationException("A SpecificId sale requires at least one lot allocation.");
        }

        var allocatedQuantity = allocation.Sum(entry => entry.Quantity);
        if (allocatedQuantity != sellingTransaction.Quantity)
        {
            throw new InvestmentRuleViolationException(
                $"Allocated lot quantity ({allocatedQuantity}) does not match the sale quantity ({sellingTransaction.Quantity}).");
        }

        var openLots = OpenLotTracker.GetOpenLots(preceding, precedingCorporateActions).ToDictionary(lot => lot.SourceTransactionId);
        var consumed = new List<RealisedGainLot>();

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

            consumed.Add(new RealisedGainLot(lot.SourceTransactionId, entry.Quantity, lot.UnitCost));
        }

        return consumed;
    }
}
