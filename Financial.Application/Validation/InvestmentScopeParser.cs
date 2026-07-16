using Financial.Application.Interfaces;

namespace Financial.Application.Validation;

public static class InvestmentScopeParser
{
    public static bool TryNormalize(string? value, out string normalized) =>
        EnumParser.TryNormalize<InvestmentScope>(value, out normalized);

    public static bool TryParse(string? value, out InvestmentScope scope) =>
        EnumParser.TryParseEnum(value, out scope);

    public static InvestmentScope ParseOrDefault(string? value) =>
        TryParse(value, out var scope) ? scope : InvestmentScope.Active;
}
