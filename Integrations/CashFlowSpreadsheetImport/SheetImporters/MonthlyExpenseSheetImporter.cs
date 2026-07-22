using ClosedXML.Excel;
using Financial.CashFlow.Domain.Entities;
using Financial.CashFlow.Domain.Enums;
using Financial.CashFlow.Infrastructure.Integrations.CashFlowSpreadsheetImport.Parsing;
using Financial.CashFlow.Infrastructure.Integrations.CashFlowSpreadsheetImport.Reporting;

namespace Financial.CashFlow.Infrastructure.Integrations.CashFlowSpreadsheetImport.SheetImporters;

/// <summary>
/// Parses one "MonYYYY" monthly expense tab into <see cref="Expense"/> entities. Tolerant of the
/// 11-18 column range across eras and the "Quem"/"Motivo" header-label swap (see
/// <see cref="ColumnResolver"/>). Historical months never record which of the 5 cards a charge
/// used, so no <see cref="Expense.CardTag"/> is set for them (see F10's spec.md). For the months
/// listed in <see cref="MonthsWithFixedCardSections"/>, the source spreadsheet instead groups
/// card charges into sections that each start at a known row number (see
/// <see cref="CardSectionStartRows"/>), so rows in those months are tagged by row position - but
/// only when column E has no explicit "T"/"C" payment-source tag, which still takes precedence
/// when present.
/// </summary>
public static class MonthlyExpenseSheetImporter
{
    private const int DayColumn = 1;
    private const int ValueColumn = 4;
    private const int PaymentSourceColumn = 5;
    private const int FirstDataRow = 2;
    private const int MaxColumnSampleRows = 200;

    private const int BarclaysPlatinumVisa8003StartRow = 129;
    private const int BarclaysPlatinumVisa6007StartRow = 142;
    private const int ChaseMaster4023StartRow = 205;
    private const int BaAmexStartRow = 226;

    private static readonly (int StartRow, CreditCard Card)[] CardSectionStartRows =
    [
        (BarclaysPlatinumVisa8003StartRow, CreditCard.BarclaysPlatinumVisa8003),
        (BarclaysPlatinumVisa6007StartRow, CreditCard.BarclaysPlatinumVisa6007),
        (ChaseMaster4023StartRow, CreditCard.ChaseMaster4023),
        (BaAmexStartRow, CreditCard.BaAmex),
    ];

    // Only the sheets for these months follow the fixed-row card layout above; every other month
    // (past or future) keeps using the column-E "T"/"C" tag exclusively. Extend this list as later
    // months are confirmed to follow the same layout.
    private static readonly (int Year, int Month)[] MonthsWithFixedCardSections =
    [
        (2026, 7),
        (2026, 8),
    ];

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

            var value = NumericCellReader.TryRead(valueCell);
            if (value is null)
            {
                report.RowFlagged(sheet.Name, row, "Value", valueCell.GetString(), "Value could not be parsed as a number - expense not imported");
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
            var rawPaymentSourceTag = sheet.Cell(row, PaymentSourceColumn).GetString();
            var paymentSource = ResolvePaymentSource(rawPaymentSourceTag);
            var cardTag = ResolveCardTag(row, year, month, rawPaymentSourceTag);

            expenses.Add(Expense.Create(date, description, value.Value, category, paymentSource, cardTag));
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

    private static CreditCard? ResolveCardTag(int row, int year, int month, string? rawPaymentSourceTag)
    {
        if (!string.IsNullOrWhiteSpace(rawPaymentSourceTag))
        {
            return null;
        }

        if (!MonthsWithFixedCardSections.Contains((year, month)))
        {
            return null;
        }

        CreditCard? cardTag = null;
        foreach (var (startRow, card) in CardSectionStartRows)
        {
            if (row >= startRow)
            {
                cardTag = card;
            }
        }

        return cardTag;
    }
}
