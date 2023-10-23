using Financial.Model;
using FinancialModel.Application;
using System.Reflection;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("Financial.Infrastructure.Tests")]
namespace FinancialModel.Infrastructure;

internal class JSONRepository : IRepository
{
    public List<string> GetAllAssets()
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
