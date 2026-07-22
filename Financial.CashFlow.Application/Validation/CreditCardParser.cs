using Financial.CashFlow.Domain.Enums;

namespace Financial.CashFlow.Application.Validation;

public static class CreditCardParser
{
    public static bool TryParse(string? value, out CreditCard creditCard) =>
        EnumParser.TryParseEnum(value, out creditCard);
}
