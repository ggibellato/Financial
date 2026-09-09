using Financial.CashFlow.Domain.Enums;
using Financial.Shared.Abstractions.Validation;

namespace Financial.CashFlow.Application.Validation;

public static class CurrencyParser
{
    public static bool TryParse(string? value, out Currency currency) =>
        EnumParser.TryParseEnum(value, out currency);
}
