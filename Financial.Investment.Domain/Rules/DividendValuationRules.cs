namespace Financial.Investment.Domain.Rules;

public static class DividendValuationRules
{
    public static readonly decimal RequiredYield = 0.06m;
    public static readonly int DividendYearsLookback = 5;
}
