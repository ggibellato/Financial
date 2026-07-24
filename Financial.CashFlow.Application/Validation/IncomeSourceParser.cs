using Financial.CashFlow.Domain.Enums;

namespace Financial.CashFlow.Application.Validation;

public static class IncomeSourceParser
{
    public static bool TryParse(string? value, out IncomeSource incomeSource) =>
        EnumParser.TryParseEnum(value, out incomeSource);
}
