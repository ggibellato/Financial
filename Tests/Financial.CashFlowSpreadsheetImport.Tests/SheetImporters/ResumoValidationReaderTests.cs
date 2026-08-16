using ClosedXML.Excel;
using Financial.CashFlow.Domain.Entities;
using Financial.CashFlow.Infrastructure.Tools.CashFlowSpreadsheetImport.Migrations.InvestmentAccounts;
using Financial.CashFlow.Infrastructure.Tools.CashFlowSpreadsheetImport.Reporting;
using Financial.CashFlow.Infrastructure.Tools.CashFlowSpreadsheetImport.SheetImporters;
using FluentAssertions;

namespace Financial.CashFlowSpreadsheetImport.Tests.SheetImporters;

public class ResumoValidationReaderTests
{
    private static IReadOnlyCollection<InvestmentAccount> SeededAccounts()
    {
        var data = CashFlowData.Create();
        InvestmentAccountMigrator.Migrate(data);
        return data.InvestmentAccounts;
    }

    [Fact]
    public void ImportAccountSnapshots_CanonicalLayout_CreatesTwelveSnapshotsWithBlankMonthsAsZero()
    {
        using var workbook = new XLWorkbook();
        var sheet = workbook.AddWorksheet("Resumo2026");

        sheet.Cell(29, 1).Value = "Blue Rewards Saver";
        sheet.Cell(29, 2).Value = 5000.0;
        sheet.Cell(29, 3).Value = 5000.0;

        var snapshots = ResumoValidationReader.ImportAccountSnapshots(sheet, 2026, SeededAccounts(), new ImportReport());

        snapshots.Should().HaveCount(12);
        snapshots.Should().OnlyContain(s => s.Account.Name == "BlueRewardsSaver" && s.Year == 2026);
        snapshots.Should().Contain(s => s.Month == 1 && s.Value == 5000.0m);
        snapshots.Should().Contain(s => s.Month == 2 && s.Value == 5000.0m);
        snapshots.Where(s => s.Month is >= 3 and <= 12).Should().OnlyContain(s => s.Value == 0m);
    }

    [Fact]
    public void ImportAccountSnapshots_LiabilityPositiveValue_TakesNegativeValue()
    {
        using var workbook = new XLWorkbook();
        var sheet = workbook.AddWorksheet("Resumo2026");

        sheet.Cell(30, 1).Value = "Platinum Visa 8003 (-)";
        sheet.Cell(30, 2).Value = 433.78;

        var snapshots = ResumoValidationReader.ImportAccountSnapshots(sheet, 2026, SeededAccounts(), new ImportReport());

        snapshots.Should().HaveCount(12);
        snapshots.Should().ContainSingle(s => s.Month == 1).Which.Value.Should().Be(-433.78m);
        snapshots.Where(s => s.Month != 1).Should().OnlyContain(s => s.Value == 0m);
    }

    [Fact]
    public void ImportAccountSnapshots_LiabilityNegativeValue_TakesPositiveValue()
    {
        using var workbook = new XLWorkbook();
        var sheet = workbook.AddWorksheet("Resumo2026");

        sheet.Cell(30, 1).Value = "Platinum Visa 8003 (-)";
        sheet.Cell(30, 2).Value = -433.78;

        var snapshots = ResumoValidationReader.ImportAccountSnapshots(sheet, 2026, SeededAccounts(), new ImportReport());

        snapshots.Should().ContainSingle(s => s.Month == 1).Which.Value.Should().Be(433.78m);
    }

    [Fact]
    public void ImportAccountSnapshots_NonLiabilityPositiveValue_TakesPositiveValue()
    {
        using var workbook = new XLWorkbook();
        var sheet = workbook.AddWorksheet("Resumo2026");

        sheet.Cell(30, 1).Value = "Trading 212 Invested";
        sheet.Cell(30, 2).Value = 433.78;

        var snapshots = ResumoValidationReader.ImportAccountSnapshots(sheet, 2026, SeededAccounts(), new ImportReport());

        snapshots.Should().ContainSingle(s => s.Month == 1).Which.Value.Should().Be(433.78m);
    }

    [Fact]
    public void ImportAccountSnapshots_NonLiabilityNegativeValue_TakesNegativeValue()
    {
        using var workbook = new XLWorkbook();
        var sheet = workbook.AddWorksheet("Resumo2026");

        sheet.Cell(30, 1).Value = "Trading 212 Invested";
        sheet.Cell(30, 2).Value = -433.78;

        var snapshots = ResumoValidationReader.ImportAccountSnapshots(sheet, 2026, SeededAccounts(), new ImportReport());

        snapshots.Should().ContainSingle(s => s.Month == 1).Which.Value.Should().Be(-433.78m);
    }


    [Fact]
    public void ImportAccountSnapshots_LabelWithDoubleSpaceVariant_StillResolves()
    {
        using var workbook = new XLWorkbook();
        var sheet = workbook.AddWorksheet("Resumo2026");

        sheet.Cell(31, 1).Value = "Chase Master  4023 (-)";
        sheet.Cell(31, 2).Value = -11526.59;

        var snapshots = ResumoValidationReader.ImportAccountSnapshots(sheet, 2026, SeededAccounts(), new ImportReport());

        snapshots.Should().OnlyContain(s => s.Account.Name == "ChaseMaster4023");
    }

    [Fact]
    public void ImportAccountSnapshots_HistoricalAccountLabel_NowResolves()
    {
        using var workbook = new XLWorkbook();
        var sheet = workbook.AddWorksheet("Resumo2023");

        sheet.Cell(30, 1).Value = "Help to Buy ISA GGS";
        sheet.Cell(30, 2).Value = 15682.05;

        var snapshots = ResumoValidationReader.ImportAccountSnapshots(sheet, 2023, SeededAccounts(), new ImportReport());

        snapshots.Should().OnlyContain(s => s.Account.Name == "HelpToBuyIsaGgs");
        snapshots.Should().ContainSingle(s => s.Month == 1).Which.Value.Should().Be(15682.05m);
    }

    [Fact]
    public void ImportAccountSnapshots_TrulyUnknownLabel_IsNotWritten()
    {
        using var workbook = new XLWorkbook();
        var sheet = workbook.AddWorksheet("Resumo2023");

        sheet.Cell(30, 1).Value = "Some Unrecognized Account";
        sheet.Cell(30, 2).Value = 100.00;

        var snapshots = ResumoValidationReader.ImportAccountSnapshots(sheet, 2023, SeededAccounts(), new ImportReport());

        snapshots.Should().BeEmpty();
    }

    [Fact]
    public void ImportAccountSnapshots_BarclaysBlueRewardsLabel_ResolvesToItsOwnAccountNotBlueRewardsSaver()
    {
        using var workbook = new XLWorkbook();
        var sheet = workbook.AddWorksheet("Resumo2020");

        sheet.Cell(23, 1).Value = "Barclays Blue Rewards";
        sheet.Cell(23, 2).Value = 250.00;

        var snapshots = ResumoValidationReader.ImportAccountSnapshots(sheet, 2020, SeededAccounts(), new ImportReport());

        snapshots.Should().OnlyContain(s => s.Account.Name == "BarclaysBlueRewards");
    }

    [Fact]
    public void ImportAccountSnapshots_ChipCashIsaLabel_ResolvesToChipCashIsaGleison()
    {
        using var workbook = new XLWorkbook();
        var sheet = workbook.AddWorksheet("Resumo2024");

        sheet.Cell(35, 1).Value = "Chip Cash ISA";
        sheet.Cell(35, 2).Value = 500.00;

        var snapshots = ResumoValidationReader.ImportAccountSnapshots(sheet, 2024, SeededAccounts(), new ImportReport());

        snapshots.Should().OnlyContain(s => s.Account.Name == "ChipCashIsaGleison");
    }

    [Fact]
    public void ImportAccountSnapshots_InstantIseIssue1Typo_ResolvesToInstantIsaIssue1()
    {
        using var workbook = new XLWorkbook();
        var sheet = workbook.AddWorksheet("Resumo2017");

        sheet.Cell(20, 1).Value = "Instant ISE Issue 1";
        sheet.Cell(20, 2).Value = 1000.00;

        var snapshots = ResumoValidationReader.ImportAccountSnapshots(sheet, 2017, SeededAccounts(), new ImportReport());

        snapshots.Should().OnlyContain(s => s.Account.Name == "InstantIsaIssue1");
    }

    [Fact]
    public void ImportAccountSnapshots_EmptyMonthCell_WritesExplicitZero()
    {
        using var workbook = new XLWorkbook();
        var sheet = workbook.AddWorksheet("Resumo2026");

        sheet.Cell(36, 1).Value = "Trading 212 Invested";
        sheet.Cell(36, 3).Value = 22644.98;

        var snapshots = ResumoValidationReader.ImportAccountSnapshots(sheet, 2026, SeededAccounts(), new ImportReport());

        snapshots.Should().HaveCount(12);
        snapshots.Should().ContainSingle(s => s.Month == 2).Which.Value.Should().Be(22644.98m);
        snapshots.Where(s => s.Month != 2).Should().OnlyContain(s => s.Value == 0m);
    }

    [Fact]
    public void ImportAccountSnapshots_MalformedMonthCell_LogsWarningAndWritesNoSnapshotForThatMonth()
    {
        using var workbook = new XLWorkbook();
        var sheet = workbook.AddWorksheet("Resumo2026");

        sheet.Cell(36, 1).Value = "Trading 212 Invested";
        sheet.Cell(36, 2).Value = "n/a";
        sheet.Cell(36, 3).Value = 100.00;
        var report = new ImportReport();

        var snapshots = ResumoValidationReader.ImportAccountSnapshots(sheet, 2026, SeededAccounts(), report);

        snapshots.Should().HaveCount(11);
        snapshots.Should().NotContain(s => s.Month == 1);
        snapshots.Should().ContainSingle(s => s.Month == 2).Which.Value.Should().Be(100.00m);
        report.RowIssues.Should().ContainSingle(i => i.SheetName == "Resumo2026" && i.Row == 36);
    }

    [Fact]
    public void ReadAnnualExpenseTotals_ModernLabel_FindsRowAndReturnsMonthlyValues()
    {
        using var workbook = new XLWorkbook();
        var sheet = workbook.AddWorksheet("Resumo2026");

        sheet.Cell(25, 1).Value = "Total despesas";
        sheet.Cell(25, 2).Value = 7636.01;
        sheet.Cell(25, 3).Value = 4776.93;

        var totals = ResumoValidationReader.ReadAnnualExpenseTotals(sheet);

        totals.Should().NotBeNull();
        totals![1].Should().Be(7636.01m);
        totals[2].Should().Be(4776.93m);
    }

    [Fact]
    public void ReadAnnualExpenseTotals_2017StyleShortLabel_FindsRowToo()
    {
        using var workbook = new XLWorkbook();
        var sheet = workbook.AddWorksheet("Resumo2017");

        sheet.Cell(14, 1).Value = "Total";
        sheet.Cell(14, 3).Value = 4125.55;

        var totals = ResumoValidationReader.ReadAnnualExpenseTotals(sheet);

        totals.Should().NotBeNull();
        totals![2].Should().Be(4125.55m);
    }

    [Fact]
    public void ReadAnnualExpenseTotals_NoMatchingRow_ReturnsNull()
    {
        using var workbook = new XLWorkbook();
        var sheet = workbook.AddWorksheet("Resumo2026");
        sheet.Cell(1, 1).Value = "Something else";

        var totals = ResumoValidationReader.ReadAnnualExpenseTotals(sheet);

        totals.Should().BeNull();
    }
}
