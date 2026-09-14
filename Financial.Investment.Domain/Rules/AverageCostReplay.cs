using Financial.Investment.Domain.Entities;

namespace Financial.Investment.Domain.Rules;

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
