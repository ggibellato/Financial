using Financial.CashFlow.Domain.Enums;

namespace Financial.CashFlow.Application.Validation;

public static class PaymentSourceParser
{
    public static bool TryParse(string? value, out PaymentSource paymentSource) =>
        EnumParser.TryParseEnum(value, out paymentSource);
}
