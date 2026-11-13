using System.Text.Json;
using System.Text.Json.Nodes;

namespace Financial.CashFlow.Infrastructure.Persistence;

public static class CashFlowDataFileVersion
{
    public const int Current = 5;
    public const string Property = "Version";

    public static int StoredIn(JsonObject root)
    {
        ArgumentNullException.ThrowIfNull(root);

        return root[Property] is { } version ? Read(JsonSerializer.SerializeToElement(version)) : 1;
    }

    public static int StoredIn(JsonElement root) =>
        root.TryGetProperty(Property, out var version) ? Read(version) : 1;

    private static int Read(JsonElement version) => version.ValueKind switch
    {
        JsonValueKind.Null => 1,
        JsonValueKind.Number when version.TryGetInt32(out var stored) => stored,
        _ => throw new InvalidDataException($"{Property} must be a whole number, but is {JsonSerializer.Serialize(version)}.")
    };

    public static void Stamp(JsonObject root, int version)
    {
        ArgumentNullException.ThrowIfNull(root);
        root[Property] = version;
    }

    public static string? Refusal(string sourceName, int storedVersion, int currentVersion = Current)
    {
        if (storedVersion == currentVersion)
        {
            return null;
        }

        var refusal = $"{sourceName} is version {storedVersion}; this build needs version {currentVersion}.";
        return storedVersion < currentVersion ? $"{refusal} Run the migrate command first." : refusal;
    }
}
