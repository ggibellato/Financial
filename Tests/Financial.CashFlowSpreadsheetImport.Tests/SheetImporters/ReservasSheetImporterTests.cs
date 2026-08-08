using ClosedXML.Excel;
using Financial.CashFlow.Domain.Entities;
using Financial.CashFlow.Infrastructure.Integrations.CashFlowSpreadsheetImport.Reporting;
using Financial.CashFlow.Infrastructure.Integrations.CashFlowSpreadsheetImport.SheetImporters;
using FluentAssertions;

namespace Financial.CashFlowSpreadsheetImport.Tests.SheetImporters;

public class ReservasSheetImporterTests
{
    private static readonly ReserveBucket Investimento = ReserveBucket.Create("Investimento", 33.33m);
    private static readonly ReserveBucket HouseTreats = ReserveBucket.Create("HouseTreats", 33.33m);
    private static readonly ReserveBucket Ariana = ReserveBucket.Create("Ariana", 16.67m);
    private static readonly ReserveBucket Gleison = ReserveBucket.Create("Gleison", 16.67m);

    private static IReadOnlyCollection<ReserveBucket> FourBuckets => [Investimento, HouseTreats, Ariana, Gleison];

    [Fact]
    public void Import_RowWithAllFourBucketsPopulated_CreatesOneMovementPerBucket()
    {
        using var workbook = new XLWorkbook();
        var sheet = workbook.AddWorksheet("Reservas");
        sheet.Cell(1, 1).Value = "Data";
        sheet.Cell(1, 2).Value = "Descricao";

        sheet.Cell(2, 1).Value = new DateTime(2020, 3, 15);
        sheet.Cell(2, 2).Value = "Ramsay";
        sheet.Cell(2, 4).Value = 50.0;
        sheet.Cell(2, 6).Value = 100.0;
        sheet.Cell(2, 7).Value = 30.0;
        sheet.Cell(2, 8).Value = 20.0;
        sheet.Cell(2, 9).Value = 20.0;

        var movements = ReservasSheetImporter.Import(sheet, FourBuckets, new ImportReport());

        movements.Should().HaveCount(4);
        movements.Should().Contain(m => m.Bucket == Investimento && m.Amount == 100.0m);
        movements.Should().Contain(m => m.Bucket == HouseTreats && m.Amount == 30.0m);
        movements.Should().Contain(m => m.Bucket == Ariana && m.Amount == 20.0m);
        movements.Should().Contain(m => m.Bucket == Gleison && m.Amount == 20.0m);
        movements.Should().OnlyContain(m => m.Date == new DateOnly(2020, 3, 15) && m.Description == "Ramsay");
    }

    [Fact]
    public void Import_DizimoColumnPopulated_IsIgnoredAsNonBucketIntermediateValue()
    {
        using var workbook = new XLWorkbook();
        var sheet = workbook.AddWorksheet("Reservas");
        sheet.Cell(1, 1).Value = "Data";

        sheet.Cell(2, 1).Value = new DateTime(2021, 1, 1);
        sheet.Cell(2, 4).Value = 50.0;

        var movements = ReservasSheetImporter.Import(sheet, FourBuckets, new ImportReport());

        movements.Should().BeEmpty();
    }

    [Fact]
    public void Import_RowWithSingleBucketPopulated_CreatesOneWithdrawalMovement()
    {
        using var workbook = new XLWorkbook();
        var sheet = workbook.AddWorksheet("Reservas");
        sheet.Cell(1, 1).Value = "Data";
        sheet.Cell(1, 2).Value = "Descricao";

        sheet.Cell(2, 1).Value = new DateTime(2021, 6, 1);
        sheet.Cell(2, 2).Value = "Saque casa";
        sheet.Cell(2, 8).Value = 75.5;

        var movements = ReservasSheetImporter.Import(sheet, FourBuckets, new ImportReport());

        movements.Should().ContainSingle();
        movements[0].Bucket.Should().Be(Ariana);
        movements[0].Amount.Should().Be(75.5m);
    }

    [Fact]
    public void Import_LimpoColumnPopulated_IsIgnoredAsNonBucketIntermediateValue()
    {
        using var workbook = new XLWorkbook();
        var sheet = workbook.AddWorksheet("Reservas");
        sheet.Cell(1, 1).Value = "Data";

        sheet.Cell(2, 1).Value = new DateTime(2021, 1, 1);
        sheet.Cell(2, 5).Value = 999.0;

        var movements = ReservasSheetImporter.Import(sheet, FourBuckets, new ImportReport());

        movements.Should().BeEmpty();
    }

    [Fact]
    public void Import_RowWithoutValidDate_IsSkipped()
    {
        using var workbook = new XLWorkbook();
        var sheet = workbook.AddWorksheet("Reservas");
        sheet.Cell(1, 1).Value = "Data";

        sheet.Cell(2, 2).Value = "Nota sem data";
        sheet.Cell(2, 4).Value = 10.0;

        var movements = ReservasSheetImporter.Import(sheet, FourBuckets, new ImportReport());

        movements.Should().BeEmpty();
    }

    [Fact]
    public void Import_WithACanonicalBucketNameNotSeeded_SkipsThatColumnAndLogsAWarning()
    {
        using var workbook = new XLWorkbook();
        var sheet = workbook.AddWorksheet("Reservas");
        sheet.Cell(1, 1).Value = "Data";
        sheet.Cell(1, 2).Value = "Descricao";
        sheet.Cell(2, 1).Value = new DateTime(2021, 6, 1);
        sheet.Cell(2, 2).Value = "Ramsay";
        sheet.Cell(2, 6).Value = 100.0;
        sheet.Cell(2, 7).Value = 30.0;

        var incompleteBuckets = new[] { HouseTreats, Ariana, Gleison };
        var report = new ImportReport();

        var movements = ReservasSheetImporter.Import(sheet, incompleteBuckets, report);

        movements.Should().ContainSingle();
        movements[0].Bucket.Should().Be(HouseTreats);
        movements[0].Amount.Should().Be(30.0m);
        report.ValidationWarnings.Should().ContainSingle(w => w.Contains("Investimento") && w.Contains("not seeded"));
    }
}
