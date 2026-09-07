using System.Text.Json;
using Financial.CashFlow.Application.Interfaces;
using Financial.CashFlow.Application.Models;

namespace Financial.CashFlow.Infrastructure.Persistence;

/// <summary>
/// Reads/writes the local Google Calendar credentials file - separate from
/// <c>data-cashflow.json</c>, so it is never loaded/saved through <c>ICashFlowRepository</c>.
/// Falls back to a default path under <c>data/</c> when unconfigured, so the rest of the app
/// never fails to start just because this optional integration hasn't been set up yet.
/// </summary>
public sealed class GoogleCalendarConnectionStore : IGoogleCalendarConnectionStore
{
    private const string DefaultRelativePath = "data/google-calendar-credentials.json";
    private const string TemporaryFileSuffix = ".tmp";

    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.General)
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };

    private readonly string _filePath;

    public GoogleCalendarConnectionStore(string? filePath)
    {
        var resolvedPath = string.IsNullOrWhiteSpace(filePath) ? DefaultRelativePath : filePath;
        _filePath = ResolveRelativeToBaseDirectory(resolvedPath);
    }

    public GoogleCalendarConnection? Load()
    {
        if (!File.Exists(_filePath))
        {
            return null;
        }

        var json = File.ReadAllText(_filePath);
        return JsonSerializer.Deserialize<GoogleCalendarConnection>(json, SerializerOptions);
    }

    public void Save(GoogleCalendarConnection connection)
    {
        ArgumentNullException.ThrowIfNull(connection);

        var directory = Path.GetDirectoryName(_filePath);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var json = JsonSerializer.Serialize(connection, SerializerOptions);
        var stagedPath = _filePath + TemporaryFileSuffix;
        File.WriteAllText(stagedPath, json);
        File.Move(stagedPath, _filePath, overwrite: true);
    }

    public void Delete()
    {
        if (File.Exists(_filePath))
        {
            File.Delete(_filePath);
        }
    }

    private static string ResolveRelativeToBaseDirectory(string path) =>
        Path.IsPathRooted(path) ? path : Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, path));
}
