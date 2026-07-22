using ClosedXML.Excel;
using Financial.CashFlow.Domain.Enums;
using Financial.CashFlow.Infrastructure.Integrations.CashFlowSpreadsheetImport.SheetImporters;
using FluentAssertions;

namespace Financial.CashFlowSpreadsheetImport.Tests.SheetImporters;

public class ReservasSheetImporterTests
{
    [Fact]
    public void Import_RowWithAllFiveBucketsPopulated_CreatesOneMovementPerBucket()
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

        var movements = ReservasSheetImporter.Import(sheet);

        movements.Should().HaveCount(5);
        movements.Should().Contain(m => m.Bucket == ReserveBucket.Dizimo && m.Amount == 50.0m);
        movements.Should().Contain(m => m.Bucket == ReserveBucket.Investimento && m.Amount == 100.0m);
        movements.Should().Contain(m => m.Bucket == ReserveBucket.HouseTreats && m.Amount == 30.0m);
        movements.Should().Contain(m => m.Bucket == ReserveBucket.Ariana && m.Amount == 20.0m);
        movements.Should().Contain(m => m.Bucket == ReserveBucket.Gleison && m.Amount == 20.0m);
        movements.Should().OnlyContain(m => m.Date == new DateOnly(2020, 3, 15) && m.Description == "Ramsay");
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

        var movements = ReservasSheetImporter.Import(sheet);

        movements.Should().ContainSingle();
        movements[0].Bucket.Should().Be(ReserveBucket.Ariana);
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

        var movements = ReservasSheetImporter.Import(sheet);

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

        var movements = ReservasSheetImporter.Import(sheet);

        movements.Should().BeEmpty();
    }
}
