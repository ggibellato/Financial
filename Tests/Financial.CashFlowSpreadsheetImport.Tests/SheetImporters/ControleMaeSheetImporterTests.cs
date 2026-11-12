using ClosedXML.Excel;
using Financial.CashFlow.Domain.Entities;
using Financial.CashFlow.Domain.Enums;
using Financial.Shared.Abstractions.Currencies;
using Financial.CashFlow.Infrastructure.Tools.CashFlowSpreadsheetImport.Reporting;
using Financial.CashFlow.Infrastructure.Tools.CashFlowSpreadsheetImport.SheetImporters;
using Financial.TestUtilities;
using FluentAssertions;

namespace Financial.CashFlowSpreadsheetImport.Tests.SheetImporters;

[Trait("Category", "Unit")]
public class ControleMaeSheetImporterTests : IDisposable
{
    private readonly XLWorkbook _workbook;
    private readonly IXLWorksheet _sheet;
    private readonly ImportReport _report;
    private readonly Account _mae = TestAccounts.Settlement("Mãe", [Currency.BRL, Currency.GBP], "Mãe");

    public ControleMaeSheetImporterTests()
    {
        _workbook = new XLWorkbook();
        _sheet = _workbook.AddWorksheet("Controle mae");
        _report = new ImportReport();
    }

    public void Dispose() => _workbook.Dispose();

    [Fact]
    public void Import_FullDdMmYyyyDate_ParsesDateAndValues()
    {
        _sheet.Cell(1, 1).Value = "Deposito feito na conta do Gabriel em 28/05/2019";
        _sheet.Cell(1, 2).Value = 300.0;
        _sheet.Cell(1, 3).Value = 60.0;

        var entries = ControleMaeSheetImporter.Import(_sheet, _mae, _report, TestClock.Today);

        entries.Should().ContainSingle();
        var entry = entries[0];
        entry.Date.Should().Be(new DateOnly(2019, 5, 28));
        entry.Account.Should().BeSameAs(_mae);
        entry.PaidBy.Should().Be(SettlementPayer.You);
        entry.Currency.Should().Be(Currency.BRL);
        entry.Amount.Should().Be(300.0m);
        entry.ConvertedValue.Should().Be(60.0m);
        _report.RowIssues.Should().BeEmpty();
    }

    [Fact]
    public void Import_MonthYearOnlyDate_ParsesToFirstOfMonth()
    {
        _sheet.Cell(1, 1).Value = "Feito acerto em Dez/2018, seguro pago ate Dez/2018";
        _sheet.Cell(1, 2).Value = 186.0;

        var entries = ControleMaeSheetImporter.Import(_sheet, _mae, _report, TestClock.Today);

        entries.Should().ContainSingle();
        entries[0].Date.Should().Be(new DateOnly(2018, 12, 1));
        entries[0].Amount.Should().Be(186.0m);
        entries[0].ConvertedValue.Should().BeNull();
    }

    [Fact]
    public void Import_ColumnCHoldsNonNumericNote_IsTreatedAsNoiseNotAValue()
    {
        _sheet.Cell(1, 1).Value = "Acerto feito em 01/03/2020 - Tudo certo ate Dez/2019";
        _sheet.Cell(1, 2).Value = 186.0;
        _sheet.Cell(1, 3).Value = "62 por mes";

        var entries = ControleMaeSheetImporter.Import(_sheet, _mae, _report, TestClock.Today);

        entries.Should().ContainSingle();
        entries[0].ConvertedValue.Should().BeNull();
    }

    [Fact]
    public void Import_SeparatorRow_IsSkippedEntirely()
    {
        _sheet.Cell(1, 1).Value = "-------------------------------------------";
        _sheet.Cell(2, 1).Value = "01/03/2020 - real entry";
        _sheet.Cell(2, 2).Value = 100.0;

        var entries = ControleMaeSheetImporter.Import(_sheet, _mae, _report, TestClock.Today);

        entries.Should().ContainSingle();
    }

    [Fact]
    public void Import_RowWithNoExtractableDate_IsSkippedAndFlagged()
    {
        _sheet.Cell(1, 1).Value = "IPTU 1/?? 2023/02";
        _sheet.Cell(1, 2).Value = 90.0;

        var entries = ControleMaeSheetImporter.Import(_sheet, _mae, _report, TestClock.Today);

        entries.Should().ContainSingle();
        entries[0].Date.Should().Be(new DateOnly(2023, 2, 1));
    }

    [Fact]
    public void Import_FirstRowHasNoExtractableDateAndNoPriorEntry_IsSkippedAndFlagged()
    {
        _sheet.Cell(1, 1).Value = "Sem nenhuma data reconhecivel aqui";
        _sheet.Cell(1, 2).Value = 90.0;

        var entries = ControleMaeSheetImporter.Import(_sheet, _mae, _report, TestClock.Today);

        entries.Should().BeEmpty();
        _report.RowIssues.Should().ContainSingle(i => i.SheetName == "Controle mae" && i.Row == 1);
    }

    [Fact]
    public void Import_MonthYearRangeWithFullSpelledEndMonth_ResolvesToFirstDayOfEndMonth()
    {
        _sheet.Cell(1, 1).Value = "Seguro Jan/2021-Maio/2022";
        _sheet.Cell(1, 2).Value = 900.0;

        var entries = ControleMaeSheetImporter.Import(_sheet, _mae, _report, TestClock.Today);

        entries.Should().ContainSingle();
        entries[0].Date.Should().Be(new DateOnly(2022, 5, 1));
    }

    [Fact]
    public void Import_MonthYearRange_ResolvesToFirstDayOfEndMonth()
    {
        _sheet.Cell(1, 1).Value = "Jan/2020-Dez/2020 - Seguro";
        _sheet.Cell(1, 2).Value = 1200.0;

        var entries = ControleMaeSheetImporter.Import(_sheet, _mae, _report, TestClock.Today);

        entries.Should().ContainSingle();
        entries[0].Date.Should().Be(new DateOnly(2020, 12, 1));
        _report.RowIssues.Should().BeEmpty();
    }

    [Fact]
    public void Import_RowWithNoExtractableDateButPriorEntryExists_InfersPreviousDatePlusOneDayAndIsImported()
    {
        _sheet.Cell(1, 1).Value = "Acerto 29/12/2023";
        _sheet.Cell(1, 2).Value = 500.0;

        _sheet.Cell(2, 1).Value = "Conta Gabriel";
        _sheet.Cell(2, 2).Value = 100.0;

        var entries = ControleMaeSheetImporter.Import(_sheet, _mae, _report, TestClock.Today);

        entries.Should().HaveCount(2);
        entries[0].Date.Should().Be(new DateOnly(2023, 12, 29));
        entries[1].Date.Should().Be(new DateOnly(2023, 12, 30));
        _report.RowIssues.Should().ContainSingle(i => i.Row == 2 && i.SheetName == "Controle mae");
    }

    [Fact]
    public void Import_MultipleConsecutiveRowsWithoutDates_EachAdvancesOneDayFromThePrevious()
    {
        _sheet.Cell(1, 1).Value = "Acerto 10/01/2021";
        _sheet.Cell(1, 2).Value = 500.0;

        _sheet.Cell(2, 1).Value = "Sem data 1";
        _sheet.Cell(2, 2).Value = 10.0;

        _sheet.Cell(3, 1).Value = "Sem data 2";
        _sheet.Cell(3, 2).Value = 20.0;

        var entries = ControleMaeSheetImporter.Import(_sheet, _mae, _report, TestClock.Today);

        entries.Should().HaveCount(3);
        entries[0].Date.Should().Be(new DateOnly(2021, 1, 10));
        entries[1].Date.Should().Be(new DateOnly(2021, 1, 11));
        entries[2].Date.Should().Be(new DateOnly(2021, 1, 12));
    }

    [Fact]
    public void Import_RowWithNoValueAtAll_IsSkipped()
    {
        _sheet.Cell(1, 1).Value = "01/03/2020 apenas nota sem valor";

        var entries = ControleMaeSheetImporter.Import(_sheet, _mae, _report, TestClock.Today);

        entries.Should().BeEmpty();
        _report.RowIssues.Should().BeEmpty();
    }

    [Fact]
    public void Import_NegativeValues_ArePaidByMaeWithPositiveAmounts()
    {
        _sheet.Cell(1, 1).Value = "Mae pagou 10/02/2024";
        _sheet.Cell(1, 2).Value = -120.0;
        _sheet.Cell(1, 3).Value = -19.5;

        var entry = ControleMaeSheetImporter.Import(_sheet, _mae, _report, TestClock.Today).Single();

        entry.PaidBy.Should().Be(SettlementPayer.Other);
        entry.Amount.Should().Be(120.0m);
        entry.ConvertedValue.Should().Be(19.5m);
    }

    [Fact]
    public void Import_OnlyGbpValue_KeepsTheSheetsGbpTag()
    {
        _sheet.Cell(1, 1).Value = "Libras 11/10/2024";
        _sheet.Cell(1, 3).Value = 80.0;

        var entry = ControleMaeSheetImporter.Import(_sheet, _mae, _report, TestClock.Today).Single();

        entry.Currency.Should().Be(Currency.GBP);
        entry.Amount.Should().Be(80.0m);
        entry.ConvertedValue.Should().BeNull();
    }

    [Theory]
    [InlineData(0.0, 10.0)]
    [InlineData(100.0, -15.0)]
    [InlineData(-100.0, 15.0)]
    [InlineData(100.0, 0.0)]
    public void Import_ZeroOrOppositeSignedValues_IsSkippedAndFlagged(double brl, double gbp)
    {
        _sheet.Cell(1, 1).Value = "Acerto 01/03/2020";
        _sheet.Cell(1, 2).Value = brl;
        _sheet.Cell(1, 3).Value = gbp;

        var entries = ControleMaeSheetImporter.Import(_sheet, _mae, _report, TestClock.Today);

        entries.Should().BeEmpty();
        _report.RowIssues.Should().ContainSingle(i => i.SheetName == "Controle mae" && i.Row == 1);
    }

    [Fact]
    public void Import_DateAfterToday_IsSkippedAndFlagged()
    {
        _sheet.Cell(1, 1).Value = "Acerto 05/10/2026";
        _sheet.Cell(1, 2).Value = 100.0;
        _sheet.Cell(2, 1).Value = "Acerto 06/10/2026";
        _sheet.Cell(2, 2).Value = 50.0;

        var entries = ControleMaeSheetImporter.Import(_sheet, _mae, _report, new DateOnly(2026, 10, 5));

        entries.Should().ContainSingle().Which.Date.Should().Be(new DateOnly(2026, 10, 5));
        _report.RowIssues.Should().ContainSingle(i => i.SheetName == "Controle mae" && i.Row == 2);
    }
}