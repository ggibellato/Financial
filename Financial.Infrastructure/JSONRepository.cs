using Financial.Model;
using FinancialModel.Application;
using System.Reflection;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("Financial.Infrastructure.Tests")]
namespace FinancialModel.Infrastructure;

internal class JSONRepository : IRepository
{

    private Investments _investiments;

    public JSONRepository()
    {
        _investiments = LoadModel();
    }

    public List<string> GetAllAssetsFullName()
    {
        var investiments = LoadModel();
        var result = new List<string>();
        investiments.Brokers.ForEach(b =>
        {
            b.Portifolios.ForEach(p =>
            {
                p.Assets.ForEach(a =>
                {
                    result.Add($"{b.Name}/{p.Name}/{a.Name}");
                });
            });
        });
        return result;
    }

    public IEnumerable<Asset> GetAssetsByBroker(string name)
    {
        return _investiments.Brokers.Where(b => b.Name == name).SelectMany(b => b.Portifolios.SelectMany(p => p.Assets));
    }

    public IEnumerable<Asset> GetAssetsByPortifolio(string name)
    {
        return _investiments.Brokers.SelectMany(b => b.Portifolios.Where(p => p.Name == name).SelectMany(p => p.Assets));
    }

    public IEnumerable<Asset> GetAssetsByAssetName(string name)
    {
        return _investiments.Brokers.SelectMany(b => b.Portifolios.SelectMany(p => p.Assets.Where(a => a.Name == name)));
    }

    private Investments LoadModel()
    {
        var modelJson = LoadEmbeddedResource("Data.data.json");
        return Investments.Deserialize(modelJson);
    }

    static string LoadEmbeddedResource(string resourceName)
    {
        Assembly assembly = Assembly.GetExecutingAssembly();
        string fullResourceName = $"{assembly.GetName().Name}.{resourceName}";

        using Stream stream = assembly.GetManifestResourceStream(fullResourceName);
        using StreamReader reader = new(stream);
        return reader.ReadToEnd();
    }
}
