using System;
using System.Collections.Generic;
using System.Linq;
using Financial.Investment.Domain.Entities;

namespace Financial.Investment.Domain.Rules;

public sealed record OpenLot(Guid SourceTransactionId, DateTime Date, decimal RemainingQuantity, decimal UnitCost);

// Depletes oldest-first even for SpecificId brokers, as a fallback until DisposalRecord.LotsConsumed exists.
public static class OpenLotTracker
{
    private sealed class MutableLot
    {
        public required Guid SourceTransactionId { get; init; }
        public required DateTime Date { get; init; }
        public required decimal RemainingQuantity { get; set; }
        public required decimal UnitCost { get; init; }
    }

    public static IReadOnlyList<OpenLot> GetOpenLots(IEnumerable<Transaction> transactions)
    {
        var lots = new List<MutableLot>();

        foreach (var transaction in TransactionReplayOrder.Sort(transactions))
        {
            var effect = TransactionTypeEffects.For(transaction.Type);

            switch (effect.Quantity)
            {
                case QuantityEffect.Increase:
                    lots.Add(new MutableLot
                    {
                        SourceTransactionId = transaction.Id,
                        Date = transaction.Date,
                        RemainingQuantity = transaction.Quantity,
                        UnitCost = (transaction.UnitPrice * transaction.Quantity + transaction.Fees) / transaction.Quantity
                    });
                    break;

                case QuantityEffect.Decrease:
                    Deplete(lots, transaction.Quantity);
                    break;
            }
        }

        return lots
            .Where(lot => lot.RemainingQuantity > 0)
            .Select(lot => new OpenLot(lot.SourceTransactionId, lot.Date, lot.RemainingQuantity, lot.UnitCost))
            .ToList();
    }

    private static void Deplete(List<MutableLot> lots, decimal quantity)
    {
        var remaining = quantity;

        foreach (var lot in lots)
        {
            if (remaining <= 0)
            {
                break;
            }

            if (lot.RemainingQuantity <= 0)
            {
                continue;
            }

            var consumed = Math.Min(lot.RemainingQuantity, remaining);
            lot.RemainingQuantity -= consumed;
            remaining -= consumed;
        }
    }
}
