using Financial.Model;
using System;
using System.Runtime.CompilerServices;
using System.IO;

[assembly: InternalsVisibleTo("Financial.Infrastructure.Tests")]
namespace FinancialModel.Infrastructure;

public class LocalJSONRepository : InvestmentsRepositoryBase
{

    public const string DataJsonPathConfigurationKey = "DataJsonPath";
    public const string DefaultDataFileName = "data.json";

    private readonly string _dataFilePath;

    public LocalJSONRepository() : this(null)
    {
    }

    public LocalJSONRepository(string? dataFilePath) : base(CreateInvestments(dataFilePath, out var resolvedPath))
    {
        _dataFilePath = resolvedPath;
    }

    private static Investments CreateInvestments(string? dataFilePath, out string resolvedPath)
    {
        resolvedPath = ResolveDataFilePath(dataFilePath);
        return LoadModel(resolvedPath);
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

    private static Investments LoadModel(string dataFilePath)
    {
        if (!File.Exists(dataFilePath))
        {
            throw new FileNotFoundException(
                $"Data file not found at '{dataFilePath}'. Configure '{DataJsonPathConfigurationKey}' or place '{DefaultDataFileName}' in the application directory.",
                dataFilePath);
        }

        var modelJson = File.ReadAllText(dataFilePath);
        return Investments.Deserialize(modelJson);
    }
}
