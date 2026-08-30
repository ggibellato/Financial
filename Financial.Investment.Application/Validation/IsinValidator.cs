using System.Text.RegularExpressions;

namespace Financial.Investment.Application.Validation;

/// <summary>
/// Validates the ISO 6166 ISIN shape: a 2-letter country code, 9 alphanumeric characters, and a
/// single check digit (12 characters total). ISIN is an optional field, so a blank value is valid.
/// </summary>
internal static partial class IsinValidator
{
    public static bool IsValid(string? isin) =>
        string.IsNullOrWhiteSpace(isin) || IsinPattern().IsMatch(isin.Trim());

    [GeneratedRegex("^[A-Z]{2}[A-Z0-9]{9}[0-9]$")]
    private static partial Regex IsinPattern();
}
