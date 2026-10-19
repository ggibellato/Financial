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

        try
        {
            return JsonNode.Parse(File.ReadAllText(outputPath)) is JsonObject root
                ? CashFlowSerializerAdapter.VersionRefusal(outputPath, CashFlowSerializerAdapter.StoredVersion(root))
                : null;
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
