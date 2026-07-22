using Financial.CashFlow.Domain.Enums;

namespace Financial.CashFlow.Application.Validation;

public static class AreaParser
{
    public static bool TryParse(string? value, out Area area) =>
        EnumParser.TryParseEnum(value, out area);
}
