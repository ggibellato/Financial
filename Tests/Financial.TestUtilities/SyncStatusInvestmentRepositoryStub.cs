using Financial.Investment.Application.Enums;
using Financial.Investment.Application.Interfaces;
using Financial.Investment.Domain.Entities;
using Financial.Shared.Infrastructure.Sync;

namespace Financial.TestUtilities;

/// <summary>
/// An <see cref="IInvestmentRepository"/> whose only implemented behavior is reporting/flushing sync status -
/// every CRUD member throws <see cref="NotImplementedException"/>. For tests that need a repository
/// double implementing <see cref="ISyncStatusProvider"/> specifically (shutdown-flush wiring, the
/// sync-status endpoint); for the "repository is NOT an ISyncStatusProvider" case, use
/// <see cref="StubInvestmentRepository"/> instead, which deliberately doesn't implement it.
/// </summary>
public sealed class SyncStatusInvestmentRepositoryStub : IInvestmentRepository, ISyncStatusProvider
{
    public SyncStatus StatusToReturn { get; set; } = new(SyncState.Idle, null, null);

    public int FlushAsyncCallCount { get; private set; }

    public SyncStatus GetStatus() => StatusToReturn;

    public Task FlushAsync()
    {
        FlushAsyncCallCount++;
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync() => throw new NotImplementedException();
    public IEnumerable<Asset> GetAssetsByBroker(string name, InvestmentScope scope = InvestmentScope.Active) => throw new NotImplementedException();
    public IEnumerable<Asset> GetAssetsByBrokerPortfolio(string broker, string portfolio, InvestmentScope scope = InvestmentScope.Active) => throw new NotImplementedException();
    public IEnumerable<Broker> GetBrokerList(InvestmentScope scope = InvestmentScope.Active) => throw new NotImplementedException();
    public Asset? GetAsset(string brokerName, string portfolioName, string assetName, InvestmentScope scope = InvestmentScope.Active) => throw new NotImplementedException();
}
