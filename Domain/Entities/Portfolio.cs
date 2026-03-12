using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Financial.Domain.Entities;

public class Portfolio
{
    [JsonInclude]
    public string Name { get; private set; }

    private List<Asset> _assets = new List<Asset>();
    [JsonInclude]
    public IReadOnlyCollection<Asset> Assets { get => _assets.AsReadOnly(); set => SetAssets(value); }
    private void SetAssets(IReadOnlyCollection<Asset> data)
    {
        _assets.Clear();
        _assets.AddRange(data);
    }

    [JsonConstructor]
    private Portfolio()
    {
    }

    private Portfolio(string name) : this()
    {
        Name = name;
    }

    internal static Portfolio Create(string name) => new Portfolio(name);

    public void AddAsset(Asset asset)
    {
        _assets.Add(asset);
    }
}
