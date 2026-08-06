using ClosedXML.Excel;
using Financial.CashFlow.Domain.Entities;
using Financial.CashFlow.Infrastructure.Integrations.CashFlowSpreadsheetImport.Migrations.Incomes;
using FluentAssertions;

namespace Financial.CashFlowSpreadsheetImport.Tests.Migrations.Incomes;

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
        data.AddIncome(Income.Create(new DateOnly(2026, 7, 1), "Lottery", null, 10m, "Chase"));
        data.AddIncome(Income.Create(new DateOnly(2026, 7, 2), "Ariana", null, 400m, "Barclays"));

        var summary = IncomeMigrator.Migrate(data);

        summary.IncomeCount.Should().Be(2);
    }

    [Fact]
    public void Migrate_CalledTwice_ProducesTheSameResult()
    {
        var data = CashFlowData.Create();
        data.AddIncome(Income.Create(new DateOnly(2026, 7, 1), "Lottery", null, 10m, "Chase"));

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

    [Fact]
    public void Migrate_WithWorkbook_BackfillsEntriesAndReportsImportedCount()
    {
        using var workbook = new XLWorkbook();
        var sheet = workbook.AddWorksheet("Jul2026");
        sheet.Cell(3, 10).Value = "Salario Ariana";
        sheet.Cell(3, 11).Value = 2595.39;
        sheet.Cell(3, 12).Value = 1878.74;
        var data = CashFlowData.Create();

        var summary = IncomeMigrator.Migrate(data, workbook);

        summary.EntriesImportedCount.Should().Be(1);
        summary.IncomeCount.Should().Be(1);
        data.Incomes.Should().ContainSingle(i => i.IncomeSource == "Ariana" && i.Bank == "Barclays");
    }

    [Fact]
    public void Migrate_WithoutWorkbook_ReportsZeroImportedEntries()
    {
        var data = CashFlowData.Create();

        var summary = IncomeMigrator.Migrate(data);

        summary.EntriesImportedCount.Should().Be(0);
    }
}
