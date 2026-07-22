using ClosedXML.Excel;
using Financial.CashFlow.Domain.Entities;
using Financial.CashFlow.Domain.Enums;
using Financial.CashFlow.Infrastructure.Integrations.CashFlowSpreadsheetImport.Parsing;

namespace Financial.CashFlow.Infrastructure.Integrations.CashFlowSpreadsheetImport.SheetImporters;

/// <summary>
/// Parses the single continuous "Mensais" sheet into <see cref="RecurringBillTemplate"/>
/// entities (one per Brasil/UK row) plus exactly one <see cref="RecurringBillInstance"/> per
/// template. The sheet only ever shows a live current-state snapshot — the month label at the
/// top has no year, so the caller supplies the actual year/month to stamp on every instance
/// (the year/month the import itself is run in), rather than parsing an ambiguous cell.
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

    public static (IReadOnlyList<RecurringBillTemplate> Templates, IReadOnlyList<RecurringBillInstance> Instances) Import(
        IXLWorksheet sheet, int currentYear, int currentMonth)
    {
        var templates = new List<RecurringBillTemplate>();
        var instances = new List<RecurringBillInstance>();
        var lastRow = sheet.LastRowUsed()?.RowNumber() ?? 1;
        Area? currentArea = null;

        for (var row = 1; row <= lastRow; row++)
        {
            var dueDayCell = sheet.Cell(row, DueDayColumn);
            var descriptionCell = sheet.Cell(row, DescriptionColumn);

            if (dueDayCell.IsEmpty() && TryResolveAreaLabel(descriptionCell.GetString(), out var area))
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

            var template = RecurringBillTemplate.Create(dueDay, description, value, currentArea.Value, note, nitNumber, minimumWageValue);
            templates.Add(template);

            var status = ResolveStatus(sheet.Cell(row, StatusColumn).GetString());
            var instance = RecurringBillInstance.Create(template.Id, currentYear, currentMonth, value);
            instance.Update(status, value);
            instances.Add(instance);
        }

        return (templates, instances);
    }

    private static bool TryResolveAreaLabel(string label, out Area area)
    {
        var trimmed = label.Trim();
        if (string.Equals(trimmed, "Brasil", StringComparison.OrdinalIgnoreCase))
        {
            area = Area.Brasil;
            return true;
        }

        if (string.Equals(trimmed, "UK", StringComparison.OrdinalIgnoreCase))
        {
            area = Area.UK;
            return true;
        }

        area = default;
        return false;
    }

    private static BillStatus ResolveStatus(string? tag) =>
        tag?.Trim().ToUpperInvariant() switch
        {
            "A" => BillStatus.Scheduled,
            "X" => BillStatus.Paid,
            _ => BillStatus.Unset,
        };
}
