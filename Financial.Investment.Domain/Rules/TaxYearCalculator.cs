using System;

namespace Financial.Investment.Domain.Rules;

/// <summary>
/// Derives a disposal's tax year from its date and the disposing broker's currency: a BRL broker
/// uses the plain BR calendar year ("2026"); every other currency uses the UK Apr 6-Apr 5 tax year
/// ("2025/26") - a date on or after Apr 6 belongs to that calendar year's opening tax year, a date
/// before Apr 6 belongs to the previous one. No broker-currency domicile tagging exists yet
/// (deferred to a later feature), so currency is the only signal available.
/// </summary>
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
