namespace Financial.Domain.Rules;

public static class DividendValuationRules
{
    public const decimal RequiredYield = 0.06m;
    public const int DividendYearsLookback = 5;
}
