using System;

namespace Financial.CashFlow.Domain.Rules;

public static class ReserveSplitCalculator
{
    private const decimal DizimoRate = 0.10m;

    public static ReserveSplitResult Calculate(
        decimal gleisonSalaryNet,
        decimal arianaSalaryNet,
        decimal lottery,
        decimal dividendoJuros)
    {
        // Dizimo (tithe) is computed on the combined household income but is informational only —
        // it is paid outside the app and never funds the Reserva pool itself.
        var dizimo = Round((gleisonSalaryNet + arianaSalaryNet + lottery + dividendoJuros) * DizimoRate);

        // Only Ariana's wage, net of her share of the tithe, seeds the Reserva pool.
        var limpo = arianaSalaryNet - dizimo;

        var investimento = Round(limpo / 3m);
        var houseTreats = Round(limpo / 3m);
        var ariana = Round(limpo / 6m);
        var gleison = Round(limpo / 6m);

        return new ReserveSplitResult(dizimo, investimento, houseTreats, ariana, gleison);
    }

    private static decimal Round(decimal value) => Math.Round(value, 2, MidpointRounding.AwayFromZero);
}
