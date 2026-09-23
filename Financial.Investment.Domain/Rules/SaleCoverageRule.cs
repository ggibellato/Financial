using System;
using System.Collections.Generic;
using Financial.Investment.Domain.Entities;

namespace Financial.Investment.Domain.Rules;

public sealed record SaleCoverageViolation(Transaction OffendingSale, decimal QuantityHeld, decimal Shortfall);

public static class SaleCoverageRule
{
    public static SaleCoverageViolation? FindFirstUncoveredSale(IEnumerable<Transaction> transactions) =>
        FindFirstUncoveredSale(transactions, Array.Empty<CorporateAction>());

    public static SaleCoverageViolation? FindFirstUncoveredSale(IEnumerable<Transaction> transactions, IEnumerable<CorporateAction> corporateActions)
    {
        var quantity = 0m;

        foreach (var step in CorporateActionReplay.Merge(transactions, corporateActions))
        {
            switch (step)
            {
                case CorporateActionReplayStep(var corporateAction):
                    quantity = CorporateActionReplay.RescalePosition(quantity, 0m, corporateAction.RatioFactor).Quantity;
                    break;

                case TransactionReplayStep(var transaction):
                    var quantityEffect = TransactionTypeEffects.For(transaction.Type).Quantity;

                    if (quantityEffect == QuantityEffect.Increase)
                    {
                        quantity += transaction.Quantity;
                    }
                    else if (quantityEffect == QuantityEffect.Decrease)
                    {
                        if (transaction.Quantity > quantity)
                        {
                            return new SaleCoverageViolation(transaction, quantity, transaction.Quantity - quantity);
                        }

                        quantity -= transaction.Quantity;
                    }
                    break;
            }
        }

        return null;
    }
}
