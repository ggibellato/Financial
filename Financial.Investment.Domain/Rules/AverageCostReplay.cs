using Financial.Investment.Domain.Entities;

namespace Financial.Investment.Domain.Rules;

/// <summary>
/// The single weighted-average-cost step formula, extracted from Transactions.Apply's Increase
/// branch so DisposalRecordCalculator and its one-time backfill (which both need the AveragePrice
/// as of one specific historical moment, not just Transactions' final value) share the exact same
/// math instead of a second hand-written copy that could silently drift from it.
/// </summary>
public static class AverageCostReplay
{
    public static decimal Apply(decimal currentQuantity, decimal currentAveragePrice, Transaction transaction)
    {
        var resultingQuantity = currentQuantity + transaction.Quantity;
        var cost = transaction.UnitPrice * transaction.Quantity + transaction.Fees;
        return resultingQuantity == 0
            ? 0m
            : (currentAveragePrice * currentQuantity + cost) / resultingQuantity;
    }
}
