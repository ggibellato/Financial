using Financial.CashFlow.Domain.Enums;

namespace Financial.CashFlow.Application.Validation;

public static class CategoryParser
{
    public static bool TryParse(string? value, out Category category) =>
        EnumParser.TryParseEnum(value, out category);
}
