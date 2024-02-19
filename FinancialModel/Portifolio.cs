using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Financial.Model;

public class Portifolio
{
    [JsonInclude]
    public string Name { get; private set; }

    private List<Asset> _assets = new List<Asset>();
    [JsonInclude]
    public IReadOnlyCollection<Asset> Assets { get { return _assets.AsReadOnly(); } }


    [JsonConstructor]
    private Portifolio()
    {
    }

    private Portifolio(string name) : this()
    {
        Name = name;
    }

    internal static Portifolio Create(string name) => new Portifolio(name);

    public void AddAsset(Asset asset)
    {
        _assets.Add(asset);
    }
}