using Financial.CashFlow.Domain.Enums;

namespace Financial.CashFlow.Application.Validation;

public static class InvestmentAccountSourceParser
{
    public static bool TryParse(string? value, out InvestmentAccountSource source) =>
        EnumParser.TryParseEnum(value, out source);
}
