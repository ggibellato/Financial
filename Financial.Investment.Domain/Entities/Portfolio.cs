using System.Collections.Generic;
using System.Linq;
using Financial.Investment.Domain.Exceptions;

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

    /// <summary>Uniqueness against sibling portfolios is the caller's responsibility (<see cref="Broker.RenamePortfolio"/>),
    /// the same division of ownership as <see cref="Create"/>.</summary>
    internal void Rename(string name)
    {
        Name = name;
    }

    public void AddAsset(Asset asset)
    {
        _assets.Add(asset);
    }

    /// <summary>
    /// Registers a brand-new asset, rejecting a name already in use under this portfolio.
    /// </summary>
    /// <remarks>
    /// Distinct from <see cref="AddAsset"/>, which the existing Move/Archive workflows rely on to
    /// re-attach a relocated asset without a duplicate check (the asset is leaving one portfolio for
    /// another, so the source can never collide with itself). An explicit Admin create must refuse a
    /// same-named asset instead, so this is a separate method, the same split <see cref="Broker.CreatePortfolio"/>
    /// makes against <see cref="Broker.AddPortfolio"/>.
    /// </remarks>
    /// <exception cref="InvestmentRuleViolationException">An asset by that name already exists in this portfolio.</exception>
    public void RegisterAsset(Asset asset)
    {
        if (FindAsset(asset.Name) is not null)
        {
            throw new InvestmentRuleViolationException(
                $"Portfolio \"{Name}\" already has an asset named \"{asset.Name}\".");
        }

        _assets.Add(asset);
    }

    /// <summary>
    /// Updates an existing asset's identity fields, rejecting a new name already in use by a
    /// different asset in this portfolio.
    /// </summary>
    /// <remarks>Works regardless of the asset's transaction history: identity is independent of
    /// position. Renaming to the asset's own current name is a no-op success rather than a
    /// collision with itself.</remarks>
    /// <exception cref="KeyNotFoundException">No asset by <paramref name="currentName"/> exists.</exception>
    /// <exception cref="InvestmentRuleViolationException">The new name is already in use by another asset.</exception>
    public Asset UpdateAssetIdentity(
        string currentName,
        string name,
        string isin,
        string exchange,
        string ticker,
        CountryCode country,
        string localTypeCode,
        GlobalAssetClass assetClass)
    {
        var asset = FindAsset(currentName)
            ?? throw new KeyNotFoundException($"Asset \"{currentName}\" was not found in portfolio \"{Name}\".");

        if (name != currentName && FindAsset(name) is not null)
        {
            throw new InvestmentRuleViolationException(
                $"Portfolio \"{Name}\" already has an asset named \"{name}\".");
        }

        asset.UpdateIdentity(name, isin, exchange, ticker, country, localTypeCode, assetClass);
        return asset;
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
