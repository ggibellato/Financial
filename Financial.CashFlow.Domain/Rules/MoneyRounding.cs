using System;

namespace Financial.CashFlow.Domain.Rules;

public static class MoneyRounding
{
    public static decimal ToCents(decimal value) => Math.Round(value, 2, MidpointRounding.AwayFromZero);
}
