using Financial.Application.Interfaces;
using System;
using System.IO;
using System.Threading.Tasks;

namespace Financial.Infrastructure.Persistence;

public sealed class LocalJsonStorage : IJsonStorage
{
    public const string DataJsonFileConfigurationKey = "DataJsonFile";
    public const string DefaultDataFileName = "data.json";

    private readonly string _dataFilePath;

    public LocalJsonStorage(string? dataFilePath)
    {
        _dataFilePath = ResolveDataFilePath(dataFilePath);
    }

    public Task<string> ReadAsync()
    {
        if (!File.Exists(_dataFilePath))
        {
            throw new FileNotFoundException(
                $"Data file not found at '{_dataFilePath}'. Configure '{DataJsonFileConfigurationKey}' or place '{DefaultDataFileName}' in the application directory.",
                _dataFilePath);
        }

        return File.ReadAllTextAsync(_dataFilePath);
    }

    public Task WriteAsync(string json)
    {
        return File.WriteAllTextAsync(_dataFilePath, json);
    }

    private static string ResolveDataFilePath(string? dataFilePath)
    {
        var resolvedPath = string.IsNullOrWhiteSpace(dataFilePath)
            ? Path.Combine(AppContext.BaseDirectory, DefaultDataFileName)
            : dataFilePath;

        if (Directory.Exists(resolvedPath))
        {
            resolvedPath = Path.Combine(resolvedPath, DefaultDataFileName);
        }

        if (!Path.IsPathRooted(resolvedPath))
        {
            resolvedPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, resolvedPath));
        }

        return resolvedPath;
    }
}
