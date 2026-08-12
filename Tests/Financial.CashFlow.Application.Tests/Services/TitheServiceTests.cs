using Financial.CashFlow.Application.Interfaces;
using Financial.CashFlow.Application.Services;
using Financial.CashFlow.Application.Tests.TestHelpers;
using Financial.CashFlow.Domain.Entities;
using Financial.CashFlow.Domain.Enums;
using Category = Financial.CashFlow.Domain.Enums.Category;
using FluentAssertions;

namespace Financial.CashFlow.Application.Tests.Services;

public class TitheServiceTests
{
    private static readonly Bank Barclays = Bank.Create("Barclays", roundUpEnabled: false);
    private static readonly Bank Trading212 = Bank.Create("Trading212", roundUpEnabled: true);
    private static readonly Bank Chase = Bank.Create("Chase", roundUpEnabled: true);

    private static IncomeSource Source(string name) => IncomeSource.Create(name, IncomeGroup.NonReportable);

    [Fact]
    public void Constructor_WithNullRepository_Throws()
    {
        Action act = () => new TitheService(null!);
        act.Should().Throw<ArgumentNullException>().WithParameterName("repository");
    }

    [Fact]
    public void GetTitheSummary_CalculatesTenPercentOfMonthlyNetIncomeAcrossSources()
    {
        var repository = new StubCashFlowRepository();
        repository.Incomes.Add(Income.Create(new DateOnly(2026, 7, 1), Source("Gleison"), 3200m, 2450m, Barclays));
        repository.Incomes.Add(Income.Create(new DateOnly(2026, 7, 8), Source("Ariana"), null, 400m, Chase));
        repository.Incomes.Add(Income.Create(new DateOnly(2026, 7, 15), Source("DividendoJuros"), null, 150m, Trading212));
        var service = new TitheService(repository);

        var result = service.GetTitheSummary(2026, 7);

        result.CalculatedTithe.Should().Be(300m);
    }

    [Fact]
    public void GetTitheSummary_SubtractsDizimoExpensesFromCalculatedTithe()
    {
        var repository = new StubCashFlowRepository();
        repository.Incomes.Add(Income.Create(new DateOnly(2026, 7, 1), Source("Gleison"), null, 3000m, Barclays));
        repository.Expenses.Add(Expense.Create(new DateOnly(2026, 7, 10), "Tithe payment", 200m, Category.Dizimo, Barclays, null));
        var service = new TitheService(repository);

        var result = service.GetTitheSummary(2026, 7);

        result.CalculatedTithe.Should().Be(300m);
        result.TitheBalance.Should().Be(100m);
    }

    [Fact]
    public void GetTitheSummary_DizimoExceedingCalculatedTithe_ReturnsNegativeBalanceWithoutError()
    {
        var repository = new StubCashFlowRepository();
        repository.Incomes.Add(Income.Create(new DateOnly(2026, 7, 1), Source("Gleison"), null, 1000m, Barclays));
        repository.Expenses.Add(Expense.Create(new DateOnly(2026, 7, 10), "Tithe payment", 200m, Category.Dizimo, Barclays, null));
        var service = new TitheService(repository);

        var result = service.GetTitheSummary(2026, 7);

        result.CalculatedTithe.Should().Be(100m);
        result.TitheBalance.Should().Be(-100m);
    }

    [Fact]
    public void GetTitheSummary_NonDizimoExpenses_AreIgnored()
    {
        var repository = new StubCashFlowRepository();
        repository.Incomes.Add(Income.Create(new DateOnly(2026, 7, 1), Source("Gleison"), null, 1000m, Barclays));
        repository.Expenses.Add(Expense.Create(new DateOnly(2026, 7, 5), "Groceries", 50m, Category.Mercado, Barclays, null));
        var service = new TitheService(repository);

        var result = service.GetTitheSummary(2026, 7);

        result.TitheBalance.Should().Be(100m);
    }

    [Fact]
    public void GetTitheSummary_ExcludesIncomeAndExpensesOutsideSelectedMonth()
    {
        var repository = new StubCashFlowRepository();
        repository.Incomes.Add(Income.Create(new DateOnly(2026, 7, 1), Source("Gleison"), null, 1000m, Barclays));
        repository.Incomes.Add(Income.Create(new DateOnly(2026, 8, 1), Source("Gleison"), null, 5000m, Barclays));
        repository.Expenses.Add(Expense.Create(new DateOnly(2026, 7, 5), "July tithe", 50m, Category.Dizimo, Barclays, null));
        repository.Expenses.Add(Expense.Create(new DateOnly(2026, 8, 5), "August tithe", 500m, Category.Dizimo, Barclays, null));
        var service = new TitheService(repository);

        var result = service.GetTitheSummary(2026, 7);

        result.CalculatedTithe.Should().Be(100m);
        result.TitheBalance.Should().Be(50m);
    }

    [Fact]
    public void GetTitheSummary_NoIncomeNoExpenses_ReturnsZeros()
    {
        var service = new TitheService(new StubCashFlowRepository());

        var result = service.GetTitheSummary(2026, 7);

        result.CalculatedTithe.Should().Be(0m);
        result.TitheBalance.Should().Be(0m);
    }

}
