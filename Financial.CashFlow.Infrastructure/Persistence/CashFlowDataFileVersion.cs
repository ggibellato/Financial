using System.Text.Json;
using System.Text.Json.Nodes;

namespace Financial.CashFlow.Infrastructure.Persistence;

public static class CashFlowDataFileVersion
{
    public const int Current = 2;
    public const string Property = "Version";

    public static int StoredIn(JsonObject root)
    {
        ArgumentNullException.ThrowIfNull(root);

        var version = root[Property];
        if (version is null)
        {
            return 1;
        }

        return version.GetValueKind() == JsonValueKind.Number && version.AsValue().TryGetValue<int>(out var stored)
            ? stored
            : throw new InvalidDataException($"{Property} must be a whole number, but is {version.ToJsonString()}.");
    }

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
