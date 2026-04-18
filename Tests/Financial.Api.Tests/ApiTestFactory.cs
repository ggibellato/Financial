using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.IO;

namespace Financial.Api.Tests;

internal sealed class ApiTestFactory : WebApplicationFactory<Program>
{
    private readonly string _dataFilePath;
    private bool _disposed;

    public ApiTestFactory()
    {
        _dataFilePath = CreateTempDataFile();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((context, config) =>
        {
            var settings = new Dictionary<string, string?>
            {
                ["Repository:Provider"] = "LocalJson",
                ["DataJsonFile"] = _dataFilePath
            };
            config.AddInMemoryCollection(settings);
        });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (disposing && !_disposed)
        {
            _disposed = true;
            TryDeleteTempDataFile();
        }
    }

    private static string CreateTempDataFile()
    {
        var tempPath = Path.Combine(Path.GetTempPath(), $"financial-api-{Guid.NewGuid():N}.json");
        File.Copy(TestDataPaths.DataJsonFile, tempPath, true);
        return tempPath;
    }

    private void TryDeleteTempDataFile()
    {
        try
        {
            if (File.Exists(_dataFilePath))
            {
                File.Delete(_dataFilePath);
            }
        }
        catch (IOException ex)
        {
            Console.Error.WriteLine($"Failed to delete temp data file '{_dataFilePath}': {ex.Message}");
        }
        catch (UnauthorizedAccessException ex)
        {
            Console.Error.WriteLine($"Failed to delete temp data file '{_dataFilePath}': {ex.Message}");
        }
    }
}
