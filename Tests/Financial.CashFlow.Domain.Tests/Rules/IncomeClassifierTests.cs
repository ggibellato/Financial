using Financial.CashFlow.Domain.Enums;
using Financial.CashFlow.Domain.Rules;
using FluentAssertions;

namespace Financial.CashFlow.Domain.Tests.Rules;

public class IncomeClassifierTests
{
    [Theory]
    [InlineData(IncomeSource.Gleison, IncomeGroup.Salary)]
    [InlineData(IncomeSource.Ariana, IncomeGroup.Salary)]
    [InlineData(IncomeSource.DividendoJuros, IncomeGroup.DividendoJuros)]
    public void Classify_ReturnsExpectedGroup(IncomeSource incomeSource, IncomeGroup expected)
    {
        IncomeClassifier.Classify(incomeSource).Should().Be(expected);
    }

    [Fact]
    public void Classify_Lottery_ReturnsNonReportable()
    {
        IncomeClassifier.Classify(IncomeSource.Lottery).Should().Be(IncomeGroup.NonReportable);
    }
}
