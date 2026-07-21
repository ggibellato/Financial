using System.Collections.Generic;

namespace Financial.Investment.Domain.Entities;

public class Portfolio
{
    public string Name { get; private set; }

    private List<Asset> _assets = new List<Asset>();
    public IReadOnlyCollection<Asset> Assets { get => _assets.AsReadOnly(); private set => SetAssets(value); }
    private void SetAssets(IReadOnlyCollection<Asset> data)
    {
        _assets.Clear();
        _assets.AddRange(data);
    }

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
