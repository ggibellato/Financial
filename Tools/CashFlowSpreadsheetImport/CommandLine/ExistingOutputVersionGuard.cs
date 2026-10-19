using System.Text.Json;
using System.Text.Json.Nodes;
using Financial.CashFlow.Infrastructure.Persistence;

namespace Financial.CashFlow.Infrastructure.Tools.CashFlowSpreadsheetImport.CommandLine;

public static class ExistingOutputVersionGuard
{
    public static string? RefusalFor(string outputPath)
    {
        if (!File.Exists(outputPath))
        {
            return null;
        }

        JsonObject? root;
        try
        {
            root = JsonNode.Parse(File.ReadAllText(outputPath)) as JsonObject;
        }
        catch (JsonException)
        {
            return null;
        }

        if (root is null)
        {
            return null;
        }

        var storedVersion = CashFlowSerializerAdapter.StoredVersion(root);
        if (storedVersion == CashFlowSerializerAdapter.CurrentVersion)
        {
            return null;
        }

        var refusal = $"{outputPath} is version {storedVersion}; this build needs version {CashFlowSerializerAdapter.CurrentVersion}.";
        return storedVersion < CashFlowSerializerAdapter.CurrentVersion
            ? $"{refusal} Run the migrate command first."
            : refusal;
    }
}
