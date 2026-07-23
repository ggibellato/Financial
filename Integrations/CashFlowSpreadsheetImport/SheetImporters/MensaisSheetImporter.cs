using ClosedXML.Excel;
using Financial.CashFlow.Application.Validation;
using Financial.CashFlow.Domain.Entities;
using Financial.CashFlow.Domain.Enums;
using Financial.CashFlow.Infrastructure.Integrations.CashFlowSpreadsheetImport.Parsing;

namespace Financial.CashFlow.Infrastructure.Integrations.CashFlowSpreadsheetImport.SheetImporters;

/// <summary>
/// Parses the single continuous "Mensais" sheet into <see cref="RecurringBill"/> entities
/// (one per Brasil/UK row), reading each row's status directly rather than stamping a
/// separate month-scoped instance.
/// </summary>
public static class MensaisSheetImporter
{
    private const int DueDayColumn = 1;
    private const int DescriptionColumn = 2;
    private const int ValueColumn = 3;
    private const int StatusColumn = 4;
    private const int NoteColumn = 5;
    private const int NitNumberColumn = 6;
    private const int MinimumWageValueColumn = 7;

    public static IReadOnlyList<RecurringBill> Import(IXLWorksheet sheet)
    {
        var bills = new List<RecurringBill>();
        var lastRow = sheet.LastRowUsed()?.RowNumber() ?? 1;
        Area? currentArea = null;

        for (var row = 1; row <= lastRow; row++)
        {
            var dueDayCell = sheet.Cell(row, DueDayColumn);
            var descriptionCell = sheet.Cell(row, DescriptionColumn);

            if (dueDayCell.IsEmpty() && AreaParser.TryParse(descriptionCell.GetString(), out var area))
            {
                currentArea = area;
                continue;
            }

            if (dueDayCell.IsEmpty() || descriptionCell.IsEmpty() || currentArea is null)
            {
                continue;
            }

            var dueDay = (int)dueDayCell.GetValue<double>();
            var description = descriptionCell.GetString();
            var value = NumericCellReader.TryRead(sheet.Cell(row, ValueColumn)) ?? 0;
            var note = sheet.Cell(row, NoteColumn).GetString();

            var nitCell = sheet.Cell(row, NitNumberColumn);
            var nitNumber = currentArea == Area.Brasil && !nitCell.IsEmpty() ? nitCell.GetString() : null;

            var minimumWageValue = currentArea == Area.Brasil
                ? NumericCellReader.TryRead(sheet.Cell(row, MinimumWageValueColumn))
                : null;

            var bill = RecurringBill.Create(dueDay, description, value, currentArea.Value, note, nitNumber, minimumWageValue);
            var status = ResolveStatus(sheet.Cell(row, StatusColumn).GetString());
            bill.Update(status, value);
            bills.Add(bill);
        }

        return bills;
    }

    private static BillStatus ResolveStatus(string? tag) =>
        tag?.Trim().ToUpperInvariant() switch
        {
            "A" => BillStatus.Scheduled,
            "X" => BillStatus.Paid,
            _ => BillStatus.Unset,
        };
}
