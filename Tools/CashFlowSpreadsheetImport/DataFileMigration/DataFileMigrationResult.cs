namespace Financial.CashFlow.Infrastructure.Tools.CashFlowSpreadsheetImport.DataFileMigration;

public sealed record DataFileMigrationResult(int FromVersion, int ToVersion, string Json, IReadOnlyList<string> StepSummaries)
{
    public bool WasAlreadyCurrent => FromVersion == ToVersion;
}
