using Financial.CashFlow.Domain.Entities;
using Financial.CashFlow.Domain.Enums;
using FluentAssertions;
using FluentAssertions.Execution;

namespace Financial.CashFlow.Domain.Tests;

public class IncomeTests
{
    private static IncomeSource Gleison => IncomeSource.Create("Gleison", IncomeGroup.Salary);
    private static IncomeSource Ariana => IncomeSource.Create("Ariana", IncomeGroup.Salary);
    private static IncomeSource Lottery => IncomeSource.Create("Lottery", IncomeGroup.NonReportable);
    private static IncomeSource DividendoJuros => IncomeSource.Create("DividendoJuros", IncomeGroup.DividendoJuros);
    private static Bank Barclays => Bank.Create("Barclays", roundUpEnabled: false);
    private static Bank Chase => Bank.Create("Chase", roundUpEnabled: true);
    private static Bank Trading212 => Bank.Create("Trading212", roundUpEnabled: true);

    [Fact]
    public void Create_AssignsAllFieldsAndANewId()
    {
        var date = new DateOnly(2026, 7, 25);
        var incomeSource = Gleison;
        var bank = Barclays;

        var income = Income.Create(date, incomeSource, 3200.00m, 2450.00m, bank);

        using (new AssertionScope())
        {
            income.Id.Should().NotBeEmpty();
            income.Date.Should().Be(date);
            income.IncomeSource.Should().Be(incomeSource);
            income.GrossValue.Should().Be(3200.00m);
            income.NetValue.Should().Be(2450.00m);
            income.Bank.Should().Be(bank);
        }
    }

    [Fact]
    public void Create_TwoIncomes_HaveDifferentIds()
    {
        var first = Income.Create(new DateOnly(2026, 7, 1), Lottery, null, 10m, Chase);
        var second = Income.Create(new DateOnly(2026, 7, 1), Lottery, null, 20m, Chase);

        first.Id.Should().NotBe(second.Id);
    }

    [Fact]
    public void Create_WithoutGrossValue_AllowsNull()
    {
        var income = Income.Create(new DateOnly(2026, 7, 1), DividendoJuros, null, 15.50m, Trading212);

        income.GrossValue.Should().BeNull();
    }

    [Fact]
    public void Create_WithGrossValueBelowNetValue_DoesNotThrow()
    {
        var act = () => Income.Create(new DateOnly(2026, 7, 1), Ariana, 100m, 150m, Chase);

        act.Should().NotThrow();
    }

    [Fact]
    public void Create_WithNegativeNetValue_Throws()
    {
        var act = () => Income.Create(new DateOnly(2026, 7, 1), Lottery, null, -1m, Chase);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_WithoutABank_DoesNotThrow()
    {
        var income = Income.Create(new DateOnly(2026, 7, 1), Lottery, null, 10m, null);

        income.Bank.Should().BeNull();
    }

    [Fact]
    public void Create_WithoutAnIncomeSource_Throws()
    {
        var act = () => Income.Create(new DateOnly(2026, 7, 1), null!, null, 10m, Chase);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_WithDescription_AssignsDescription()
    {
        var income = Income.Create(new DateOnly(2026, 7, 1), Lottery, null, 10m, Chase, "Chip ISA dividend");

        income.Description.Should().Be("Chip ISA dividend");
    }

    [Fact]
    public void Create_WithoutDescription_DescriptionIsNull()
    {
        var income = Income.Create(new DateOnly(2026, 7, 1), Lottery, null, 10m, Chase);

        income.Description.Should().BeNull();
    }

    [Fact]
    public void Create_WithWhitespaceDescription_NormalizesToNull()
    {
        var income = Income.Create(new DateOnly(2026, 7, 1), Lottery, null, 10m, Chase, "   ");

        income.Description.Should().BeNull();
    }

    [Fact]
    public void UpdateDetails_ReplacesAllFields()
    {
        var income = Income.Create(new DateOnly(2026, 7, 1), Gleison, 3000m, 2400m, Barclays);
        var newDate = new DateOnly(2026, 7, 2);
        var ariana = Ariana;
        var chase = Chase;

        income.UpdateDetails(newDate, ariana, 500m, 400m, chase);

        using (new AssertionScope())
        {
            income.Date.Should().Be(newDate);
            income.IncomeSource.Should().Be(ariana);
            income.GrossValue.Should().Be(500m);
            income.NetValue.Should().Be(400m);
            income.Bank.Should().Be(chase);
        }
    }

    [Fact]
    public void UpdateDetails_WithGrossValueBelowNetValue_DoesNotThrow()
    {
        var income = Income.Create(new DateOnly(2026, 7, 1), Gleison, 3000m, 2400m, Barclays);

        var act = () => income.UpdateDetails(income.Date, income.IncomeSource, 100m, 200m, income.Bank);

        act.Should().NotThrow();
    }

    [Fact]
    public void UpdateDetails_WithNegativeNetValue_Throws()
    {
        var income = Income.Create(new DateOnly(2026, 7, 1), Gleison, 3000m, 2400m, Barclays);

        var act = () => income.UpdateDetails(income.Date, income.IncomeSource, null, -5m, income.Bank);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void UpdateDetails_RemovingBank_SetsBankNull()
    {
        var income = Income.Create(new DateOnly(2026, 7, 1), Gleison, 3000m, 2400m, Barclays);

        income.UpdateDetails(income.Date, income.IncomeSource, income.GrossValue, income.NetValue, null);

        income.Bank.Should().BeNull();
    }

    [Fact]
    public void UpdateDetails_ReplacesDescription()
    {
        var income = Income.Create(new DateOnly(2026, 7, 1), Gleison, 3000m, 2400m, Barclays, "Original note");

        income.UpdateDetails(income.Date, income.IncomeSource, income.GrossValue, income.NetValue, income.Bank, "Updated note");

        income.Description.Should().Be("Updated note");
    }

    [Fact]
    public void Create_WithoutSplitToReserve_DefaultsToFalse()
    {
        var income = Income.Create(new DateOnly(2026, 7, 1), Ariana, 3200m, 2450m, Barclays);

        income.SplitToReserve.Should().BeFalse();
    }

    [Fact]
    public void Create_WithSplitToReserveTrue_AssignsTrue()
    {
        var income = Income.Create(new DateOnly(2026, 7, 1), Ariana, 3200m, 2450m, Barclays, splitToReserve: true);

        income.SplitToReserve.Should().BeTrue();
    }

    [Fact]
    public void UpdateDetails_TogglingSplitToReserve_ReplacesValue()
    {
        var income = Income.Create(new DateOnly(2026, 7, 1), Ariana, 3200m, 2450m, Barclays, splitToReserve: true);

        income.UpdateDetails(income.Date, income.IncomeSource, income.GrossValue, income.NetValue, income.Bank, income.Description, splitToReserve: false);

        income.SplitToReserve.Should().BeFalse();
    }
}
