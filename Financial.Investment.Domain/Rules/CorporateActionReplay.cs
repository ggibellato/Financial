using System;
using System.Collections.Generic;
using System.Linq;
using Financial.Investment.Domain.Entities;

namespace Financial.Investment.Domain.Rules;

public abstract record ReplayStep
{
    public abstract DateTime Date { get; }
}

public sealed record TransactionReplayStep(Transaction Transaction) : ReplayStep
{
    public override DateTime Date => Transaction.Date;
}

public sealed record CorporateActionReplayStep(CorporateAction CorporateAction) : ReplayStep
{
    public override DateTime Date => CorporateAction.EffectiveDate;
}

// Every replay consumer (position, lots, disposal calc) shares this ordering instead of re-deriving it.
public static class CorporateActionReplay
{
    public static IEnumerable<ReplayStep> Merge(IEnumerable<Transaction> transactions, IEnumerable<CorporateAction> corporateActions)
    {
        var transactionSteps = transactions.Select(t => (ReplayStep)new TransactionReplayStep(t));
        var corporateActionSteps = corporateActions.Select(c => (ReplayStep)new CorporateActionReplayStep(c));

        return transactionSteps.Concat(corporateActionSteps).OrderBy(step => step.Date).ThenBy(RankWithinDate);
    }

    private static readonly (decimal Quantity, decimal AveragePrice) ClosedPosition = (0m, 0m);

    public static (decimal Quantity, decimal AveragePrice) RescalePosition(decimal quantity, decimal averagePrice, decimal factor) =>
        (quantity * factor, averagePrice / factor);

    public static (decimal Quantity, decimal AveragePrice) RescalePosition(decimal quantity, decimal averagePrice, CorporateAction action) =>
        RescalePosition(quantity, averagePrice, RequireRatioFactor(action));

    public static (decimal Quantity, decimal AveragePrice) ApplyToPosition(decimal quantity, decimal averagePrice, CorporateAction action) =>
        action switch
        {
            { Type: CorporateAction.CorporateActionType.Split } => RescalePosition(quantity, averagePrice, action),
            { Type: CorporateAction.CorporateActionType.Merger, Role: CorporateAction.CorporateActionRole.Source } => ClosedPosition,
            { Type: CorporateAction.CorporateActionType.Merger, Role: CorporateAction.CorporateActionRole.Target } =>
                ReceiveIntoPosition(quantity, averagePrice, action.ConvertedQuantity!.Value, action.CarriedCostBasis!.Value),
            { Type: CorporateAction.CorporateActionType.SpinOff, Role: CorporateAction.CorporateActionRole.Parent } =>
                (quantity, averagePrice * RequireRetainedFraction(action)),
            { Type: CorporateAction.CorporateActionType.SpinOff, Role: CorporateAction.CorporateActionRole.New } =>
                ReceiveIntoPosition(quantity, averagePrice, action.ConvertedQuantity!.Value, action.CarriedCostBasis!.Value),
            _ => throw new ArgumentOutOfRangeException(nameof(action), action.Type, "Unsupported corporate action type/role combination.")
        };

    public static (decimal Quantity, decimal AveragePrice) ReceiveIntoPosition(decimal quantity, decimal averagePrice, decimal receivedQuantity, decimal carriedCostBasis) =>
        (quantity + receivedQuantity, AverageCostReplay.Blend(quantity, averagePrice, receivedQuantity, carriedCostBasis));

    public static decimal RequireRatioFactor(CorporateAction action) =>
        action.RatioFactor ?? throw new InvalidOperationException($"Corporate action {action.Id} of type {action.Type} has no ratio factor.");

    public static decimal RequireRetainedFraction(CorporateAction action) =>
        1 - (action.AllocationPercentage ?? throw new InvalidOperationException(
            $"Corporate action {action.Id} of type {action.Type} has no allocation percentage.")) / 100;

    public static IReadOnlyList<OpenLot> RescaleLots(IReadOnlyList<OpenLot> lots, decimal factor) =>
        lots.Select(lot => lot with { RemainingQuantity = lot.RemainingQuantity * factor, UnitCost = lot.UnitCost / factor }).ToList();

    public static decimal ApplyTransactionToQuantity(decimal quantity, Transaction transaction) =>
        TransactionTypeEffects.For(transaction.Type).Quantity switch
        {
            QuantityEffect.Increase => quantity + transaction.Quantity,
            QuantityEffect.Decrease => quantity - transaction.Quantity,
            _ => quantity
        };

    // Shared by every average-cost position replay (a running "as of" preview and disposal cost-basis
    // calculation both need the same quantity/average-price fold); stopBefore lets a caller cut the
    // replay short instead of always walking the whole sequence.
    public static (decimal Quantity, decimal AveragePrice) ReplayAveragePosition(
        IEnumerable<Transaction> transactions, IEnumerable<CorporateAction> corporateActions, Func<ReplayStep, bool>? stopBefore = null)
    {
        var quantity = 0m;
        var averagePrice = 0m;

        foreach (var step in Merge(transactions, corporateActions))
        {
            if (stopBefore is not null && stopBefore(step))
            {
                break;
            }

            switch (step)
            {
                case CorporateActionReplayStep(var corporateAction):
                    (quantity, averagePrice) = ApplyToPosition(quantity, averagePrice, corporateAction);
                    break;

                case TransactionReplayStep(var transaction):
                    if (TransactionTypeEffects.For(transaction.Type).Quantity == QuantityEffect.Increase)
                    {
                        averagePrice = AverageCostReplay.Apply(quantity, averagePrice, transaction);
                    }

                    quantity = ApplyTransactionToQuantity(quantity, transaction);
                    break;
            }
        }

        return (quantity, averagePrice);
    }

    private static int RankWithinDate(ReplayStep step) => step switch
    {
        CorporateActionReplayStep => 0,
        TransactionReplayStep(var transaction) => TransactionReplayOrder.IsDecrease(transaction) ? 2 : 1,
        _ => 3
    };
}
