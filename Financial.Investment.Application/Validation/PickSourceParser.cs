using Financial.Investment.Domain.Entities;

namespace Financial.Investment.Application.Validation;

internal static class PickSourceParser
{
    public static PickSource? ParseOptional(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return EnumParser.TryParseDefinedName(value.Trim(), out PickSource parsed)
            ? parsed
            : throw new ArgumentException("Pick source must be Recommended or MyChoice.");
    }
}
