using Financial.Domain.Entities;

namespace Financial.Application.Validation;

public static class CreditTypeParser
{
    private static readonly string[] NormalizedValues = { "Dividend", "Rent" };

    public static bool TryNormalize(string? value, out string normalized)
    {
        return EnumParser.TryNormalize(value, NormalizedValues, out normalized);
    }

    public static bool TryParse(string? value, out Credit.CreditType creditType)
    {
        return EnumParser.TryParseEnum(value, out creditType);
    }
}
