using Financial.CashFlow.Domain.Enums;

namespace Financial.CashFlow.Application.Validation;

public static class BillStatusParser
{
    public static bool TryParse(string? value, out BillStatus status) =>
        EnumParser.TryParseEnum(value, out status);
}
