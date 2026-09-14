using System;
using Financial.Investment.Domain.Rules;
using FluentAssertions;

namespace Financial.Investment.Domain.Tests;

public class TaxYearCalculatorTests
{
    [Fact]
    public void Calculate_BrlCurrency_UsesPlainCalendarYear()
    {
        var taxYear = TaxYearCalculator.Calculate(new DateTime(2026, 3, 15), "BRL");

        taxYear.Should().Be("2026");
    }

    [Fact]
    public void Calculate_NonBrlCurrency_OnApril6th_BelongsToThatYearsOpeningTaxYear()
    {
        var taxYear = TaxYearCalculator.Calculate(new DateTime(2026, 4, 6), "GBP");

        taxYear.Should().Be("2026/27");
    }

    [Fact]
    public void Calculate_NonBrlCurrency_OnApril5th_BelongsToThePreviousTaxYear()
    {
        var taxYear = TaxYearCalculator.Calculate(new DateTime(2026, 4, 5), "GBP");

        taxYear.Should().Be("2025/26");
    }

    [Fact]
    public void Calculate_NonBrlCurrency_MidYear_BelongsToTheYearThatOpenedInApril()
    {
        var taxYear = TaxYearCalculator.Calculate(new DateTime(2026, 12, 31), "GBP");

        taxYear.Should().Be("2026/27");
    }

    [Fact]
    public void Calculate_NonBrlCurrency_EarlyInYear_BelongsToThePreviousAprilToAprilTaxYear()
    {
        var taxYear = TaxYearCalculator.Calculate(new DateTime(2026, 1, 1), "GBP");

        taxYear.Should().Be("2025/26");
    }
}
