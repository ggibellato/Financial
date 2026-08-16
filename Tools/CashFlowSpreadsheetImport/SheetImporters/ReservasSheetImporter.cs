using ClosedXML.Excel;
using Financial.CashFlow.Application.Validation;
using Financial.CashFlow.Domain.Entities;
using Financial.CashFlow.Infrastructure.Tools.CashFlowSpreadsheetImport.Parsing;
using Financial.CashFlow.Infrastructure.Tools.CashFlowSpreadsheetImport.Reporting;

namespace Financial.CashFlow.Infrastructure.Tools.CashFlowSpreadsheetImport.SheetImporters;

/// <summary>
/// Parses the single continuous "Reservas" sheet (spanning every year) into
/// <see cref="ReserveMovement"/> entities. Each row can populate any subset of the 4 bucket
/// columns; one movement is created per populated bucket column on that row, regardless of
/// whether the row represents an income-split deposit (all 4 populated, proportioned per F05's
/// tithe-then-thirds/sixths math) or a single-bucket withdrawal (one column populated) — both
/// shapes reconstruct correct running balances this way without needing to be told apart.
/// Column 4 (Dizimo/tithe) is a non-bucket intermediate value and is not imported.
/// </summary>
public static class ReservasSheetImporter
{
    private const int DateColumn = 1;
    private const int DescriptionColumn = 2;
    private const int FirstDataRow = 2;

    private static readonly (int Column, string BucketName)[] BucketColumns =
    [
        (6, "Investimento"),
        (7, "HouseTreats"),
        (8, "Ariana"),
        (9, "Gleison"),
    ];

    public static IReadOnlyList<ReserveMovement> Import(IXLWorksheet sheet, IReadOnlyCollection<ReserveBucket> buckets, ImportReport report)
    {
        var lastRow = sheet.LastRowUsed()?.RowNumber() ?? 1;
        var movements = new List<ReserveMovement>();
        var resolvedBuckets = ResolveBucketColumns(sheet.Name, buckets, report);

        for (var row = FirstDataRow; row <= lastRow; row++)
        {
            var dateCell = sheet.Cell(row, DateColumn);
            if (dateCell.IsEmpty() || !dateCell.TryGetValue<DateTime>(out var dateTime))
            {
                continue;
            }

            var date = DateOnly.FromDateTime(dateTime);
            var description = sheet.Cell(row, DescriptionColumn).GetString();

            foreach (var (column, bucket) in resolvedBuckets)
            {
                var amount = NumericCellReader.TryRead(sheet.Cell(row, column));
                if (amount is null)
                {
                    continue;
                }

                movements.Add(ReserveMovement.Create(bucket, amount.Value, date, description));
            }
        }

        return movements;
    }

    private static (int Column, ReserveBucket Bucket)[] ResolveBucketColumns(
        string sheetName, IReadOnlyCollection<ReserveBucket> buckets, ImportReport report)
    {
        var resolved = new List<(int Column, ReserveBucket Bucket)>();

        foreach (var entry in BucketColumns)
        {
            if (ReserveBucketNameResolver.TryResolve(entry.BucketName, buckets, out var bucket))
            {
                resolved.Add((entry.Column, bucket!));
            }
            else
            {
                report.ValidationWarning(
                    $"{sheetName}: reserve bucket '{entry.BucketName}' is not seeded - column {entry.Column} skipped, no movements imported for it");
            }
        }

        return resolved.ToArray();
    }
}
