using System;
using System.Collections.Generic;
using System.Linq;
using Financial.Investment.Domain.Exceptions;

namespace Financial.Investment.Domain.Entities;

public class Broker
{
    public string Name { get; private set; } = string.Empty;
    public string Currency { get; private set; } = string.Empty;

    private List<Portfolio> _portfolios = new List<Portfolio>();
    public IReadOnlyCollection<Portfolio> Portfolios { get => _portfolios.AsReadOnly(); private set => SetPortfolios(value); }
    private void SetPortfolios(IReadOnlyCollection<Portfolio> data)
    {
        _portfolios.Clear();
        _portfolios.AddRange(data);
    }

    private Broker() { }

    private Broker(string name, string currency) : this()
    {
        Name = name;
        Currency = currency;
    }

    public static Broker Create(string name, string currency) => new(name, currency);

    public Portfolio AddPortfolio(string name)
    {
        var portfolio = Portfolios.FirstOrDefault(p => p.Name == name);
        if (portfolio is null)
        {
            portfolio = Portfolio.Create(name);
            _portfolios.Add(portfolio);
        }
        return portfolio;
    }

    /// <summary>Matches by name ordinally, the same way <see cref="AddPortfolio"/> resolves one.</summary>
    public Portfolio? FindPortfolio(string name) => _portfolios.FirstOrDefault(p => p.Name == name);

    /// <summary>
    /// Moves an asset between two of this broker's portfolios, creating the destination when the
    /// name is one this broker does not have yet.
    /// </summary>
    /// <remarks>
    /// The asset is relocated, never copied: the same instance is detached and re-attached, so its
    /// transactions, credits and price history travel with it and every figure derived from them
    /// stays exactly as it was. Nothing is recalculated because nothing is rebuilt.
    /// </remarks>
    /// <exception cref="ArgumentException">The destination name is blank or whitespace.</exception>
    /// <exception cref="KeyNotFoundException">The source portfolio, or the asset within it, does not exist.</exception>
    /// <exception cref="InvestmentRuleViolationException">
    /// The destination is the portfolio the asset already sits in, or it already holds an asset of
    /// that name.
    /// </exception>
    public void MoveAsset(string sourcePortfolioName, string assetName, string destinationPortfolioName)
    {
        var destinationName = NormalizeDestinationName(destinationPortfolioName);

        var source = FindPortfolio(sourcePortfolioName)
            ?? throw new KeyNotFoundException($"Portfolio \"{sourcePortfolioName}\" was not found under broker \"{Name}\".");

        var asset = source.FindAsset(assetName)
            ?? throw new KeyNotFoundException($"Asset \"{assetName}\" was not found in portfolio \"{sourcePortfolioName}\".");

        if (destinationName == sourcePortfolioName)
        {
            throw new InvestmentRuleViolationException(
                $"\"{sourcePortfolioName}\" is already the portfolio holding this asset.");
        }

        var destination = ResolveDestination(destinationName, assetName);

        source.RemoveAsset(assetName);
        destination.AddAsset(asset);
    }

    /// <summary>
    /// Removes a portfolio that holds no assets.
    /// </summary>
    /// <remarks>
    /// Only an empty one: a portfolio still holding assets is the only record of where they live,
    /// so removing it would take them with it. Emptying it first is a move, which is a separate,
    /// deliberate act.
    /// </remarks>
    /// <exception cref="KeyNotFoundException">No portfolio by that name.</exception>
    /// <exception cref="InvestmentRuleViolationException">The portfolio still holds assets.</exception>
    public bool RemoveEmptyPortfolio(string name)
    {
        var portfolio = FindPortfolio(name)
            ?? throw new KeyNotFoundException($"Portfolio \"{name}\" was not found under broker \"{Name}\".");

        if (!portfolio.IsEmpty)
        {
            throw new InvestmentRuleViolationException(
                $"Portfolio \"{name}\" still holds {portfolio.Assets.Count} asset(s). Only an empty portfolio can be deleted.");
        }

        return _portfolios.Remove(portfolio);
    }

    /// <summary>
    /// Trims a destination name and rejects a blank one. Shared with <see cref="Investments"/>,
    /// which applies the same naming rules when archiving across scopes.
    /// </summary>
    internal static string NormalizeDestinationName(string destinationPortfolioName) =>
        string.IsNullOrWhiteSpace(destinationPortfolioName)
            ? throw new ArgumentException("A destination portfolio name is required.", nameof(destinationPortfolioName))
            : destinationPortfolioName.Trim();

    /// <summary>
    /// Resolves the destination, creating it when the name is new. Every refusal is raised before
    /// anything is created: the in-memory graph is the process's only copy, so a portfolio created
    /// on the way to a throw would survive in memory and be written by the next unrelated save.
    /// </summary>
    /// <remarks>
    /// An existing portfolio is matched exactly, so selecting one behaves as it always has. Only a
    /// name that matches nothing creates a portfolio, and then a near-miss on case or padding is
    /// refused rather than silently producing two portfolios the tree renders identically.
    /// <para>
    /// Deliberately says nothing about where the asset is coming from. A move within one broker
    /// also has to refuse the portfolio the asset already sits in, but archiving across scopes does
    /// not - a Historic "ETF" is a perfectly good home for an asset leaving an Active "ETF". That
    /// check belongs to the operation, not to resolving a name.
    /// </para>
    /// </remarks>
    internal Portfolio ResolveDestination(string destinationPortfolioName, string assetName)
    {
        var existing = FindPortfolio(destinationPortfolioName);
        if (existing is not null)
        {
            return existing.FindAsset(assetName) is null
                ? existing
                : throw new InvestmentRuleViolationException(
                    $"Portfolio \"{existing.Name}\" already holds an asset named \"{assetName}\".");
        }

        var nearMiss = _portfolios.FirstOrDefault(
            p => string.Equals(p.Name.Trim(), destinationPortfolioName, StringComparison.OrdinalIgnoreCase));
        if (nearMiss is not null)
        {
            throw new InvestmentRuleViolationException(
                $"Broker \"{Name}\" already has a portfolio named \"{nearMiss.Name}\". Select it instead of creating another.");
        }

        return AddPortfolio(destinationPortfolioName);
    }
}
