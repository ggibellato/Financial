using System.Text.RegularExpressions;
using Financial.Investment.Domain.Entities;

namespace Financial.Presentation.App.ViewModels.Admin;

public static partial class AssetIdentityValidation
{
    public static string? ValidateIsin(string isin)
    {
        var trimmed = isin.Trim();
        return trimmed.Length == 0 || IsinPattern().IsMatch(trimmed)
            ? null
            : "ISIN must be 2 letters, 9 alphanumeric characters, and a check digit (e.g. US0378331005).";
    }

    [GeneratedRegex("^[A-Z]{2}[A-Z0-9]{9}[0-9]$")]
    private static partial Regex IsinPattern();
}

public static class AssetIdentityOptions
{
    public static IReadOnlyList<CountryCode> CountryOptions { get; } = Enum.GetValues<CountryCode>();

    public static IReadOnlyList<GlobalAssetClass> ClassOptions { get; } = Enum.GetValues<GlobalAssetClass>();
}
