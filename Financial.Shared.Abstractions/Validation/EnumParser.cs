namespace Financial.Shared.Abstractions.Validation;

public static class EnumParser
{
    public static bool TryParseEnum<TEnum>(string? value, out TEnum parsed)
        where TEnum : struct, Enum =>
        Enum.TryParse(value, true, out parsed);

    public static bool TryParseDefinedName<TEnum>(string? value, out TEnum parsed)
        where TEnum : struct, Enum =>
        TryParseEnum(value, out parsed) && Enum.IsDefined(parsed) && !int.TryParse(value, out _);
}
