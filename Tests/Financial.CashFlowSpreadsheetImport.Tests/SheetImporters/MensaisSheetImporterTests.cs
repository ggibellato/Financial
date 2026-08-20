using ClosedXML.Excel;
using Financial.CashFlow.Domain.Enums;
using Financial.CashFlow.Infrastructure.Tools.CashFlowSpreadsheetImport.SheetImporters;
using FluentAssertions;

namespace Financial.CashFlowSpreadsheetImport.Tests.SheetImporters;

public class MensaisSheetImporterTests : IDisposable
{
    /// <summary>Every test drives a fresh workbook, worksheet; xUnit builds one instance per test, so they stay
    /// isolated exactly as the per-test `using var workbook` did.</summary>
    private readonly XLWorkbook _workbook;
    private readonly IXLWorksheet _sheet;

    public MensaisSheetImporterTests()
    {
        _workbook = new XLWorkbook();
        _sheet = _workbook.AddWorksheet("Mensais");
    }

    public void Dispose() => _workbook.Dispose();

    [Fact]
    public void Import_BrasilAndUkSections_CreatesOneBillPerRow()
    {
        _sheet.Cell(1, 2).Value = "Brasil";

        _sheet.Cell(2, 1).Value = 10;
        _sheet.Cell(2, 2).Value = "Aluguel";
        _sheet.Cell(2, 3).Value = 500.0;
        _sheet.Cell(2, 4).Value = "X";
        _sheet.Cell(2, 6).Value = "123456";
        _sheet.Cell(2, 7).Value = 1412.0;

        _sheet.Cell(3, 2).Value = "UK";

        _sheet.Cell(4, 1).Value = 5;
        _sheet.Cell(4, 2).Value = "Council Tax";
        _sheet.Cell(4, 3).Value = 150.0;
        _sheet.Cell(4, 4).Value = "A";

        var bills = MensaisSheetImporter.Import(_sheet);

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
        _sheet.Cell(1, 1).Value = 1;
        _sheet.Cell(1, 2).Value = "Orphan bill";
        _sheet.Cell(1, 3).Value = 10.0;

        var bills = MensaisSheetImporter.Import(_sheet);

        bills.Should().BeEmpty();
    }

    [Fact]
    public void Import_UnrecognizedStatusTag_ResolvesToUnset()
    {
        _sheet.Cell(1, 2).Value = "UK";
        _sheet.Cell(2, 1).Value = 1;
        _sheet.Cell(2, 2).Value = "Some bill";
        _sheet.Cell(2, 3).Value = 20.0;

        var bills = MensaisSheetImporter.Import(_sheet);

        bills.Should().ContainSingle().Which.Status.Should().Be(BillStatus.Unset);
    }
}
