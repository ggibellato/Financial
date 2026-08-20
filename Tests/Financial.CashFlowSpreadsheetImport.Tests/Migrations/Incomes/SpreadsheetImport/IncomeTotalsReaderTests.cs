using ClosedXML.Excel;
using Financial.CashFlow.Infrastructure.Tools.CashFlowSpreadsheetImport.Migrations.Incomes.SpreadsheetImport;
using FluentAssertions;

namespace Financial.CashFlowSpreadsheetImport.Tests.Migrations.Incomes.SpreadsheetImport;

public class IncomeTotalsReaderTests : IDisposable
{
    /// <summary>Every test drives a fresh workbook; xUnit builds one instance per test, so they stay
    /// isolated exactly as the per-test `using var workbook` did.</summary>
    private readonly XLWorkbook _workbook;

    public IncomeTotalsReaderTests()
    {
        _workbook = new XLWorkbook();
    }

    public void Dispose() => _workbook.Dispose();

    [Fact]
    public void ReadTotals_Era4Layout_ReadsGrossAndNetForEachSource()
    {
        var sheet = _workbook.AddWorksheet("Jul2026");

        sheet.Cell(2, 10).Value = "Salario Gleison";
        sheet.Cell(2, 11).Value = 0.0;
        sheet.Cell(2, 12).Value = 0.0;
        sheet.Cell(3, 10).Value = "Salario Ariana";
        sheet.Cell(3, 11).Value = 2595.39;
        sheet.Cell(3, 12).Value = 1878.74;
        sheet.Cell(4, 10).Value = "Lottery";
        sheet.Cell(4, 11).Value = 0.0;
        sheet.Cell(5, 10).Value = "Dividendo/Juros";
        sheet.Cell(5, 11).Value = 361.24;
        sheet.Cell(6, 10).Value = "Dizimo";
        sheet.Cell(6, 11).Value = 223.998;

        var totals = IncomeTotalsReader.ReadTotals(sheet);

        totals.Should().HaveCount(4);
        totals.Should().Contain(t => t.Source == "Ariana" && t.GrossValue == 2595.39m && t.NetValue == 1878.74m);
        totals.Should().Contain(t => t.Source == "DividendoJuros" && t.GrossValue == null && t.NetValue == 361.24m);
        totals.Should().Contain(t => t.Source == "Gleison" && t.NetValue == 0m);
    }

    [Fact]
    public void ReadTotals_Era1Layout_LabelsInColumnKValuesInColumnL_NetOnly()
    {
        var sheet = _workbook.AddWorksheet("Jun2018");

        sheet.Cell(4, 11).Value = "Salario Gleison";
        sheet.Cell(4, 12).Value = 5381.25;
        sheet.Cell(5, 11).Value = "Salario Ariana";
        sheet.Cell(5, 12).Value = 1846.73;
        sheet.Cell(6, 11).Value = "Lotery";
        sheet.Cell(6, 12).Value = 0.0;
        sheet.Cell(7, 11).Value = "Dizimo";
        sheet.Cell(7, 12).Value = 722.798;

        var totals = IncomeTotalsReader.ReadTotals(sheet);

        var gleison = totals.Should().ContainSingle(t => t.Source == "Gleison").Which;
        gleison.GrossValue.Should().BeNull();
        gleison.NetValue.Should().Be(5381.25m);
        totals.Should().ContainSingle(t => t.Source == "Ariana").Which.NetValue.Should().Be(1846.73m);
    }

    [Fact]
    public void ReadTotals_NoMatchingLabelsButBareValueAtJ1_ReadsItAsGleisonsNetIncome()
    {
        var sheet = _workbook.AddWorksheet("Fev2017");
        sheet.Cell(1, 10).Value = 3744.97;

        var totals = IncomeTotalsReader.ReadTotals(sheet);

        var gleison = totals.Should().ContainSingle().Which;
        gleison.Source.Should().Be("Gleison");
        gleison.GrossValue.Should().BeNull();
        gleison.NetValue.Should().Be(3744.97m);
    }

    [Fact]
    public void ReadTotals_NoMatchingLabelsAndNoValueAtJ1_ReturnsEmpty()
    {
        var sheet = _workbook.AddWorksheet("Jan2017");

        var totals = IncomeTotalsReader.ReadTotals(sheet);

        totals.Should().BeEmpty();
    }

    [Fact]
    public void ReadTotals_NetColumnExceedsGrossColumn_ImportsGrossAsIs()
    {
        var sheet = _workbook.AddWorksheet("Fev2026");
        sheet.Cell(3, 10).Value = "Salario Ariana";
        sheet.Cell(3, 11).Value = 343.18;
        sheet.Cell(3, 12).Value = 642.18;

        var totals = IncomeTotalsReader.ReadTotals(sheet);

        var ariana = totals.Should().ContainSingle(t => t.Source == "Ariana").Which;
        ariana.GrossValue.Should().Be(343.18m);
        ariana.NetValue.Should().Be(642.18m);
    }

    [Fact]
    public void ReadTotals_TypoedLotteryLabel_StillResolvesToLotterySource()
    {
        var sheet = _workbook.AddWorksheet("Out2018");
        sheet.Cell(4, 10).Value = "Lotery";
        sheet.Cell(4, 11).Value = 12.5;

        var totals = IncomeTotalsReader.ReadTotals(sheet);

        totals.Should().ContainSingle().Which.Source.Should().Be("Lottery");
    }
}
