using System.Globalization;
using System.Text.RegularExpressions;

namespace Financial.Infrastructure.Integrations.WebPageParser;

internal static class GoogleFinanceParsing
{
    internal static decimal ParsePriceValue(string rawValue)
    {
        var cleaned = rawValue
            .Replace("R$", "")
            .Replace("?", "")
            .Replace("$", "")
            .Replace("GBX", "")
            .Replace("£", "")
            .Trim();
        var value = decimal.Parse(cleaned);
        if (rawValue.Contains("GBX"))
        {
            value /= 100;
        }

        return value;
    }

    internal static DateTimeOffset? TryParseAsOf(string? rawValue)
    {
        if (string.IsNullOrWhiteSpace(rawValue))
        {
            return null;
        }

        var candidate = NormalizeAsOfCandidate(rawValue);
        if (TryParseUtcOffsetStamp(candidate, out var parsed))
        {
            return parsed;
        }

        var formats = new[]
        {
            "MMM d, h:mm:ss tt",
            "MMM dd, h:mm:ss tt",
            "MMM d, hh:mm:ss tt",
            "MMM dd, hh:mm:ss tt",
            "MMM d, h:mm:ss tt UTC zzz",
            "MMM dd, h:mm:ss tt UTC zzz",
            "MMM d, hh:mm:ss tt UTC zzz",
            "MMM dd, hh:mm:ss tt UTC zzz"
        };

        if (DateTimeOffset.TryParseExact(candidate,
                formats,
                CultureInfo.InvariantCulture,
                DateTimeStyles.AllowWhiteSpaces | DateTimeStyles.AssumeLocal,
                out parsed))
        {
            return parsed;
        }

        if (DateTimeOffset.TryParse(candidate, CultureInfo.InvariantCulture, DateTimeStyles.AllowWhiteSpaces, out parsed))
        {
            return parsed;
        }

        if (DateTimeOffset.TryParse(candidate, CultureInfo.GetCultureInfo("en-US"), DateTimeStyles.AllowWhiteSpaces, out parsed))
        {
            return parsed;
        }

        return null;
    }

    private static bool TryParseUtcOffsetStamp(string candidate, out DateTimeOffset parsed)
    {
        var match = Regex.Match(candidate,
            @"^(?<date>.+?)\s+(UTC|GMT)(?<sign>[+-])(?<hours>\d{1,2})(:(?<minutes>\d{2}))?$",
            RegexOptions.IgnoreCase);
        if (match.Success)
        {
            var datePart = match.Groups["date"].Value.Trim();
            var sign = match.Groups["sign"].Value == "-" ? -1 : 1;
            var hours = int.Parse(match.Groups["hours"].Value, CultureInfo.InvariantCulture);
            var minutes = match.Groups["minutes"].Success
                ? int.Parse(match.Groups["minutes"].Value, CultureInfo.InvariantCulture)
                : 0;
            var dateFormats = new[]
            {
                "MMM d, h:mm:ss tt",
                "MMM dd, h:mm:ss tt",
                "MMM d, hh:mm:ss tt",
                "MMM dd, hh:mm:ss tt"
            };
            if (DateTime.TryParseExact(datePart, dateFormats, CultureInfo.InvariantCulture, DateTimeStyles.AllowWhiteSpaces, out var dateTime))
            {
                var offset = TimeSpan.FromMinutes(sign * (hours * 60 + minutes));
                parsed = new DateTimeOffset(dateTime, offset);
                return true;
            }
        }

        parsed = default;
        return false;
    }

    private static string NormalizeAsOfCandidate(string value)
    {
        var candidate = value.Trim()
            .Replace('\u202F', ' ')
            .Replace('\u00A0', ' ')
            .Replace('\u2212', '-')
            .Replace('\u2010', '-')
            .Replace('\u2011', '-')
            .Replace('\u2012', '-')
            .Replace('\u2013', '-')
            .Replace('\u2014', '-');

        var asOfIndex = candidate.IndexOf("As of", StringComparison.OrdinalIgnoreCase);
        if (asOfIndex >= 0)
        {
            candidate = candidate[(asOfIndex + "As of".Length)..].Trim().TrimStart(':').Trim();
        }

        var middotIndex = candidate.IndexOf("&middot;", StringComparison.OrdinalIgnoreCase);
        if (middotIndex >= 0)
        {
            candidate = candidate[..middotIndex].Trim();
        }

        var bulletIndex = candidate.IndexOf('·');
        if (bulletIndex >= 0)
        {
            candidate = candidate[..bulletIndex].Trim();
        }

        candidate = Regex.Replace(candidate,
            @"(?:UTC|GMT)(?<sign>[+-])(?<hours>\d{1,2})(:(?<minutes>\d{2}))?\b",
            match =>
            {
                var sign = match.Groups["sign"].Value;
                var hours = int.Parse(match.Groups["hours"].Value, CultureInfo.InvariantCulture);
                var minutes = match.Groups["minutes"].Success
                    ? int.Parse(match.Groups["minutes"].Value, CultureInfo.InvariantCulture)
                    : 0;
                var offsetFormatted = $"{sign}{hours:00}:{minutes:00}";
                return $"UTC{offsetFormatted}";
            },
            RegexOptions.IgnoreCase);

        return candidate;
    }
}
