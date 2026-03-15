using Financial.Domain.Entities;

namespace Financial.Application.Validation;

public static class OperationTypeParser
{
    private static readonly string[] NormalizedValues = { "Buy", "Sell" };

    public static bool TryNormalize(string? value, out string normalized)
    {
        return EnumParser.TryNormalize(value, NormalizedValues, out normalized);
    }

    public static bool TryParse(string? value, out Operation.OperationType operationType)
    {
        return EnumParser.TryParseEnum(value, out operationType);
    }
}
