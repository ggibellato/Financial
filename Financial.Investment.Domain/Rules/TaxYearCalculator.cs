using System;

namespace Financial.Investment.Domain.Rules;

public static class TaxYearCalculator
{
    public static string Calculate(DateTime date, string currency)
    {
        if (string.Equals(currency, "BRL", StringComparison.OrdinalIgnoreCase))
        {
            return date.Year.ToString();
        }

        var isOnOrAfterAprilSixth = date.Month > 4 || (date.Month == 4 && date.Day >= 6);
        var openingYear = isOnOrAfterAprilSixth ? date.Year : date.Year - 1;
        return $"{openingYear}/{(openingYear + 1) % 100:D2}";
    }
}
