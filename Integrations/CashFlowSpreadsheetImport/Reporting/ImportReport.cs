namespace Financial.CashFlow.Infrastructure.Integrations.CashFlowSpreadsheetImport.Reporting;

public sealed record RowIssue(string SheetName, int Row, string Field, string RawValue, string Reason);

public sealed class ImportReport
{
    private readonly List<string> _importedSheets = new();
    private readonly List<(string Sheet, string Reason)> _skippedSheets = new();
    private readonly List<RowIssue> _rowIssues = new();
    private readonly List<string> _validationWarnings = new();

    public void SheetImported(string sheetName) => _importedSheets.Add(sheetName);

    public void SheetSkipped(string sheetName, string reason) => _skippedSheets.Add((sheetName, reason));

    public void RowFlagged(string sheetName, int row, string field, string rawValue, string reason) =>
        _rowIssues.Add(new RowIssue(sheetName, row, field, rawValue, reason));

    public void ValidationWarning(string message) => _validationWarnings.Add(message);

    public IReadOnlyList<string> ImportedSheets => _importedSheets;
    public IReadOnlyList<(string Sheet, string Reason)> SkippedSheets => _skippedSheets;
    public IReadOnlyList<RowIssue> RowIssues => _rowIssues;
    public IReadOnlyList<string> ValidationWarnings => _validationWarnings;

    public string Render()
    {
        var lines = new List<string>
        {
            "=== CashFlow Historical Import Report ===",
            $"Sheets imported: {_importedSheets.Count}",
            $"Sheets skipped: {_skippedSheets.Count}",
            $"Row issues flagged: {_rowIssues.Count}",
            $"Validation warnings: {_validationWarnings.Count}",
            string.Empty,
        };

        if (_importedSheets.Count > 0)
        {
            lines.Add("--- Imported sheets ---");
            lines.Add($"  {string.Join(", ", _importedSheets)}");
            lines.Add(string.Empty);
        }

        if (_skippedSheets.Count > 0)
        {
            lines.Add("--- Skipped sheets ---");
            foreach (var (sheet, reason) in _skippedSheets)
            {
                lines.Add($"  {sheet}: {reason}");
            }

            lines.Add(string.Empty);
        }

        if (_rowIssues.Count > 0)
        {
            lines.Add("--- Row issues ---");
            foreach (var issue in _rowIssues)
            {
                lines.Add($"  [{issue.SheetName}] row {issue.Row}, {issue.Field}='{issue.RawValue}': {issue.Reason}");
            }

            lines.Add(string.Empty);
        }

        if (_validationWarnings.Count > 0)
        {
            lines.Add("--- Resumo validation warnings ---");
            foreach (var warning in _validationWarnings)
            {
                lines.Add($"  {warning}");
            }

            lines.Add(string.Empty);
        }

        return string.Join(Environment.NewLine, lines);
    }
}
