namespace Financial.Shared.Infrastructure.Persistence;

public sealed class LocalJsonStorage : IJsonStorage
{
    public const string DefaultDataFileName = "data.json";

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

    public Task WriteAsync(string json)
    {
        return File.WriteAllTextAsync(_dataFilePath, json);
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
