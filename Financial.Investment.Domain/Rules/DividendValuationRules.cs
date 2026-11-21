namespace Financial.Investment.Domain.Rules;

public static class DividendValuationRules
{
    public static readonly decimal RequiredYield = 0.06m;
    public static readonly int DividendYearsLookback = 5;

    public static decimal? CalculatePriceMaxBuy(decimal averageDividend) =>
        averageDividend > 0m ? averageDividend / RequiredYield : null;

    public static decimal? CalculateDiscountPercent(decimal price, decimal? priceMaxBuy) =>
        priceMaxBuy is decimal maxBuy && price > 0m ? (1m - (price / maxBuy)) * 100m : null;

    public static decimal? CalculateDividendYieldPercent(decimal averageDividend, decimal price) =>
        price > 0m ? (averageDividend / price) * 100m : null;
}
