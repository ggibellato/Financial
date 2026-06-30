using Financial.Domain.Entities;

namespace Financial.Application.Validation;

public static class CreditTypeParser
{
    public static bool TryNormalize(string? value, out string normalized) =>
        EnumParser.TryNormalize<Credit.CreditType>(value, out normalized);

    public static bool TryParse(string? value, out Credit.CreditType creditType) =>
        EnumParser.TryParseEnum(value, out creditType);
}
