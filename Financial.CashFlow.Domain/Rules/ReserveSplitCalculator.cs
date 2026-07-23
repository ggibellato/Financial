using System;

namespace Financial.CashFlow.Domain.Rules;

public static class ReserveSplitCalculator
{
    public static ReserveSplitResult Calculate(decimal amount)
    {
        var investimento = Round(amount / 3m);
        var houseTreats = Round(amount / 3m);
        var ariana = Round(amount / 6m);
        var gleison = Round(amount / 6m);

        return new ReserveSplitResult(investimento, houseTreats, ariana, gleison);
    }

    private static decimal Round(decimal value) => Math.Round(value, 2, MidpointRounding.AwayFromZero);
}
