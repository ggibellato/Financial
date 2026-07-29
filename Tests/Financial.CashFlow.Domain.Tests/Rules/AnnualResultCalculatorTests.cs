using Financial.CashFlow.Domain.Rules;
using Financial.CashFlow.Domain.ValueObjects;
using FluentAssertions;

namespace Financial.CashFlow.Domain.Tests.Rules;

public class AnnualResultCalculatorTests
{
    [Fact]
    public void ComputeResultado_Scalar_ReturnsSalaryMinusDespesasPlusInvestimento()
    {
        var result = AnnualResultCalculator.ComputeResultado(
            salaryAfterTaxes: 2000m,
            totalDespesas: 1500m,
            investimentoCategoryValue: 300m);

        result.Should().Be(800m);
    }

    [Fact]
    public void ComputeResultado_MonthlySeries_ComputesElementWise()
    {
        var salaryAfterTaxes = MonthlySeries.FromMonthlyValues(Enumerable.Repeat(2000m, 12).ToArray());
        var totalDespesas = MonthlySeries.FromMonthlyValues(Enumerable.Repeat(1500m, 12).ToArray());
        var investimento = MonthlySeries.FromMonthlyValues(Enumerable.Repeat(300m, 12).ToArray());

        var result = AnnualResultCalculator.ComputeResultado(salaryAfterTaxes, totalDespesas, investimento);

        result[0].Should().Be(800m);
        result[11].Should().Be(800m);
    }
}
