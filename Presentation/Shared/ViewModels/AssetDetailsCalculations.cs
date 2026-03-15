namespace Financial.Presentation.Shared.ViewModels;

public static class AssetDetailsCalculations
{
    public static decimal CalculateTotalCurrentValue(decimal todayCurrentValue, decimal quantity)
    {
        return todayCurrentValue * quantity;
    }

    public static decimal CalculateResultPercent(decimal averagePrice, decimal quantity, decimal totalCurrentValue)
    {
        if (!HasAveragePrice(averagePrice, quantity))
        {
            return 0;
        }

        var investedTotal = averagePrice * quantity;
        return investedTotal == 0 ? 0 : (totalCurrentValue / investedTotal) - 1;
    }

    public static decimal CalculateResultPercentWithCredits(decimal averagePrice, decimal quantity, decimal totalCurrentValueWithCredits)
    {
        if (!HasAveragePrice(averagePrice, quantity))
        {
            return 0;
        }

        var investedTotal = averagePrice * quantity;
        return investedTotal == 0 ? 0 : (totalCurrentValueWithCredits / investedTotal) - 1;
    }

    public static bool HasAveragePrice(decimal averagePrice, decimal quantity)
    {
        return averagePrice > 0 && quantity > 0;
    }
}
