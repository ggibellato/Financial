using ClosedXML.Excel;
using Financial.CashFlow.Domain.Enums;
using Financial.CashFlow.Infrastructure.Integrations.CashFlowSpreadsheetImport.Migrations.Incomes.SpreadsheetImport;
using FluentAssertions;

namespace Financial.CashFlowSpreadsheetImport.Tests.Migrations.Incomes.SpreadsheetImport;

public class IncomeTotalsReaderTests
{
    [Fact]
    public void ReadTotals_Era4Layout_ReadsGrossAndNetForEachSource()
    {
        using var workbook = new XLWorkbook();
        var sheet = workbook.AddWorksheet("Jul2026");

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
        totals.Should().Contain(t => t.Source == IncomeSource.Ariana && t.GrossValue == 2595.39m && t.NetValue == 1878.74m);
        totals.Should().Contain(t => t.Source == IncomeSource.DividendoJuros && t.GrossValue == null && t.NetValue == 361.24m);
        totals.Should().Contain(t => t.Source == IncomeSource.Gleison && t.NetValue == 0m);
    }

    [Fact]
    public void ReadTotals_Era1Layout_LabelsInColumnKValuesInColumnL_NetOnly()
    {
        using var workbook = new XLWorkbook();
        var sheet = workbook.AddWorksheet("Jun2018");

        sheet.Cell(4, 11).Value = "Salario Gleison";
        sheet.Cell(4, 12).Value = 5381.25;
        sheet.Cell(5, 11).Value = "Salario Ariana";
        sheet.Cell(5, 12).Value = 1846.73;
        sheet.Cell(6, 11).Value = "Lotery";
        sheet.Cell(6, 12).Value = 0.0;
        sheet.Cell(7, 11).Value = "Dizimo";
        sheet.Cell(7, 12).Value = 722.798;

        var totals = IncomeTotalsReader.ReadTotals(sheet);

        var gleison = totals.Should().ContainSingle(t => t.Source == IncomeSource.Gleison).Which;
        gleison.GrossValue.Should().BeNull();
        gleison.NetValue.Should().Be(5381.25m);
        totals.Should().ContainSingle(t => t.Source == IncomeSource.Ariana).Which.NetValue.Should().Be(1846.73m);
    }

    [Fact]
    public void ReadTotals_NoMatchingLabels_ReturnsEmpty()
    {
        using var workbook = new XLWorkbook();
        var sheet = workbook.AddWorksheet("Jan2017");
        sheet.Cell(1, 10).Value = 3744.97;

        var totals = IncomeTotalsReader.ReadTotals(sheet);

        totals.Should().BeEmpty();
    }

    [Fact]
    public void ReadTotals_NetColumnExceedsGrossColumn_DropsGrossRatherThanViolatingTheDomainInvariant()
    {
        using var workbook = new XLWorkbook();
        var sheet = workbook.AddWorksheet("Fev2026");
        sheet.Cell(3, 10).Value = "Salario Ariana";
        sheet.Cell(3, 11).Value = 343.18;
        sheet.Cell(3, 12).Value = 642.18;

        var totals = IncomeTotalsReader.ReadTotals(sheet);

        var ariana = totals.Should().ContainSingle(t => t.Source == IncomeSource.Ariana).Which;
        ariana.GrossValue.Should().BeNull();
        ariana.NetValue.Should().Be(642.18m);
    }

    [Fact]
    public void ReadTotals_TypoedLotteryLabel_StillResolvesToLotterySource()
    {
        using var workbook = new XLWorkbook();
        var sheet = workbook.AddWorksheet("Out2018");
        sheet.Cell(4, 10).Value = "Lotery";
        sheet.Cell(4, 11).Value = 12.5;

        var totals = IncomeTotalsReader.ReadTotals(sheet);

        totals.Should().ContainSingle().Which.Source.Should().Be(IncomeSource.Lottery);
    }
}
