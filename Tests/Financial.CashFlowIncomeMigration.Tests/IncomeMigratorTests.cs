using Financial.CashFlow.Domain.Entities;
using Financial.CashFlow.Domain.Enums;
using Financial.CashFlow.Infrastructure.Integrations.CashFlowIncomeMigration;
using FluentAssertions;

namespace Financial.CashFlowIncomeMigration.Tests;

public class IncomeMigratorTests
{
    [Fact]
    public void Migrate_OnEmptyData_ReportsEmptyIncomesCollection()
    {
        var data = CashFlowData.Create();

        var summary = IncomeMigrator.Migrate(data);

        summary.IncomeCount.Should().Be(0);
        data.Incomes.Should().BeEmpty();
    }

    [Fact]
    public void Migrate_WithExistingIncomes_ReportsTheCorrectCount()
    {
        var data = CashFlowData.Create();
        data.AddIncome(Income.Create(new DateOnly(2026, 7, 1), IncomeSource.Lottery, null, 10m, "Chase"));
        data.AddIncome(Income.Create(new DateOnly(2026, 7, 2), IncomeSource.Ariana, null, 400m, "Barclays"));

        var summary = IncomeMigrator.Migrate(data);

        summary.IncomeCount.Should().Be(2);
    }

    [Fact]
    public void Migrate_CalledTwice_ProducesTheSameResult()
    {
        var data = CashFlowData.Create();
        data.AddIncome(Income.Create(new DateOnly(2026, 7, 1), IncomeSource.Lottery, null, 10m, "Chase"));

        var first = IncomeMigrator.Migrate(data);
        var second = IncomeMigrator.Migrate(data);

        first.IncomeCount.Should().Be(second.IncomeCount);
        data.Incomes.Should().HaveCount(1);
    }

    [Fact]
    public void Migrate_WithNullData_Throws()
    {
        var act = () => IncomeMigrator.Migrate(null!);

        act.Should().Throw<ArgumentNullException>();
    }
}
