using ClosedXML.Excel;
using Financial.CashFlow.Domain.Entities;
using Financial.CashFlow.Infrastructure.Tools.CashFlowSpreadsheetImport.Reporting;
using Financial.CashFlow.Infrastructure.Tools.CashFlowSpreadsheetImport.SheetImporters;
using FluentAssertions;

namespace Financial.CashFlowSpreadsheetImport.Tests.SheetImporters;

public class ReservasSheetImporterTests : IDisposable
{
    /// <summary>Every test drives a fresh workbook, worksheet and import report; xUnit builds one instance per test, so they stay
    /// isolated exactly as the per-test `using var workbook` did.</summary>
    private readonly XLWorkbook _workbook;
    private readonly IXLWorksheet _sheet;
    private readonly ImportReport _report;

    public ReservasSheetImporterTests()
    {
        _workbook = new XLWorkbook();
        _sheet = _workbook.AddWorksheet("Reservas");
        _report = new ImportReport();
    }

    public void Dispose() => _workbook.Dispose();

    private static readonly ReserveBucket Investimento = ReserveBucket.Create("Investimento", 33.33m);
    private static readonly ReserveBucket HouseTreats = ReserveBucket.Create("HouseTreats", 33.33m);
    private static readonly ReserveBucket Ariana = ReserveBucket.Create("Ariana", 16.67m);
    private static readonly ReserveBucket Gleison = ReserveBucket.Create("Gleison", 16.67m);

    private static IReadOnlyCollection<ReserveBucket> FourBuckets => [Investimento, HouseTreats, Ariana, Gleison];

    [Fact]
    public void Import_RowWithAllFourBucketsPopulated_CreatesOneMovementPerBucket()
    {
        _sheet.Cell(1, 1).Value = "Data";
        _sheet.Cell(1, 2).Value = "Descricao";

        _sheet.Cell(2, 1).Value = new DateTime(2020, 3, 15);
        _sheet.Cell(2, 2).Value = "Ramsay";
        _sheet.Cell(2, 4).Value = 50.0;
        _sheet.Cell(2, 6).Value = 100.0;
        _sheet.Cell(2, 7).Value = 30.0;
        _sheet.Cell(2, 8).Value = 20.0;
        _sheet.Cell(2, 9).Value = 20.0;

        var movements = ReservasSheetImporter.Import(_sheet, FourBuckets, _report);

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
        _sheet.Cell(1, 1).Value = "Data";

        _sheet.Cell(2, 1).Value = new DateTime(2021, 1, 1);
        _sheet.Cell(2, 4).Value = 50.0;

        var movements = ReservasSheetImporter.Import(_sheet, FourBuckets, _report);

        movements.Should().BeEmpty();
    }

    [Fact]
    public void Import_RowWithSingleBucketPopulated_CreatesOneWithdrawalMovement()
    {
        _sheet.Cell(1, 1).Value = "Data";
        _sheet.Cell(1, 2).Value = "Descricao";

        _sheet.Cell(2, 1).Value = new DateTime(2021, 6, 1);
        _sheet.Cell(2, 2).Value = "Saque casa";
        _sheet.Cell(2, 8).Value = 75.5;

        var movements = ReservasSheetImporter.Import(_sheet, FourBuckets, _report);

        movements.Should().ContainSingle();
        movements[0].Bucket.Should().Be(Ariana);
        movements[0].Amount.Should().Be(75.5m);
    }

    [Fact]
    public void Import_LimpoColumnPopulated_IsIgnoredAsNonBucketIntermediateValue()
    {
        _sheet.Cell(1, 1).Value = "Data";

        _sheet.Cell(2, 1).Value = new DateTime(2021, 1, 1);
        _sheet.Cell(2, 5).Value = 999.0;

        var movements = ReservasSheetImporter.Import(_sheet, FourBuckets, _report);

        movements.Should().BeEmpty();
    }

    [Fact]
    public void Import_RowWithoutValidDate_IsSkipped()
    {
        _sheet.Cell(1, 1).Value = "Data";

        _sheet.Cell(2, 2).Value = "Nota sem data";
        _sheet.Cell(2, 4).Value = 10.0;

        var movements = ReservasSheetImporter.Import(_sheet, FourBuckets, _report);

        movements.Should().BeEmpty();
    }

    [Fact]
    public void Import_WithACanonicalBucketNameNotSeeded_SkipsThatColumnAndLogsAWarning()
    {
        _sheet.Cell(1, 1).Value = "Data";
        _sheet.Cell(1, 2).Value = "Descricao";
        _sheet.Cell(2, 1).Value = new DateTime(2021, 6, 1);
        _sheet.Cell(2, 2).Value = "Ramsay";
        _sheet.Cell(2, 6).Value = 100.0;
        _sheet.Cell(2, 7).Value = 30.0;

        var incompleteBuckets = new[] { HouseTreats, Ariana, Gleison };

        var movements = ReservasSheetImporter.Import(_sheet, incompleteBuckets, _report);

        movements.Should().ContainSingle();
        movements[0].Bucket.Should().Be(HouseTreats);
        movements[0].Amount.Should().Be(30.0m);
        _report.ValidationWarnings.Should().ContainSingle(w => w.Contains("Investimento") && w.Contains("not seeded"));
    }
}
