using Financial.CashFlow.Domain.Rules;
using FluentAssertions;

namespace Financial.CashFlow.Domain.Tests;

public class ReserveSplitCalculatorTests
{
    [Fact]
    public void Calculate_ComputesDizimoAsExactly10PercentOfCombinedIncome()
    {
        var result = ReserveSplitCalculator.Calculate(
            gleisonSalaryNet: 3600m,
            arianaSalaryNet: 2600m,
            lottery: 50m,
            dividendoJuros: 120m);

        // (3600 + 2600 + 50 + 120) * 10% = 637
        result.Dizimo.Should().Be(637m);
    }

    [Fact]
    public void Calculate_SplitsLimpoIntoExactThirdsAndSixths()
    {
        var result = ReserveSplitCalculator.Calculate(
            gleisonSalaryNet: 3600m,
            arianaSalaryNet: 2600m,
            lottery: 50m,
            dividendoJuros: 120m);

        // Limpo = (3600 + 2600) - 637 = 5563
        result.Investimento.Should().Be(1854.33m);
        result.HouseTreats.Should().Be(1854.33m);
        result.Ariana.Should().Be(927.17m);
        result.Gleison.Should().Be(927.17m);
    }

    [Fact]
    public void Calculate_LotteryAndDividendoJuros_AreNeverDirectlyAddedToTheSplitPool()
    {
        // Lottery/Dividendo/Juros are not part of the 1/3-1/3-1/6-1/6 split pool itself (Limpo is
        // combined net salary minus Dizimo, full stop) - but since Dizimo grows with Lottery/Dividendo,
        // Limpo (and therefore the four splits) shrinks correspondingly. This proves the splits are
        // always derived from Limpo alone, never from (net salary + Lottery + Dividendo) directly.
        const decimal combinedNetSalary = 3600m + 2600m;

        var withoutExtraIncome = ReserveSplitCalculator.Calculate(3600m, 2600m, lottery: 0m, dividendoJuros: 0m);
        var withExtraIncome = ReserveSplitCalculator.Calculate(3600m, 2600m, lottery: 500m, dividendoJuros: 300m);

        withExtraIncome.Dizimo.Should().BeGreaterThan(withoutExtraIncome.Dizimo);

        var limpoWithout = combinedNetSalary - withoutExtraIncome.Dizimo;
        var limpoWith = combinedNetSalary - withExtraIncome.Dizimo;
        (withoutExtraIncome.Investimento + withoutExtraIncome.HouseTreats + withoutExtraIncome.Ariana + withoutExtraIncome.Gleison)
            .Should().BeApproximately(limpoWithout, 0.02m);
        (withExtraIncome.Investimento + withExtraIncome.HouseTreats + withExtraIncome.Ariana + withExtraIncome.Gleison)
            .Should().BeApproximately(limpoWith, 0.02m);
    }

    [Fact]
    public void Calculate_AllZeroInputs_ProducesAllZeroOutputs()
    {
        var result = ReserveSplitCalculator.Calculate(0m, 0m, 0m, 0m);

        result.Dizimo.Should().Be(0m);
        result.Investimento.Should().Be(0m);
        result.HouseTreats.Should().Be(0m);
        result.Ariana.Should().Be(0m);
        result.Gleison.Should().Be(0m);
    }
}
