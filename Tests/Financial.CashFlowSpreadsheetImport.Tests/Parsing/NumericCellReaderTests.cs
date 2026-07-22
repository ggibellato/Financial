using ClosedXML.Excel;
using Financial.CashFlow.Infrastructure.Integrations.CashFlowSpreadsheetImport.Parsing;
using FluentAssertions;

namespace Financial.CashFlowSpreadsheetImport.Tests.Parsing;

public class NumericCellReaderTests
{
    [Fact]
    public void TryRead_GenuineExcelNumber_ReadsDirectly()
    {
        using var workbook = new XLWorkbook();
        var sheet = workbook.AddWorksheet("Sheet1");
        sheet.Cell(1, 1).Value = 130.0;

        var result = NumericCellReader.TryRead(sheet.Cell(1, 1));

        result.Should().Be(130.0m);
    }

    [Fact]
    public void TryRead_TextWithCommaDecimalSeparator_NormalizesToPeriodAndParsesCorrectly()
    {
        using var workbook = new XLWorkbook();
        var sheet = workbook.AddWorksheet("Sheet1");
        sheet.Cell(1, 1).Value = "17,28";

        var result = NumericCellReader.TryRead(sheet.Cell(1, 1));

        result.Should().Be(17.28m);
    }

    [Fact]
    public void TryRead_TextWithPeriodDecimalSeparator_ParsesDirectly()
    {
        using var workbook = new XLWorkbook();
        var sheet = workbook.AddWorksheet("Sheet1");
        sheet.Cell(1, 1).Value = "17.28";

        var result = NumericCellReader.TryRead(sheet.Cell(1, 1));

        result.Should().Be(17.28m);
    }

    [Fact]
    public void TryRead_EmptyCell_ReturnsNull()
    {
        using var workbook = new XLWorkbook();
        var sheet = workbook.AddWorksheet("Sheet1");

        var result = NumericCellReader.TryRead(sheet.Cell(1, 1));

        result.Should().BeNull();
    }

    [Fact]
    public void TryRead_NonNumericText_ReturnsNull()
    {
        using var workbook = new XLWorkbook();
        var sheet = workbook.AddWorksheet("Sheet1");
        sheet.Cell(1, 1).Value = "not a number";

        var result = NumericCellReader.TryRead(sheet.Cell(1, 1));

        result.Should().BeNull();
    }
}
