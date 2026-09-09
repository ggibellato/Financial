using Financial.CashFlow.Domain.Enums;
using Financial.Shared.Abstractions.Validation;

namespace Financial.CashFlow.Application.Validation;

public static class BillStatusParser
{
    public static bool TryParse(string? value, out BillStatus status) =>
        EnumParser.TryParseEnum(value, out status);
}
