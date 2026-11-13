using Financial.Investment.Application.Enums;

namespace Financial.Investment.Application.Validation;

public static class InvestmentScopeParser
{
    public static bool TryParse(string? value, out InvestmentScope scope) =>
        EnumParser.TryParseEnum(value, out scope);

    public static InvestmentScope ParseOrDefault(string? value) =>
        TryParse(value, out var scope) ? scope : InvestmentScope.Active;

    public static InvestmentScope ParseOptional(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return InvestmentScope.Active;
        }

        return TryParse(value, out var scope)
            ? scope
            : throw new ArgumentException($"\"{value}\" is not a recognised investment scope.", nameof(value));
    }
}
