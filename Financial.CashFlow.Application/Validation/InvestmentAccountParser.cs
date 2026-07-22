using Financial.CashFlow.Domain.Enums;

namespace Financial.CashFlow.Application.Validation;

public static class InvestmentAccountParser
{
    public static bool TryParse(string? value, out InvestmentAccount account) =>
        EnumParser.TryParseEnum(value, out account);
}
