using System;
using System.Collections.Generic;

namespace Financial.Application.Validation;

internal static class EnumParser
{
    public static bool TryNormalize(string? value, IReadOnlyList<string> canonicalValues, out string normalized)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            normalized = string.Empty;
            return false;
        }

        foreach (var canonicalValue in canonicalValues)
        {
            if (string.Equals(value, canonicalValue, StringComparison.OrdinalIgnoreCase))
            {
                normalized = canonicalValue;
                return true;
            }
        }

        normalized = string.Empty;
        return false;
    }

    public static bool TryParseEnum<TEnum>(string? value, out TEnum parsed)
        where TEnum : struct, Enum
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            parsed = default;
            return false;
        }

        return Enum.TryParse(value, true, out parsed);
    }
}
