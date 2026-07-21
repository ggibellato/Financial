using Financial.Investment.Application.Enums;

namespace Financial.Investment.Application.Validation;

public static class InvestmentScopeParser
{
    public static bool TryParse(string? value, out InvestmentScope scope) =>
        EnumParser.TryParseEnum(value, out scope);

    public static InvestmentScope ParseOrDefault(string? value) =>
        TryParse(value, out var scope) ? scope : InvestmentScope.Active;
}
