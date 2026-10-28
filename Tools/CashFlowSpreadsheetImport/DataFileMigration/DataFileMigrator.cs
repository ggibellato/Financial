using System.Text.Json;
using System.Text.Json.Nodes;
using Financial.CashFlow.Infrastructure.Persistence;

namespace Financial.CashFlow.Infrastructure.Tools.CashFlowSpreadsheetImport.DataFileMigration;

public sealed class DataFileMigrator
{
    private readonly IReadOnlyList<IDataFileMigrationStep> _steps;
    private readonly int _currentVersion;
    private readonly Func<string, string> _loadAndSave;

    public DataFileMigrator(IReadOnlyList<IDataFileMigrationStep> steps, int currentVersion, Func<string, string> loadAndSave)
    {
        _steps = steps ?? throw new ArgumentNullException(nameof(steps));
        _currentVersion = currentVersion;
        _loadAndSave = loadAndSave ?? throw new ArgumentNullException(nameof(loadAndSave));
    }

    public static DataFileMigrator CreateDefault()
    {
        var serializer = new CashFlowSerializerAdapter();
        return new DataFileMigrator(
            [new V1ToV2BanksToAccounts(), new V2ToV3CardsToAccounts(), new V3ToV4PotsAccounts()],
            CashFlowDataFileVersion.Current,
            json => serializer.Serialize(serializer.Deserialize(json)));
    }

    public DataFileMigrationResult Migrate(string json, string sourceName)
    {
        var root = ParseRoot(json, sourceName);
        var storedVersion = CashFlowDataFileVersion.StoredIn(root);

        if (storedVersion > _currentVersion)
        {
            throw new InvalidDataException(CashFlowDataFileVersion.Refusal(sourceName, storedVersion, _currentVersion));
        }

        if (storedVersion == _currentVersion)
        {
            return new DataFileMigrationResult(storedVersion, storedVersion, json, []);
        }

        var summaries = new List<string>();
        for (var version = storedVersion; version < _currentVersion; version++)
        {
            var step = StepFrom(version);
            summaries.Add(ApplyStep(step, root));
            CashFlowDataFileVersion.Stamp(root, version + 1);
        }

        return new DataFileMigrationResult(storedVersion, _currentVersion, LoadAndSave(root), summaries);
    }

    private static JsonObject ParseRoot(string json, string sourceName)
    {
        try
        {
            return JsonNode.Parse(json) as JsonObject
                ?? throw new InvalidDataException($"{sourceName} is not a JSON object.");
        }
        catch (JsonException ex)
        {
            throw new InvalidDataException($"{sourceName} is not valid JSON: {ex.Message}", ex);
        }
    }

    private IDataFileMigrationStep StepFrom(int version) =>
        _steps.SingleOrDefault(step => step.FromVersion == version)
        ?? throw new InvalidDataException($"No migration step from version {version} to {version + 1}.");

    private static string ApplyStep(IDataFileMigrationStep step, JsonObject root)
    {
        try
        {
            return step.Apply(root);
        }
        catch (InvalidDataException ex)
        {
            throw new InvalidDataException($"Migration step {step.Name} failed: {ex.Message}", ex);
        }
    }

    private string LoadAndSave(JsonObject root)
    {
        try
        {
            return _loadAndSave(root.ToJsonString());
        }
        catch (Exception ex) when (ex is JsonException or InvalidDataException or ArgumentException or InvalidOperationException)
        {
            throw new InvalidDataException($"The migrated file does not load: {ex.Message}", ex);
        }
    }
}
