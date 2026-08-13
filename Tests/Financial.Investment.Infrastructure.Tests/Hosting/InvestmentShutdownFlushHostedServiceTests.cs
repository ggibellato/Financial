using Financial.Investment.Application.Enums;
using Financial.Investment.Application.Interfaces;
using Financial.Investment.Domain.Entities;
using Financial.Investment.Infrastructure.Hosting;
using Financial.Shared.Infrastructure.Sync;
using FluentAssertions;

namespace Financial.Investment.Infrastructure.Tests.Hosting;

public class InvestmentShutdownFlushHostedServiceTests
{
    [Fact]
    public async Task StopAsync_WhenRepositoryIsASyncStatusProvider_CallsFlushAsync()
    {
        var repository = new SyncStatusStubRepository();
        var hostedService = new InvestmentShutdownFlushHostedService(repository);

        await hostedService.StopAsync(CancellationToken.None);

        repository.FlushAsyncCallCount.Should().Be(1);
    }

    [Fact]
    public async Task StopAsync_WhenRepositoryIsNotASyncStatusProvider_CompletesWithoutError()
    {
        var repository = new PlainStubRepository();
        var hostedService = new InvestmentShutdownFlushHostedService(repository);

        var act = async () => await hostedService.StopAsync(CancellationToken.None);

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public void Constructor_WithNullRepository_Throws()
    {
        Action act = () => new InvestmentShutdownFlushHostedService(null!);

        act.Should().Throw<ArgumentNullException>().WithParameterName("repository");
    }

    private sealed class SyncStatusStubRepository : IRepository, ISyncStatusProvider
    {
        internal int FlushAsyncCallCount { get; private set; }

        public SyncStatus GetStatus() => new(SyncState.Idle, null, null);

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

    private sealed class PlainStubRepository : IRepository
    {
        public Task SaveChangesAsync() => throw new NotImplementedException();
        public IEnumerable<Asset> GetAssetsByBroker(string name, InvestmentScope scope = InvestmentScope.Active) => throw new NotImplementedException();
        public IEnumerable<Asset> GetAssetsByBrokerPortfolio(string broker, string portfolio, InvestmentScope scope = InvestmentScope.Active) => throw new NotImplementedException();
        public IEnumerable<Broker> GetBrokerList(InvestmentScope scope = InvestmentScope.Active) => throw new NotImplementedException();
        public Asset? GetAsset(string brokerName, string portfolioName, string assetName, InvestmentScope scope = InvestmentScope.Active) => throw new NotImplementedException();
    }
}
