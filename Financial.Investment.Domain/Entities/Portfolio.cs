using System.Collections.Generic;
using System.Linq;

namespace Financial.Investment.Domain.Entities;

public class Portfolio
{
    public string Name { get; private set; } = string.Empty;

    /// <summary>Derived rather than stored: a persisted flag would be a second source of truth
    /// that could disagree with <see cref="Assets"/>.</summary>
    public bool IsEmpty => _assets.Count == 0;

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

    /// <summary>Matches by name ordinally, the same way the repository addresses an asset.</summary>
    public Asset? FindAsset(string name) => _assets.FirstOrDefault(asset => asset.Name == name);

    /// <summary>
    /// Detaches an asset without destroying it, so a caller can attach it elsewhere. Deliberately
    /// internal: an asset must never simply disappear, so only <see cref="Broker"/> - which
    /// completes the move by re-attaching it - can call this.
    /// </summary>
    internal bool RemoveAsset(string name)
    {
        var index = _assets.FindIndex(asset => asset.Name == name);
        if (index < 0)
        {
            return false;
        }

        _assets.RemoveAt(index);
        return true;
    }
}
