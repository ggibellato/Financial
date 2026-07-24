using Financial.CashFlow.Domain.Entities;
using Financial.CashFlow.Domain.Enums;
using FluentAssertions;

namespace Financial.CashFlow.Domain.Tests;

public class IncomeTests
{
    [Fact]
    public void Create_AssignsAllFieldsAndANewId()
    {
        var date = new DateOnly(2026, 7, 25);

        var income = Income.Create(date, IncomeSource.Gleison, 3200.00m, 2450.00m, "Barclays");

        income.Id.Should().NotBeEmpty();
        income.Date.Should().Be(date);
        income.IncomeSource.Should().Be(IncomeSource.Gleison);
        income.GrossValue.Should().Be(3200.00m);
        income.NetValue.Should().Be(2450.00m);
        income.Bank.Should().Be("Barclays");
    }

    [Fact]
    public void Create_TwoIncomes_HaveDifferentIds()
    {
        var first = Income.Create(new DateOnly(2026, 7, 1), IncomeSource.Lottery, null, 10m, "Chase");
        var second = Income.Create(new DateOnly(2026, 7, 1), IncomeSource.Lottery, null, 20m, "Chase");

        first.Id.Should().NotBe(second.Id);
    }

    [Fact]
    public void Create_WithoutGrossValue_AllowsNull()
    {
        var income = Income.Create(new DateOnly(2026, 7, 1), IncomeSource.DividendoJuros, null, 15.50m, "Trading212");

        income.GrossValue.Should().BeNull();
    }

    [Fact]
    public void Create_WithGrossValueBelowNetValue_Throws()
    {
        var act = () => Income.Create(new DateOnly(2026, 7, 1), IncomeSource.Ariana, 100m, 150m, "Chase");

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_WithGrossValueEqualToNetValue_DoesNotThrow()
    {
        var act = () => Income.Create(new DateOnly(2026, 7, 1), IncomeSource.Ariana, 100m, 100m, "Chase");

        act.Should().NotThrow();
    }

    [Fact]
    public void Create_WithNegativeNetValue_Throws()
    {
        var act = () => Income.Create(new DateOnly(2026, 7, 1), IncomeSource.Lottery, null, -1m, "Chase");

        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithoutABank_Throws(string? bank)
    {
        var act = () => Income.Create(new DateOnly(2026, 7, 1), IncomeSource.Lottery, null, 10m, bank!);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void UpdateDetails_ReplacesAllFields()
    {
        var income = Income.Create(new DateOnly(2026, 7, 1), IncomeSource.Gleison, 3000m, 2400m, "Barclays");
        var newDate = new DateOnly(2026, 7, 2);

        income.UpdateDetails(newDate, IncomeSource.Ariana, 500m, 400m, "Chase");

        income.Date.Should().Be(newDate);
        income.IncomeSource.Should().Be(IncomeSource.Ariana);
        income.GrossValue.Should().Be(500m);
        income.NetValue.Should().Be(400m);
        income.Bank.Should().Be("Chase");
    }

    [Fact]
    public void UpdateDetails_WithGrossValueBelowNetValue_Throws()
    {
        var income = Income.Create(new DateOnly(2026, 7, 1), IncomeSource.Gleison, 3000m, 2400m, "Barclays");

        var act = () => income.UpdateDetails(income.Date, income.IncomeSource, 100m, 200m, income.Bank);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void UpdateDetails_WithNegativeNetValue_Throws()
    {
        var income = Income.Create(new DateOnly(2026, 7, 1), IncomeSource.Gleison, 3000m, 2400m, "Barclays");

        var act = () => income.UpdateDetails(income.Date, income.IncomeSource, null, -5m, income.Bank);

        act.Should().Throw<ArgumentException>();
    }
}
