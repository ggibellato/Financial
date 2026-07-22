using ClosedXML.Excel;
using Financial.CashFlow.Domain.Entities;
using Financial.CashFlow.Domain.Enums;
using Financial.CashFlow.Infrastructure.Integrations.CashFlowSpreadsheetImport.Parsing;
using Financial.CashFlow.Infrastructure.Integrations.CashFlowSpreadsheetImport.Reporting;

namespace Financial.CashFlow.Infrastructure.Integrations.CashFlowSpreadsheetImport.SheetImporters;

/// <summary>
/// Parses one "MonYYYY" monthly expense tab into <see cref="Expense"/> entities. Tolerant of the
/// 11-18 column range across eras and the "Quem"/"Motivo" header-label swap (see
/// <see cref="ColumnResolver"/>). No per-transaction credit-card attribution is imported — the
/// source data never records which of the 5 cards a charge used (see F10's spec.md).
/// </summary>
public static class MonthlyExpenseSheetImporter
{
    private const int DayColumn = 1;
    private const int ValueColumn = 4;
    private const int PaymentSourceColumn = 5;
    private const int FirstDataRow = 2;
    private const int MaxColumnSampleRows = 200;

    public static IReadOnlyList<Expense> Import(IXLWorksheet sheet, int year, int month, ImportReport report)
    {
        var (descriptionColumn, categoryColumn) = ResolveColumns(sheet);
        var lastRow = sheet.LastRowUsed()?.RowNumber() ?? 1;
        var expenses = new List<Expense>();

        for (var row = FirstDataRow; row <= lastRow; row++)
        {
            var dayCell = sheet.Cell(row, DayColumn);
            var valueCell = sheet.Cell(row, ValueColumn);
            if (dayCell.IsEmpty() || valueCell.IsEmpty())
            {
                continue;
            }

            var rawCategory = sheet.Cell(row, categoryColumn).GetString();
            if (!CategoryResolver.TryResolve(rawCategory, out var category))
            {
                report.RowFlagged(sheet.Name, row, "Category", rawCategory, "Unrecognized category - expense not imported");
                continue;
            }

            var day = (int)dayCell.GetValue<double>();
            var daysInMonth = DateTime.DaysInMonth(year, month);
            DateOnly date;
            if (day >= 1 && day <= daysInMonth)
            {
                date = new DateOnly(year, month, day);
            }
            else
            {
                var clampedDay = Math.Clamp(day, 1, daysInMonth);
                date = new DateOnly(year, month, clampedDay);
                report.RowFlagged(
                    sheet.Name, row, "Day", day.ToString(),
                    $"Day {day} does not exist in {year}-{month:D2} - clamped to {clampedDay} (verify the actual date)");
            }

            var description = sheet.Cell(row, descriptionColumn).GetString();
            var value = (decimal)valueCell.GetValue<double>();
            var paymentSource = ResolvePaymentSource(sheet.Cell(row, PaymentSourceColumn).GetString());

            expenses.Add(Expense.Create(date, description, value, category, paymentSource, cardTag: null));
        }

        return expenses;
    }

    private static (int DescriptionColumn, int CategoryColumn) ResolveColumns(IXLWorksheet sheet)
    {
        const int ColumnB = 2;
        const int ColumnC = 3;
        var lastRow = Math.Min(sheet.LastRowUsed()?.RowNumber() ?? 1, MaxColumnSampleRows);

        var columnBValues = new List<string?>();
        var columnCValues = new List<string?>();
        for (var row = FirstDataRow; row <= lastRow; row++)
        {
            columnBValues.Add(sheet.Cell(row, ColumnB).GetString());
            columnCValues.Add(sheet.Cell(row, ColumnC).GetString());
        }

        return ColumnResolver.IsCategoryColumn(columnCValues, columnBValues)
            ? (ColumnB, ColumnC)
            : (ColumnC, ColumnB);
    }

    private static PaymentSource ResolvePaymentSource(string? tag) =>
        tag?.Trim().ToUpperInvariant() switch
        {
            "T" => PaymentSource.Trading212,
            "C" => PaymentSource.Chase,
            _ => PaymentSource.Barclays,
        };
}
