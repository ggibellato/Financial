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
        public required decimal UnitCost { get; set; }
    }

    public static IReadOnlyList<OpenLot> GetOpenLots(IEnumerable<Transaction> transactions) =>
        GetOpenLots(transactions, Array.Empty<CorporateAction>());

    public static IReadOnlyList<OpenLot> GetOpenLots(IEnumerable<Transaction> transactions, IEnumerable<CorporateAction> corporateActions)
    {
        var lots = new List<MutableLot>();

        foreach (var step in CorporateActionReplay.Merge(transactions, corporateActions))
        {
            switch (step)
            {
                case CorporateActionReplayStep(var corporateAction):
                    ApplyCorporateAction(lots, corporateAction);
                    break;

                case TransactionReplayStep(var transaction):
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
                    break;
            }
        }

        return lots
            .Where(lot => lot.RemainingQuantity > 0)
            .Select(lot => new OpenLot(lot.SourceTransactionId, lot.Date, lot.RemainingQuantity, lot.UnitCost))
            .ToList();
    }

    private static void ApplyCorporateAction(List<MutableLot> lots, CorporateAction corporateAction)
    {
        switch (corporateAction)
        {
            case { Type: CorporateAction.CorporateActionType.Split }:
                Rescale(lots, CorporateActionReplay.RequireRatioFactor(corporateAction));
                break;

            case { Type: CorporateAction.CorporateActionType.Merger, Role: CorporateAction.CorporateActionRole.Source }:
                lots.Clear();
                break;

            case { Type: CorporateAction.CorporateActionType.Merger, Role: CorporateAction.CorporateActionRole.Target }:
                AppendCarriedLot(lots, corporateAction);
                break;

            case { Type: CorporateAction.CorporateActionType.SpinOff, Role: CorporateAction.CorporateActionRole.Parent }:
                ReduceLotCostBasis(lots, CorporateActionReplay.RequireRetainedFraction(corporateAction));
                break;

            case { Type: CorporateAction.CorporateActionType.SpinOff, Role: CorporateAction.CorporateActionRole.New }:
                AppendCarriedLot(lots, corporateAction);
                break;

            default:
                throw new ArgumentOutOfRangeException(nameof(corporateAction), corporateAction.Type, "Unsupported corporate action type/role combination.");
        }
    }

    private static void AppendCarriedLot(List<MutableLot> lots, CorporateAction corporateAction) =>
        lots.Add(new MutableLot
        {
            SourceTransactionId = corporateAction.Id,
            Date = corporateAction.EffectiveDate,
            RemainingQuantity = corporateAction.ConvertedQuantity!.Value,
            UnitCost = corporateAction.CarriedCostBasis!.Value / corporateAction.ConvertedQuantity!.Value
        });

    private static void ReduceLotCostBasis(List<MutableLot> lots, decimal retainedFraction)
    {
        foreach (var lot in lots)
        {
            lot.UnitCost *= retainedFraction;
        }
    }

    private static void Rescale(List<MutableLot> lots, decimal factor)
    {
        foreach (var lot in lots)
        {
            lot.RemainingQuantity *= factor;
            lot.UnitCost /= factor;
        }
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
