using Financial.CashFlow.Domain.Enums;

namespace Financial.CashFlow.Application.Validation;

public static class ReserveBucketParser
{
    public static bool TryParse(string? value, out ReserveBucket bucket) =>
        EnumParser.TryParseEnum(value, out bucket);
}
