using Financial.Shared.Abstractions.Persistence;

namespace Financial.Shared.Infrastructure.Persistence;

public sealed class LocalJsonStorage : IJsonStorage
{
    public const string DefaultDataFileName = "data.json";

    /// <summary>Appended to the data file's own path, so the staged copy always lands on the same
    /// volume as its target - File.Move cannot rename across volumes, and in Docker the data
    /// directory is a bind mount.</summary>
    internal const string TemporaryFileSuffix = ".tmp";

    private readonly string _dataFilePath;
    private readonly string _defaultFileName;

    public LocalJsonStorage(string? dataFilePath, string defaultFileName = DefaultDataFileName)
    {
        _defaultFileName = defaultFileName;
        _dataFilePath = ResolveDataFilePath(dataFilePath, defaultFileName);
    }

    public Task<string> ReadAsync()
    {
        if (!File.Exists(_dataFilePath))
        {
            throw new FileNotFoundException(
                $"Data file not found at '{_dataFilePath}'. Configure the data file path, or place '{_defaultFileName}' in the application directory.",
                _dataFilePath);
        }

        return File.ReadAllTextAsync(_dataFilePath);
    }

    /// <summary>
    /// Staged to a sibling file and renamed into place, because a direct write truncates the
    /// document before refilling it: a crash, a kill, or a power loss part-way through left a
    /// half-written data file that then failed to load at startup. A rename is atomic, so the
    /// file on disk is only ever the whole previous document or the whole new one.
    /// </summary>
    public async Task WriteAsync(string json)
    {
        var stagedPath = _dataFilePath + TemporaryFileSuffix;
        try
        {
            await File.WriteAllTextAsync(stagedPath, json).ConfigureAwait(false);
            File.Move(stagedPath, _dataFilePath, overwrite: true);
        }
        catch
        {
            DiscardStagedFile(stagedPath);
            throw;
        }
    }

    /// <summary>The write already failed, so a failure to tidy up must not replace the exception
    /// explaining why.</summary>
    private static void DiscardStagedFile(string stagedPath)
    {
        try
        {
            if (File.Exists(stagedPath))
            {
                File.Delete(stagedPath);
            }
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
    }

    private static string ResolveDataFilePath(string? dataFilePath, string defaultFileName)
    {
        var resolvedPath = string.IsNullOrWhiteSpace(dataFilePath)
            ? Path.Combine(AppContext.BaseDirectory, defaultFileName)
            : dataFilePath;

        if (Directory.Exists(resolvedPath))
        {
            resolvedPath = Path.Combine(resolvedPath, defaultFileName);
        }

        return PathResolution.ResolveRelativeToBaseDirectory(resolvedPath);
    }
}
