using Financial.CashFlow.Domain.Enums;

namespace Financial.CashFlow.Application.Validation;

public static class CurrencyParser
{
    public static bool TryParse(string? value, out Currency currency) =>
        EnumParser.TryParseEnum(value, out currency);
}
