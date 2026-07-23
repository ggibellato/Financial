using ClosedXML.Excel;
using Financial.CashFlow.Domain.Enums;
using Financial.CashFlow.Infrastructure.Integrations.CashFlowSpreadsheetImport.SheetImporters;
using FluentAssertions;

namespace Financial.CashFlowSpreadsheetImport.Tests.SheetImporters;

public class MensaisSheetImporterTests
{
    [Fact]
    public void Import_BrasilAndUkSections_CreatesOneBillPerRow()
    {
        using var workbook = new XLWorkbook();
        var sheet = workbook.AddWorksheet("Mensais");

        sheet.Cell(1, 2).Value = "Brasil";

        sheet.Cell(2, 1).Value = 10;
        sheet.Cell(2, 2).Value = "Aluguel";
        sheet.Cell(2, 3).Value = 500.0;
        sheet.Cell(2, 4).Value = "X";
        sheet.Cell(2, 6).Value = "123456";
        sheet.Cell(2, 7).Value = 1412.0;

        sheet.Cell(3, 2).Value = "UK";

        sheet.Cell(4, 1).Value = 5;
        sheet.Cell(4, 2).Value = "Council Tax";
        sheet.Cell(4, 3).Value = 150.0;
        sheet.Cell(4, 4).Value = "A";

        var bills = MensaisSheetImporter.Import(sheet);

        bills.Should().HaveCount(2);

        var brasilBill = bills.Single(b => b.Description == "Aluguel");
        brasilBill.Area.Should().Be(Area.Brasil);
        brasilBill.DueDay.Should().Be(10);
        brasilBill.Value.Should().Be(500.0m);
        brasilBill.NitNumber.Should().Be("123456");
        brasilBill.MinimumWageValue.Should().Be(1412.0m);
        brasilBill.Status.Should().Be(BillStatus.Paid);

        var ukBill = bills.Single(b => b.Description == "Council Tax");
        ukBill.Area.Should().Be(Area.UK);
        ukBill.NitNumber.Should().BeNull();
        ukBill.MinimumWageValue.Should().BeNull();
        ukBill.Status.Should().Be(BillStatus.Scheduled);
    }

    [Fact]
    public void Import_RowsBeforeAnyAreaLabel_AreIgnored()
    {
        using var workbook = new XLWorkbook();
        var sheet = workbook.AddWorksheet("Mensais");

        sheet.Cell(1, 1).Value = 1;
        sheet.Cell(1, 2).Value = "Orphan bill";
        sheet.Cell(1, 3).Value = 10.0;

        var bills = MensaisSheetImporter.Import(sheet);

        bills.Should().BeEmpty();
    }

    [Fact]
    public void Import_UnrecognizedStatusTag_ResolvesToUnset()
    {
        using var workbook = new XLWorkbook();
        var sheet = workbook.AddWorksheet("Mensais");

        sheet.Cell(1, 2).Value = "UK";
        sheet.Cell(2, 1).Value = 1;
        sheet.Cell(2, 2).Value = "Some bill";
        sheet.Cell(2, 3).Value = 20.0;

        var bills = MensaisSheetImporter.Import(sheet);

        bills.Should().ContainSingle().Which.Status.Should().Be(BillStatus.Unset);
    }
}
