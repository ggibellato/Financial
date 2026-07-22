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
        var combinedNetSalary = gleisonSalaryNet + arianaSalaryNet;
        var dizimo = Round((combinedNetSalary + lottery + dividendoJuros) * DizimoRate);
        var limpo = combinedNetSalary - dizimo;

        var investimento = Round(limpo / 3m);
        var houseTreats = Round(limpo / 3m);
        var ariana = Round(limpo / 6m);
        var gleison = Round(limpo / 6m);

        return new ReserveSplitResult(dizimo, investimento, houseTreats, ariana, gleison);
    }

    private static decimal Round(decimal value) => Math.Round(value, 2, MidpointRounding.AwayFromZero);
}
