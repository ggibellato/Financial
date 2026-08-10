using Financial.CashFlow.Domain.Enums;

using CreditCardEnum = Financial.CashFlow.Domain.Enums.CreditCard;

namespace Financial.CashFlow.Application.Validation;

public static class CreditCardParser
{
    public static bool TryParse(string? value, out CreditCardEnum creditCard) =>
        EnumParser.TryParseEnum<CreditCardEnum>(value, out creditCard);
}
