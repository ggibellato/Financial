using Financial.Investment.Application.Enums;
using Financial.Investment.Application.Interfaces;
using Financial.Investment.Domain.Entities;

namespace Financial.TestUtilities;

/// <summary>
/// Shared in-memory <see cref="IInvestmentRepository"/> test double. Three ways to configure a scenario:
/// set <see cref="Broker"/> (and/or <see cref="Brokers"/>) to have every query method derive its
/// answer from that real domain object graph, leave them null and set the flat
/// <see cref="AssetsByBroker"/>/<see cref="AssetsByBrokerPortfolio"/>/<see cref="Asset"/> fields
/// directly for tests that don't need a full broker/portfolio/asset graph, or set
/// <see cref="Investments"/> for the operations that span both scopes.
/// </summary>
public sealed class StubInvestmentRepository : IInvestmentRepository
{
    public Broker? Broker { get; set; }
    public IEnumerable<Broker>? Brokers { get; set; }

    /// <summary>
    /// The aggregate root, for archiving - the one operation that reaches both scopes. Set it and
    /// every query answers from it, scope and all. Left null, as every pre-existing test leaves it,
    /// the stub behaves exactly as it always did and <see cref="GetInvestments"/> is the only member
    /// that objects.
    /// </summary>
    public Investments? Investments { get; set; }

    public IEnumerable<Asset> AssetsByBroker { get; set; } = [];
    public IEnumerable<Asset> AssetsByBrokerPortfolio { get; set; } = [];
    public Asset? Asset { get; set; }

    /// <summary>Counts persisted writes, not calls: a mutation that reports no change
    /// still enters <see cref="ApplyAndSaveAsync"/> but must not write.</summary>
    public int WriteCallCount { get; private set; }
    public InvestmentScope? LastGetAssetsByBrokerScope { get; private set; }
    public InvestmentScope? LastGetAssetsByBrokerPortfolioScope { get; private set; }
    public InvestmentScope? LastGetBrokerListScope { get; private set; }

    public StubInvestmentRepository()
    {
    }

    /// <summary>Convenience constructor for tests that only need <see cref="GetBrokerList"/> to return a fixed list.</summary>
    public StubInvestmentRepository(IEnumerable<Broker> brokers)
    {
        Brokers = brokers.ToList();
    }

    public IEnumerable<Asset> GetAssetsByBroker(string name, InvestmentScope scope = InvestmentScope.Active)
    {
        LastGetAssetsByBrokerScope = scope;

        if (Investments is not null)
        {
            return ScopedBroker(name, scope)?.Portfolios.SelectMany(p => p.Assets) ?? [];
        }

        return Broker is not null ? Broker.Portfolios.SelectMany(p => p.Assets) : AssetsByBroker;
    }

    public IEnumerable<Asset> GetAssetsByBrokerPortfolio(string broker, string portfolio, InvestmentScope scope = InvestmentScope.Active)
    {
        LastGetAssetsByBrokerPortfolioScope = scope;

        if (Investments is not null)
        {
            return ScopedBroker(broker, scope)?.FindPortfolio(portfolio)?.Assets ?? [];
        }

        return Broker is not null
            ? Broker.Portfolios.FirstOrDefault(p => p.Name == portfolio)?.Assets ?? []
            : AssetsByBrokerPortfolio;
    }

    public IEnumerable<Broker> GetBrokerList(InvestmentScope scope = InvestmentScope.Active)
    {
        LastGetBrokerListScope = scope;

        if (Investments is not null)
        {
            return scope == InvestmentScope.Historic ? Investments.HistoricBrokers : Investments.ActiveBrokers;
        }

        return Brokers ?? (Broker is not null ? [Broker] : []);
    }

    public Asset? GetAsset(string brokerName, string portfolioName, string assetName, InvestmentScope scope = InvestmentScope.Active)
    {
        if (Investments is not null)
        {
            return ScopedBroker(brokerName, scope)?.FindPortfolio(portfolioName)?.FindAsset(assetName);
        }

        return Broker is not null
            ? Broker.Portfolios.FirstOrDefault(p => p.Name == portfolioName)?.Assets.FirstOrDefault(a => a.Name == assetName)
            : Asset;
    }

    public Investments GetInvestments() =>
        Investments ?? throw new InvalidOperationException(
            $"This test reached the aggregate root; set {nameof(StubInvestmentRepository)}.{nameof(Investments)} to the graph it should see.");

    /// <summary>Runs the mutation for real - the delegate is where the change now lives, so a
    /// stub that only counted would silently stop exercising it.</summary>
    public Task<bool> ApplyAndSaveAsync(Func<bool> applyChanges)
    {
        if (!applyChanges())
        {
            return Task.FromResult(false);
        }

        WriteCallCount++;
        return Task.FromResult(true);
    }

    private Broker? ScopedBroker(string brokerName, InvestmentScope scope) =>
        scope == InvestmentScope.Historic
            ? Investments!.FindHistoricBroker(brokerName)
            : Investments!.FindActiveBroker(brokerName);
}
