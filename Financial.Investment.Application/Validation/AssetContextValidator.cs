using System.Diagnostics.CodeAnalysis;

namespace Financial.Investment.Application.Validation;

internal static class AssetContextValidator
{
    public static bool IsInvalid(
        [NotNullWhen(false)] string? brokerName,
        [NotNullWhen(false)] string? portfolioName,
        [NotNullWhen(false)] string? assetName) =>
        string.IsNullOrWhiteSpace(brokerName) ||
        string.IsNullOrWhiteSpace(portfolioName) ||
        string.IsNullOrWhiteSpace(assetName);
}
