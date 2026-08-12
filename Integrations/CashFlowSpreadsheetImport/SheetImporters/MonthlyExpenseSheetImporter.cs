using ClosedXML.Excel;
using Financial.CashFlow.Domain.Entities;
using Financial.CashFlow.Domain.Enums;
using Financial.CashFlow.Infrastructure.Integrations.CashFlowSpreadsheetImport.Parsing;
using Financial.CashFlow.Infrastructure.Integrations.CashFlowSpreadsheetImport.Reporting;
using CreditCardEntity = Financial.CashFlow.Domain.Entities.CreditCard;

namespace Financial.CashFlow.Infrastructure.Integrations.CashFlowSpreadsheetImport.SheetImporters;

/// <summary>
/// Parses one "MonYYYY" monthly expense tab into <see cref="Expense"/> entities. Tolerant of the
/// 11-18 column range across eras and the "Quem"/"Motivo" header-label swap (see
/// <see cref="ColumnResolver"/>). For the current calendar month and any later month (relative to
/// the <c>today</c> passed to <see cref="Import"/>), the source spreadsheet groups card charges
/// into sections that each start at a known row number (see <see cref="CardSectionStartRows"/>),
/// so rows in those months are tagged by row position when column E is blank or carries the
/// "X"/"x" credit-card marker - "X" confirms the row belongs to the card section it falls in, it
/// never names a specific card. An explicit "T"/"C" tag still takes precedence over row position
/// (it means the row was paid directly from that bank, not a card). A row that does resolve a
/// card tag imports as an unsettled credit card charge - <see cref="Expense.PaymentSource"/>
/// stays null - and settlement is applied afterward in-app by marking the card statement paid,
/// never inferred by the import. Months before the current one never resolve a card tag by row
/// position - by the time a past month is imported, its statement has already been settled and
/// consolidated in the spreadsheet, so those rows (and any "X" that can't be matched to a card
/// section even in an eligible month) fall back to the normal column-E rule, which defaults an
/// unrecognized tag to the Barclays bank payment source.
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

    private static readonly (int StartRow, string CardName)[] CardSectionStartRows =
    [
        (BarclaysPlatinumVisa8003StartRow, "Platinum Visa 8003"),
        (BarclaysPlatinumVisa6007StartRow, "Platinum Visa 6007"),
        (ChaseMaster4023StartRow, "Chase Master 4023"),
        (BaAmexStartRow, "BA Amex"),
    ];

    public static IReadOnlyList<Expense> Import(
        IXLWorksheet sheet, int year, int month, DateOnly today, ImportReport report,
        IReadOnlyCollection<Bank> banks, IReadOnlyCollection<CreditCardEntity> creditCards)
    {
        var (descriptionColumn, categoryColumn) = ResolveColumns(sheet);
        var lastRow = sheet.LastRowUsed()?.RowNumber() ?? 1;
        var expenses = new List<Expense>();
        var cardsByName = creditCards.ToDictionary(c => c.Name, c => c, StringComparer.OrdinalIgnoreCase);

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
            var cardName = ResolveCardTag(row, year, month, today, rawPaymentSourceTag);
            Bank? paymentSourceBank = null;
            CreditCardEntity? creditCard = null;
            if (cardName is null)
            {
                var paymentSourceName = ResolvePaymentSource(rawPaymentSourceTag);
                paymentSourceBank = banks.FirstOrDefault(b => b.Name == paymentSourceName);
            }
            else if (!cardsByName.TryGetValue(cardName, out creditCard))
            {
                report.RowFlagged(
                    sheet.Name, row, "Card", cardName,
                    $"No seeded credit card matches inferred card name '{cardName}' - expense not imported");
                continue;
            }

            expenses.Add(Expense.Create(date, description, value.Value, category, paymentSourceBank, creditCard));
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

    private static string ResolvePaymentSource(string? tag) =>
        tag?.Trim().ToUpperInvariant() switch
        {
            "T" => "Trading212",
            "C" => "Chase",
            _ => "Barclays",
        };

    private static string? ResolveCardTag(int row, int year, int month, DateOnly today, string? rawPaymentSourceTag)
    {
        var isCardSectionEligible = string.IsNullOrWhiteSpace(rawPaymentSourceTag) || IsCreditCardMarker(rawPaymentSourceTag);
        if (!isCardSectionEligible)
        {
            return null;
        }

        var tabMonth = new DateOnly(year, month, 1);
        var currentMonth = new DateOnly(today.Year, today.Month, 1);
        if (tabMonth < currentMonth)
        {
            return null;
        }

        string? cardName = null;
        foreach (var (startRow, name) in CardSectionStartRows)
        {
            if (row >= startRow)
            {
                cardName = name;
            }
        }

        return cardName;
    }

    private static bool IsCreditCardMarker(string? tag) =>
        string.Equals(tag?.Trim(), "X", StringComparison.OrdinalIgnoreCase);
}
