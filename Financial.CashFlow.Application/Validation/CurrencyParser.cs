using Financial.Shared.Abstractions.Currencies;
using Financial.Shared.Abstractions.Validation;

namespace Financial.CashFlow.Application.Validation;

public static class CurrencyParser
{
    public static bool TryParse(string? value, out Currency currency) =>
        EnumParser.TryParseEnum(value, out currency);

    public static Currency Parse(string? value) =>
        TryParse(value, out var currency) ? currency : throw new ArgumentException($"Unknown currency '{value}'.");
}
