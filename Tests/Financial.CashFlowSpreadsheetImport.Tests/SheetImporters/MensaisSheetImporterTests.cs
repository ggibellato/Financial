using ClosedXML.Excel;
using Financial.CashFlow.Domain.Enums;
using Financial.CashFlow.Infrastructure.Integrations.CashFlowSpreadsheetImport.SheetImporters;
using FluentAssertions;

namespace Financial.CashFlowSpreadsheetImport.Tests.SheetImporters;

public class MensaisSheetImporterTests
{
    [Fact]
    public void Import_BrasilAndUkSections_CreatesOneTemplateAndInstancePerRow()
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

        var (templates, instances) = MensaisSheetImporter.Import(sheet, 2026, 7);

        templates.Should().HaveCount(2);
        instances.Should().HaveCount(2);

        var brasilTemplate = templates.Single(t => t.Description == "Aluguel");
        brasilTemplate.Area.Should().Be(Area.Brasil);
        brasilTemplate.DueDay.Should().Be(10);
        brasilTemplate.Value.Should().Be(500.0m);
        brasilTemplate.NitNumber.Should().Be("123456");
        brasilTemplate.MinimumWageValue.Should().Be(1412.0m);

        var brasilInstance = instances.Single(i => i.TemplateId == brasilTemplate.Id);
        brasilInstance.Year.Should().Be(2026);
        brasilInstance.Month.Should().Be(7);
        brasilInstance.Status.Should().Be(BillStatus.Paid);

        var ukTemplate = templates.Single(t => t.Description == "Council Tax");
        ukTemplate.Area.Should().Be(Area.UK);
        ukTemplate.NitNumber.Should().BeNull();
        ukTemplate.MinimumWageValue.Should().BeNull();

        var ukInstance = instances.Single(i => i.TemplateId == ukTemplate.Id);
        ukInstance.Status.Should().Be(BillStatus.Scheduled);
    }

    [Fact]
    public void Import_RowsBeforeAnyAreaLabel_AreIgnored()
    {
        using var workbook = new XLWorkbook();
        var sheet = workbook.AddWorksheet("Mensais");

        sheet.Cell(1, 1).Value = 1;
        sheet.Cell(1, 2).Value = "Orphan bill";
        sheet.Cell(1, 3).Value = 10.0;

        var (templates, instances) = MensaisSheetImporter.Import(sheet, 2026, 7);

        templates.Should().BeEmpty();
        instances.Should().BeEmpty();
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

        var (_, instances) = MensaisSheetImporter.Import(sheet, 2026, 7);

        instances.Should().ContainSingle().Which.Status.Should().Be(BillStatus.Unset);
    }
}
