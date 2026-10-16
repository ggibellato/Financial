using Financial.Investment.Domain.Entities;

namespace Financial.Investment.Application.Validation;

public static class CreditTypeParser
{
    public static bool TryParse(string? value, out Credit.CreditType creditType) =>
        EnumParser.TryParseEnum(value, out creditType);
}
