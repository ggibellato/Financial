using Financial.Investment.Domain.Entities;

namespace Financial.Investment.Domain.Rules;

public static class AverageCostReplay
{
    public static decimal Apply(decimal currentQuantity, decimal currentAveragePrice, Transaction transaction)
    {
        var cost = transaction.UnitPrice * transaction.Quantity + transaction.Fees;
        return Blend(currentQuantity, currentAveragePrice, transaction.Quantity, cost);
    }

    public static decimal Blend(decimal currentQuantity, decimal currentAveragePrice, decimal incomingQuantity, decimal incomingCost)
    {
        var resultingQuantity = currentQuantity + incomingQuantity;
        return resultingQuantity == 0
            ? 0m
            : (currentAveragePrice * currentQuantity + incomingCost) / resultingQuantity;
    }
}
