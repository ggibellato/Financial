using ClosedXML.Excel;
using Financial.CashFlow.Domain.Enums;
using Financial.CashFlow.Infrastructure.Integrations.CashFlowSpreadsheetImport.Reporting;
using Financial.CashFlow.Infrastructure.Integrations.CashFlowSpreadsheetImport.SheetImporters;
using FluentAssertions;

namespace Financial.CashFlowSpreadsheetImport.Tests.SheetImporters;

public class MonthlyExpenseSheetImporterTests
{
    [Fact]
    public void Import_2017ShapedSheet_QuemThenMotivo_ParsesExpensesCorrectly()
    {
        using var workbook = new XLWorkbook();
        var sheet = workbook.AddWorksheet("Fev2017");
        // Header row: Dia, Quem, Motivo, Valor (2017 era - description in B, category in C)
        sheet.Cell(1, 1).Value = "Dia";
        sheet.Cell(1, 2).Value = "Quem";
        sheet.Cell(1, 3).Value = "Motivo";
        sheet.Cell(1, 4).Value = "Valor";

        sheet.Cell(2, 1).Value = 1;
        sheet.Cell(2, 2).Value = "Lidl UK";
        sheet.Cell(2, 3).Value = "Mercado";
        sheet.Cell(2, 4).Value = 71.04;

        sheet.Cell(3, 1).Value = 3;
        sheet.Cell(3, 2).Value = "Amazon Digital Video";
        sheet.Cell(3, 3).Value = "Extras";
        sheet.Cell(3, 4).Value = 9.99;
        sheet.Cell(3, 5).Value = "T";

        var report = new ImportReport();

        var expenses = MonthlyExpenseSheetImporter.Import(sheet, 2017, 2, report);

        expenses.Should().HaveCount(2);
        var first = expenses.Single(e => e.Description == "Lidl UK");
        first.Category.Should().Be(Category.Mercado);
        first.Value.Should().Be(71.04m);
        first.Date.Should().Be(new DateOnly(2017, 2, 1));
        first.PaymentSource.Should().Be(PaymentSource.Barclays);

        var second = expenses.Single(e => e.Description == "Amazon Digital Video");
        second.Category.Should().Be(Category.Extras);
        second.PaymentSource.Should().Be(PaymentSource.Trading212);
        report.RowIssues.Should().BeEmpty();
    }

    [Fact]
    public void Import_2026ShapedSheet_MotivoThenQuem_ParsesExpensesCorrectly()
    {
        using var workbook = new XLWorkbook();
        var sheet = workbook.AddWorksheet("Jul2026");
        // Header row: Dia, Motivo, Quem, Valor (2019+ era - description in B, category in C, same content order)
        sheet.Cell(1, 1).Value = "Dia";
        sheet.Cell(1, 2).Value = "Motivo";
        sheet.Cell(1, 3).Value = "Quem";
        sheet.Cell(1, 4).Value = "Valor";

        sheet.Cell(2, 1).Value = 1;
        sheet.Cell(2, 2).Value = "Chartered Society";
        sheet.Cell(2, 3).Value = "Ariana";
        sheet.Cell(2, 4).Value = 39.17;
        sheet.Cell(2, 5).Value = "C";

        var report = new ImportReport();

        var expenses = MonthlyExpenseSheetImporter.Import(sheet, 2026, 7, report);

        expenses.Should().ContainSingle();
        var expense = expenses.Single();
        expense.Description.Should().Be("Chartered Society");
        expense.Category.Should().Be(Category.Ariana);
        expense.PaymentSource.Should().Be(PaymentSource.Chase);
    }

    [Fact]
    public void Import_UnrecognizedCategory_SkipsExpenseAndFlagsRow()
    {
        using var workbook = new XLWorkbook();
        var sheet = workbook.AddWorksheet("Out2017");
        sheet.Cell(1, 1).Value = "Dia";
        sheet.Cell(1, 2).Value = "Quem";
        sheet.Cell(1, 3).Value = "Motivo";
        sheet.Cell(1, 4).Value = "Valor";

        sheet.Cell(2, 1).Value = 5;
        sheet.Cell(2, 2).Value = "Some Store";
        sheet.Cell(2, 3).Value = "TotallyUnknownCategory";
        sheet.Cell(2, 4).Value = 10.0;

        var report = new ImportReport();

        var expenses = MonthlyExpenseSheetImporter.Import(sheet, 2017, 10, report);

        expenses.Should().BeEmpty();
        report.RowIssues.Should().ContainSingle(i => i.RawValue == "TotallyUnknownCategory" && i.SheetName == "Out2017");
    }

    [Fact]
    public void Import_KnownTypoCasas_ResolvesToCasaCategory()
    {
        using var workbook = new XLWorkbook();
        var sheet = workbook.AddWorksheet("Out2017");
        sheet.Cell(1, 1).Value = "Dia";
        sheet.Cell(1, 2).Value = "Quem";
        sheet.Cell(1, 3).Value = "Motivo";
        sheet.Cell(1, 4).Value = "Valor";

        sheet.Cell(2, 1).Value = 5;
        sheet.Cell(2, 2).Value = "Some Store";
        sheet.Cell(2, 3).Value = "Casas";
        sheet.Cell(2, 4).Value = 10.0;

        var report = new ImportReport();

        var expenses = MonthlyExpenseSheetImporter.Import(sheet, 2017, 10, report);

        expenses.Should().ContainSingle().Which.Category.Should().Be(Category.Casa);
        report.RowIssues.Should().BeEmpty();
    }

    [Fact]
    public void Import_DayDoesNotExistInMonth_ClampsToLastValidDayAndFlagsRowWithoutDroppingTheExpense()
    {
        using var workbook = new XLWorkbook();
        var sheet = workbook.AddWorksheet("Fev2026");
        sheet.Cell(1, 1).Value = "Dia";
        sheet.Cell(1, 2).Value = "Motivo";
        sheet.Cell(1, 3).Value = "Quem";
        sheet.Cell(1, 4).Value = "Valor";

        // 2026 is not a leap year - February has only 28 days, but the sheet records a real
        // transaction under day 29 (a genuine data-entry slip in the source spreadsheet).
        sheet.Cell(2, 1).Value = 29;
        sheet.Cell(2, 2).Value = "Oxford Dental";
        sheet.Cell(2, 3).Value = "Ariana";
        sheet.Cell(2, 4).Value = 130.0;

        var report = new ImportReport();

        var expenses = MonthlyExpenseSheetImporter.Import(sheet, 2026, 2, report);

        expenses.Should().ContainSingle();
        expenses[0].Date.Should().Be(new DateOnly(2026, 2, 28));
        expenses[0].Value.Should().Be(130.0m);
        report.RowIssues.Should().ContainSingle(i => i.SheetName == "Fev2026" && i.RawValue == "29");
    }
}
