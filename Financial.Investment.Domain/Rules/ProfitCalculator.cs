namespace Financial.Investment.Domain.Rules;

public static class ProfitCalculator
{
    public static bool HasCostBasis(decimal averagePrice, decimal quantity) =>
        averagePrice > 0 && quantity > 0;

    public static decimal CalculateResultFraction(decimal averagePrice, decimal quantity, decimal currentValue)
    {
        if (!HasCostBasis(averagePrice, quantity))
        {
            return 0m;
        }

        var costBasis = averagePrice * quantity;
        return (currentValue / costBasis) - 1;
    }

    public static decimal? CalculateProfitPercent(decimal currentValue, decimal costBasis) =>
        costBasis != 0 ? (currentValue - costBasis) / costBasis * 100 : null;
}
