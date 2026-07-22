using ClosedXML.Excel;
using Financial.CashFlow.Domain.Entities;
using Financial.CashFlow.Domain.Enums;
using Financial.CashFlow.Infrastructure.Integrations.CashFlowSpreadsheetImport.Parsing;

namespace Financial.CashFlow.Infrastructure.Integrations.CashFlowSpreadsheetImport.SheetImporters;

/// <summary>
/// Parses the single continuous "Reservas" sheet (spanning every year) into
/// <see cref="ReserveMovement"/> entities. Each row can populate any subset of the 5 bucket
/// columns; one movement is created per populated bucket column on that row, regardless of
/// whether the row represents an income-split deposit (all 5 populated, proportioned per F05's
/// tithe-then-thirds/sixths math) or a single-bucket withdrawal (one column populated) — both
/// shapes reconstruct correct running balances this way without needing to be told apart.
/// </summary>
public static class ReservasSheetImporter
{
    private const int DateColumn = 1;
    private const int DescriptionColumn = 2;
    private const int FirstDataRow = 2;

    private static readonly (int Column, ReserveBucket Bucket)[] BucketColumns =
    [
        (4, ReserveBucket.Dizimo),
        (6, ReserveBucket.Investimento),
        (7, ReserveBucket.HouseTreats),
        (8, ReserveBucket.Ariana),
        (9, ReserveBucket.Gleison),
    ];

    public static IReadOnlyList<ReserveMovement> Import(IXLWorksheet sheet)
    {
        var lastRow = sheet.LastRowUsed()?.RowNumber() ?? 1;
        var movements = new List<ReserveMovement>();

        for (var row = FirstDataRow; row <= lastRow; row++)
        {
            var dateCell = sheet.Cell(row, DateColumn);
            if (dateCell.IsEmpty() || !dateCell.TryGetValue<DateTime>(out var dateTime))
            {
                continue;
            }

            var date = DateOnly.FromDateTime(dateTime);
            var description = sheet.Cell(row, DescriptionColumn).GetString();

            foreach (var (column, bucket) in BucketColumns)
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
}
