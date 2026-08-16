namespace Financial.Investment.Domain.Rules;

public static class DividendValuationRules
{
    public static readonly decimal RequiredYield = 0.06m;
    public static readonly int DividendYearsLookback = 5;

    public static decimal CalculatePriceMaxBuy(decimal averageDividend) =>
        averageDividend > 0m ? averageDividend / RequiredYield : 0m;

    public static decimal CalculateDiscountPercent(decimal price, decimal priceMaxBuy) =>
        priceMaxBuy > 0m ? (1m - (price / priceMaxBuy)) * 100m : 0m;

    public static decimal CalculateDividendYieldPercent(decimal averageDividend, decimal price) =>
        price > 0m ? (averageDividend / price) * 100m : 0m;
}
