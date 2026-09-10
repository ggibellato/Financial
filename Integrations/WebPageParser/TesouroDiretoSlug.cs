using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace Financial.Integrations.WebPageParser;

/// <summary>
/// Both Status Invest and redentia.com.br key their per-bond page URLs off the same slug shape
/// (e.g. "TESOURO IPCA+ 2029" -> "tesouro-ipca-2029"), so the derivation is shared rather than
/// duplicated per site.
/// </summary>
internal static class TesouroDiretoSlug
{
    internal static string Derive(string bondTitle)
    {
        var formD = bondTitle.Trim().ToLowerInvariant().Normalize(NormalizationForm.FormD);
        var stripped = new StringBuilder();
        foreach (var c in formD)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
            {
                stripped.Append(c);
            }
        }

        var cleaned = Regex.Replace(stripped.ToString(), @"[^a-z0-9\s-]", string.Empty);
        var collapsed = Regex.Replace(cleaned, @"\s+", " ").Trim();
        return collapsed.Replace(" ", "-");
    }
}
