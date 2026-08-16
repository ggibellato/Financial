using ClosedXML.Excel;
using Financial.CashFlow.Domain.Entities;
using Financial.CashFlow.Domain.Enums;
using Financial.CashFlow.Infrastructure.Tools.CashFlowSpreadsheetImport.Migrations.Incomes;
using FluentAssertions;

namespace Financial.CashFlowSpreadsheetImport.Tests.Migrations.Incomes;

public class IncomeBackfillImporterTests
{
    // The importer resolves each row's source/bank name against the seeded lists (F01) - in a real
    // run these are already carried over from the prior data file by the time this importer runs
    // (see Program.cs), so tests seed the same fixed set here to match that precondition.
    private static CashFlowData SeededData()
    {
        var data = CashFlowData.Create();
        data.AddBank(Bank.Create("Barclays", roundUpEnabled: false));
        data.AddIncomeSource(IncomeSource.Create("Gleison", IncomeGroup.Salary));
        data.AddIncomeSource(IncomeSource.Create("Ariana", IncomeGroup.Salary));
        data.AddIncomeSource(IncomeSource.Create("Lottery", IncomeGroup.NonReportable));
        data.AddIncomeSource(IncomeSource.Create("DividendoJuros", IncomeGroup.DividendoJuros));
        return data;
    }

    [Fact]
    public void Import_TwoMonthlySheets_CreatesOneEntryPerNonZeroSourcePerMonthDatedFirstOfMonth()
    {
        using var workbook = new XLWorkbook();
        AddMonthlySheet(workbook, "Jul2026", ("Salario Gleison", 0.0, 0.0), ("Salario Ariana", 2595.39, 1878.74), ("Lottery", 0.0, null), ("Dividendo/Juros", 361.24, null));
        AddMonthlySheet(workbook, "Ago2026", ("Salario Gleison", 0.0, 0.0), ("Salario Ariana", 0.0, 0.0), ("Lottery", 0.0, null), ("Dividendo/Juros", 0.0, null));
        var data = SeededData();

        var importedCount = IncomeBackfillImporter.Import(data, workbook);

        importedCount.Should().Be(2);
        data.Incomes.Should().HaveCount(2);
        data.Incomes.Should().Contain(i =>
            i.Date == new DateOnly(2026, 7, 1) && i.IncomeSource.Name == "Ariana" &&
            i.GrossValue == 2595.39m && i.NetValue == 1878.74m && i.Bank!.Name == "Barclays");
        data.Incomes.Should().Contain(i =>
            i.Date == new DateOnly(2026, 7, 1) && i.IncomeSource.Name == "DividendoJuros" &&
            i.GrossValue == null && i.NetValue == 361.24m && i.Bank!.Name == "Barclays");
    }

    [Fact]
    public void Import_ZeroTotalForASource_CreatesNoEntryForThatSource()
    {
        using var workbook = new XLWorkbook();
        AddMonthlySheet(workbook, "Ago2026", ("Salario Gleison", 0.0, 0.0));
        var data = SeededData();

        IncomeBackfillImporter.Import(data, workbook);

        data.Incomes.Should().BeEmpty();
    }

    [Fact]
    public void Import_SheetNameNotAMonthlyPattern_IsIgnored()
    {
        using var workbook = new XLWorkbook();
        var sheet = workbook.AddWorksheet("Resumo2026");
        sheet.Cell(2, 10).Value = "Salario Gleison";
        sheet.Cell(2, 11).Value = 100.0;
        var data = SeededData();

        var importedCount = IncomeBackfillImporter.Import(data, workbook);

        importedCount.Should().Be(0);
        data.Incomes.Should().BeEmpty();
    }

    [Fact]
    public void Import_CalledTwiceOnTheSameWorkbook_DoesNotDuplicateEntries()
    {
        using var workbook = new XLWorkbook();
        AddMonthlySheet(workbook, "Jul2026", ("Salario Ariana", 2595.39, 1878.74));
        var data = SeededData();

        IncomeBackfillImporter.Import(data, workbook);
        var secondImportedCount = IncomeBackfillImporter.Import(data, workbook);

        secondImportedCount.Should().Be(0);
        data.Incomes.Should().HaveCount(1);
    }

    private static void AddMonthlySheet(XLWorkbook workbook, string sheetName, params (string Label, double First, double? Second)[] rows)
    {
        var sheet = workbook.AddWorksheet(sheetName);
        var row = 2;
        foreach (var (label, first, second) in rows)
        {
            sheet.Cell(row, 10).Value = label;
            sheet.Cell(row, 11).Value = first;
            if (second is not null)
            {
                sheet.Cell(row, 12).Value = second.Value;
            }

            row++;
        }
    }
}
