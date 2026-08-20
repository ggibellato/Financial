using ClosedXML.Excel;
using Financial.CashFlow.Infrastructure.Tools.CashFlowSpreadsheetImport.Parsing;
using FluentAssertions;

namespace Financial.CashFlowSpreadsheetImport.Tests.Parsing;

public class NumericCellReaderTests : IDisposable
{
    /// <summary>Every test drives a fresh workbook, worksheet; xUnit builds one instance per test, so they stay
    /// isolated exactly as the per-test `using var workbook` did.</summary>
    private readonly XLWorkbook _workbook;
    private readonly IXLWorksheet _sheet;

    public NumericCellReaderTests()
    {
        _workbook = new XLWorkbook();
        _sheet = _workbook.AddWorksheet("Sheet1");
    }

    public void Dispose() => _workbook.Dispose();

    [Fact]
    public void TryRead_GenuineExcelNumber_ReadsDirectly()
    {
        _sheet.Cell(1, 1).Value = 130.0;

        var result = NumericCellReader.TryRead(_sheet.Cell(1, 1));

        result.Should().Be(130.0m);
    }

    [Fact]
    public void TryRead_TextWithCommaDecimalSeparator_NormalizesToPeriodAndParsesCorrectly()
    {
        _sheet.Cell(1, 1).Value = "17,28";

        var result = NumericCellReader.TryRead(_sheet.Cell(1, 1));

        result.Should().Be(17.28m);
    }

    [Fact]
    public void TryRead_TextWithPeriodDecimalSeparator_ParsesDirectly()
    {
        _sheet.Cell(1, 1).Value = "17.28";

        var result = NumericCellReader.TryRead(_sheet.Cell(1, 1));

        result.Should().Be(17.28m);
    }

    [Fact]
    public void TryRead_EmptyCell_ReturnsNull()
    {
        var result = NumericCellReader.TryRead(_sheet.Cell(1, 1));

        result.Should().BeNull();
    }

    [Fact]
    public void TryRead_NonNumericText_ReturnsNull()
    {
        _sheet.Cell(1, 1).Value = "not a number";

        var result = NumericCellReader.TryRead(_sheet.Cell(1, 1));

        result.Should().BeNull();
    }
}
