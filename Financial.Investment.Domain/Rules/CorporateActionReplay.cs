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

// Corporate-action steps always rank ahead of same-date transactions, and same-date transactions
// keep TransactionReplayOrder's purchases-before-sales tie-break - this is the single ordering rule
// every replay consumer (position, lots, disposal calc) shares instead of re-deriving it.
public static class CorporateActionReplay
{
    public static IEnumerable<ReplayStep> Merge(IEnumerable<Transaction> transactions, IEnumerable<CorporateAction> corporateActions)
    {
        var transactionSteps = transactions.Select(t => (ReplayStep)new TransactionReplayStep(t));
        var corporateActionSteps = corporateActions.Select(c => (ReplayStep)new CorporateActionReplayStep(c));

        return transactionSteps.Concat(corporateActionSteps).OrderBy(step => step.Date).ThenBy(RankWithinDate);
    }

    public static (decimal Quantity, decimal AveragePrice) RescalePosition(decimal quantity, decimal averagePrice, decimal factor) =>
        (quantity * factor, averagePrice / factor);

    public static IReadOnlyList<OpenLot> RescaleLots(IReadOnlyList<OpenLot> lots, decimal factor) =>
        lots.Select(lot => lot with { RemainingQuantity = lot.RemainingQuantity * factor, UnitCost = lot.UnitCost / factor }).ToList();

    private static int RankWithinDate(ReplayStep step) => step switch
    {
        CorporateActionReplayStep => 0,
        TransactionReplayStep(var transaction) => TransactionTypeEffects.For(transaction.Type).Quantity == QuantityEffect.Decrease ? 2 : 1,
        _ => 3
    };
}
