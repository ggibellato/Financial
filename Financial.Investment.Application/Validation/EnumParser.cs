using SharedEnumParser = Financial.Shared.Abstractions.Validation.EnumParser;

namespace Financial.Investment.Application.Validation;

internal static class EnumParser
{
    public static bool TryParseEnum<TEnum>(string? value, out TEnum parsed)
        where TEnum : struct, Enum =>
        SharedEnumParser.TryParseEnum(value, out parsed);
}
