namespace Financial.Investment.Application.Validation;

internal static class EnumParser
{
    public static bool TryNormalize<TEnum>(string? value, out string normalized)
        where TEnum : struct, Enum
    {
        if (Enum.TryParse<TEnum>(value, ignoreCase: true, out var parsed))
        {
            normalized = parsed.ToString();
            return true;
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
