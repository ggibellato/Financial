using SharedEnumParser = Financial.Shared.Abstractions.Validation.EnumParser;

namespace Financial.Investment.Application.Validation;

internal static class EnumParser
{
    public static bool TryParseEnum<TEnum>(string? value, out TEnum parsed)
        where TEnum : struct, Enum =>
        SharedEnumParser.TryParseEnum(value, out parsed);

    public static bool TryParseDefinedName<TEnum>(string? value, out TEnum parsed)
        where TEnum : struct, Enum =>
        SharedEnumParser.TryParseDefinedName(value, out parsed);

    public static TEnum? ParseOptionalDefinedName<TEnum>(string? value, string errorMessage)
        where TEnum : struct, Enum
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return TryParseDefinedName(value.Trim(), out TEnum parsed) ? parsed : throw new ArgumentException(errorMessage);
    }
}
