using ClosedXML.Excel;
using Financial.CashFlow.Domain.Enums;
using Financial.CashFlow.Infrastructure.Integrations.CashFlowSpreadsheetImport.Reporting;
using Financial.CashFlow.Infrastructure.Integrations.CashFlowSpreadsheetImport.SheetImporters;
using FluentAssertions;

namespace Financial.CashFlowSpreadsheetImport.Tests.SheetImporters;

public class ControleMaeSheetImporterTests
{
    [Fact]
    public void Import_FullDdMmYyyyDate_ParsesDateAndValues()
    {
        using var workbook = new XLWorkbook();
        var sheet = workbook.AddWorksheet("Controle mae");

        sheet.Cell(1, 1).Value = "Deposito feito na conta do Gabriel em 28/05/2019";
        sheet.Cell(1, 2).Value = 300.0;
        sheet.Cell(1, 3).Value = 60.0;

        var report = new ImportReport();

        var entries = ControleMaeSheetImporter.Import(sheet, report);

        entries.Should().ContainSingle();
        var entry = entries[0];
        entry.Date.Should().Be(new DateOnly(2019, 5, 28));
        entry.BrlValue.Should().Be(300.0m);
        entry.GbpValue.Should().Be(60.0m);
        entry.SourceCurrency.Should().Be(Currency.BRL);
        report.RowIssues.Should().BeEmpty();
    }

    [Fact]
    public void Import_MonthYearOnlyDate_ParsesToFirstOfMonth()
    {
        using var workbook = new XLWorkbook();
        var sheet = workbook.AddWorksheet("Controle mae");

        sheet.Cell(1, 1).Value = "Feito acerto em Dez/2018, seguro pago ate Dez/2018";
        sheet.Cell(1, 2).Value = 186.0;

        var report = new ImportReport();

        var entries = ControleMaeSheetImporter.Import(sheet, report);

        entries.Should().ContainSingle();
        entries[0].Date.Should().Be(new DateOnly(2018, 12, 1));
        entries[0].BrlValue.Should().Be(186.0m);
        entries[0].GbpValue.Should().BeNull();
    }

    [Fact]
    public void Import_ColumnCHoldsNonNumericNote_IsTreatedAsNoiseNotAValue()
    {
        using var workbook = new XLWorkbook();
        var sheet = workbook.AddWorksheet("Controle mae");

        sheet.Cell(1, 1).Value = "Acerto feito em 01/03/2020 - Tudo certo ate Dez/2019";
        sheet.Cell(1, 2).Value = 186.0;
        sheet.Cell(1, 3).Value = "62 por mes";

        var report = new ImportReport();

        var entries = ControleMaeSheetImporter.Import(sheet, report);

        entries.Should().ContainSingle();
        entries[0].GbpValue.Should().BeNull();
    }

    [Fact]
    public void Import_SeparatorRow_IsSkippedEntirely()
    {
        using var workbook = new XLWorkbook();
        var sheet = workbook.AddWorksheet("Controle mae");

        sheet.Cell(1, 1).Value = "-------------------------------------------";
        sheet.Cell(2, 1).Value = "01/03/2020 - real entry";
        sheet.Cell(2, 2).Value = 100.0;

        var report = new ImportReport();

        var entries = ControleMaeSheetImporter.Import(sheet, report);

        entries.Should().ContainSingle();
    }

    [Fact]
    public void Import_RowWithNoExtractableDate_IsSkippedAndFlagged()
    {
        using var workbook = new XLWorkbook();
        var sheet = workbook.AddWorksheet("Controle mae");

        sheet.Cell(1, 1).Value = "IPTU 1/?? 2023/02";
        sheet.Cell(1, 2).Value = 90.0;

        var report = new ImportReport();

        var entries = ControleMaeSheetImporter.Import(sheet, report);

        entries.Should().ContainSingle();
        entries[0].Date.Should().Be(new DateOnly(2023, 2, 1));
    }

    [Fact]
    public void Import_FirstRowHasNoExtractableDateAndNoPriorEntry_IsSkippedAndFlagged()
    {
        using var workbook = new XLWorkbook();
        var sheet = workbook.AddWorksheet("Controle mae");

        sheet.Cell(1, 1).Value = "Sem nenhuma data reconhecivel aqui";
        sheet.Cell(1, 2).Value = 90.0;

        var report = new ImportReport();

        var entries = ControleMaeSheetImporter.Import(sheet, report);

        entries.Should().BeEmpty();
        report.RowIssues.Should().ContainSingle(i => i.SheetName == "Controle mae" && i.Row == 1);
    }

    [Fact]
    public void Import_MonthYearRangeWithFullSpelledEndMonth_ResolvesToFirstDayOfEndMonth()
    {
        using var workbook = new XLWorkbook();
        var sheet = workbook.AddWorksheet("Controle mae");

        sheet.Cell(1, 1).Value = "Seguro Jan/2021-Maio/2022";
        sheet.Cell(1, 2).Value = 900.0;

        var report = new ImportReport();

        var entries = ControleMaeSheetImporter.Import(sheet, report);

        entries.Should().ContainSingle();
        entries[0].Date.Should().Be(new DateOnly(2022, 5, 1));
    }

    [Fact]
    public void Import_MonthYearRange_ResolvesToFirstDayOfEndMonth()
    {
        using var workbook = new XLWorkbook();
        var sheet = workbook.AddWorksheet("Controle mae");

        sheet.Cell(1, 1).Value = "Jan/2020-Dez/2020 - Seguro";
        sheet.Cell(1, 2).Value = 1200.0;

        var report = new ImportReport();

        var entries = ControleMaeSheetImporter.Import(sheet, report);

        entries.Should().ContainSingle();
        entries[0].Date.Should().Be(new DateOnly(2020, 12, 1));
        report.RowIssues.Should().BeEmpty();
    }

    [Fact]
    public void Import_RowWithNoExtractableDateButPriorEntryExists_InfersPreviousDatePlusOneDayAndIsImported()
    {
        using var workbook = new XLWorkbook();
        var sheet = workbook.AddWorksheet("Controle mae");

        sheet.Cell(1, 1).Value = "Acerto 29/12/2023";
        sheet.Cell(1, 2).Value = 500.0;

        sheet.Cell(2, 1).Value = "Conta Gabriel";
        sheet.Cell(2, 2).Value = 100.0;

        var report = new ImportReport();

        var entries = ControleMaeSheetImporter.Import(sheet, report);

        entries.Should().HaveCount(2);
        entries[0].Date.Should().Be(new DateOnly(2023, 12, 29));
        entries[1].Date.Should().Be(new DateOnly(2023, 12, 30));
        report.RowIssues.Should().ContainSingle(i => i.Row == 2 && i.SheetName == "Controle mae");
    }

    [Fact]
    public void Import_MultipleConsecutiveRowsWithoutDates_EachAdvancesOneDayFromThePrevious()
    {
        using var workbook = new XLWorkbook();
        var sheet = workbook.AddWorksheet("Controle mae");

        sheet.Cell(1, 1).Value = "Acerto 10/01/2021";
        sheet.Cell(1, 2).Value = 500.0;

        sheet.Cell(2, 1).Value = "Sem data 1";
        sheet.Cell(2, 2).Value = 10.0;

        sheet.Cell(3, 1).Value = "Sem data 2";
        sheet.Cell(3, 2).Value = 20.0;

        var report = new ImportReport();

        var entries = ControleMaeSheetImporter.Import(sheet, report);

        entries.Should().HaveCount(3);
        entries[0].Date.Should().Be(new DateOnly(2021, 1, 10));
        entries[1].Date.Should().Be(new DateOnly(2021, 1, 11));
        entries[2].Date.Should().Be(new DateOnly(2021, 1, 12));
    }

    [Fact]
    public void Import_RowWithNoValueAtAll_IsSkipped()
    {
        using var workbook = new XLWorkbook();
        var sheet = workbook.AddWorksheet("Controle mae");

        sheet.Cell(1, 1).Value = "01/03/2020 apenas nota sem valor";

        var report = new ImportReport();

        var entries = ControleMaeSheetImporter.Import(sheet, report);

        entries.Should().BeEmpty();
        report.RowIssues.Should().BeEmpty();
    }
}
