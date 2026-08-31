using Financial.CashFlow.Domain.Enums;

namespace Financial.CashFlow.Application.Validation;

public static class IncomeGroupParser
{
    public static bool TryParse(string? value, out IncomeGroup group) =>
        EnumParser.TryParseEnum(value, out group);
}
