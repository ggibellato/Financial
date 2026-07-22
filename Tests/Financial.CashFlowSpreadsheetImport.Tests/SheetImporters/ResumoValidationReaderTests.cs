using ClosedXML.Excel;
using Financial.CashFlow.Domain.Enums;
using Financial.CashFlow.Infrastructure.Integrations.CashFlowSpreadsheetImport.SheetImporters;
using FluentAssertions;

namespace Financial.CashFlowSpreadsheetImport.Tests.SheetImporters;

public class ResumoValidationReaderTests
{
    [Fact]
    public void ImportAccountSnapshots_CanonicalLayout_CreatesOneSnapshotPerMonthPerAccount()
    {
        using var workbook = new XLWorkbook();
        var sheet = workbook.AddWorksheet("Resumo2026");

        sheet.Cell(29, 1).Value = "Blue Rewards Saver";
        sheet.Cell(29, 2).Value = 5000.0;
        sheet.Cell(29, 3).Value = 5000.0;

        var snapshots = ResumoValidationReader.ImportAccountSnapshots(sheet, 2026);

        snapshots.Should().HaveCount(2);
        snapshots.Should().OnlyContain(s => s.Account == InvestmentAccount.BlueRewardsSaver && s.Year == 2026);
        snapshots.Should().Contain(s => s.Month == 1 && s.Value == 5000.0m);
        snapshots.Should().Contain(s => s.Month == 2 && s.Value == 5000.0m);
    }

    [Fact]
    public void ImportAccountSnapshots_LiabilityNegativeValue_TakesAbsoluteValue()
    {
        using var workbook = new XLWorkbook();
        var sheet = workbook.AddWorksheet("Resumo2026");

        sheet.Cell(30, 1).Value = "Platinum Visa 8003 (-)";
        sheet.Cell(30, 2).Value = -433.78;

        var snapshots = ResumoValidationReader.ImportAccountSnapshots(sheet, 2026);

        snapshots.Should().ContainSingle();
        snapshots[0].Account.Should().Be(InvestmentAccount.PlatinumVisa8003);
        snapshots[0].Value.Should().Be(433.78m);
    }

    [Fact]
    public void ImportAccountSnapshots_LabelWithDoubleSpaceVariant_StillResolves()
    {
        using var workbook = new XLWorkbook();
        var sheet = workbook.AddWorksheet("Resumo2026");

        sheet.Cell(31, 1).Value = "Chase Master  4023 (-)";
        sheet.Cell(31, 2).Value = -11526.59;

        var snapshots = ResumoValidationReader.ImportAccountSnapshots(sheet, 2026);

        snapshots.Should().ContainSingle().Which.Account.Should().Be(InvestmentAccount.ChaseMaster4023);
    }

    [Fact]
    public void ImportAccountSnapshots_UnrecognizedHistoricalAccountLabel_IsNotWritten()
    {
        using var workbook = new XLWorkbook();
        var sheet = workbook.AddWorksheet("Resumo2023");

        sheet.Cell(30, 1).Value = "Help to Buy ISA GGS";
        sheet.Cell(30, 2).Value = 15682.05;

        var snapshots = ResumoValidationReader.ImportAccountSnapshots(sheet, 2023);

        snapshots.Should().BeEmpty();
    }

    [Fact]
    public void ImportAccountSnapshots_EmptyMonthCell_IsSkippedForThatMonthOnly()
    {
        using var workbook = new XLWorkbook();
        var sheet = workbook.AddWorksheet("Resumo2026");

        sheet.Cell(36, 1).Value = "Trading 212 Invested";
        sheet.Cell(36, 3).Value = 22644.98;

        var snapshots = ResumoValidationReader.ImportAccountSnapshots(sheet, 2026);

        snapshots.Should().ContainSingle().Which.Month.Should().Be(2);
    }

    [Fact]
    public void ReadYearlyExpenseTotals_ModernLabel_FindsRowAndReturnsMonthlyValues()
    {
        using var workbook = new XLWorkbook();
        var sheet = workbook.AddWorksheet("Resumo2026");

        sheet.Cell(25, 1).Value = "Total despesas";
        sheet.Cell(25, 2).Value = 7636.01;
        sheet.Cell(25, 3).Value = 4776.93;

        var totals = ResumoValidationReader.ReadYearlyExpenseTotals(sheet);

        totals.Should().NotBeNull();
        totals![1].Should().Be(7636.01m);
        totals[2].Should().Be(4776.93m);
    }

    [Fact]
    public void ReadYearlyExpenseTotals_2017StyleShortLabel_FindsRowToo()
    {
        using var workbook = new XLWorkbook();
        var sheet = workbook.AddWorksheet("Resumo2017");

        sheet.Cell(14, 1).Value = "Total";
        sheet.Cell(14, 3).Value = 4125.55;

        var totals = ResumoValidationReader.ReadYearlyExpenseTotals(sheet);

        totals.Should().NotBeNull();
        totals![2].Should().Be(4125.55m);
    }

    [Fact]
    public void ReadYearlyExpenseTotals_NoMatchingRow_ReturnsNull()
    {
        using var workbook = new XLWorkbook();
        var sheet = workbook.AddWorksheet("Resumo2026");
        sheet.Cell(1, 1).Value = "Something else";

        var totals = ResumoValidationReader.ReadYearlyExpenseTotals(sheet);

        totals.Should().BeNull();
    }
}
