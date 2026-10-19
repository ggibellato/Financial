using System.Text.Json.Nodes;

namespace Financial.CashFlow.Infrastructure.Tools.CashFlowSpreadsheetImport.DataFileMigration;

public interface IDataFileMigrationStep
{
    int FromVersion { get; }

    string Name { get; }

    string Apply(JsonObject root);
}
