using System.Text.Json.Nodes;

namespace Financial.CashFlow.Infrastructure.Tools.CashFlowSpreadsheetImport.DataFileMigration;

internal static class DataFileMigrationJson
{
    internal static List<JsonObject> Objects(JsonObject root, string collection) =>
        (root[collection] as JsonArray ?? [])
            .Select(item => item as JsonObject ?? throw new InvalidDataException($"{collection} contains a value that is not an object: {item?.ToJsonString() ?? "null"}"))
            .ToList();

    internal static string IdText(JsonNode? id, string owner) =>
        id is JsonValue value && value.TryGetValue<string>(out var text)
            ? text
            : throw new InvalidDataException($"{owner} has an Id that is not a string: {id?.ToJsonString() ?? "null"}");
}
