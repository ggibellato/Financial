using Financial.Investment.Application.Enums;
using Financial.Investment.Domain.Entities;
using Financial.Investment.Infrastructure.Persistence;
using Financial.Shared.Infrastructure.Persistence;
using Financial.Shared.Infrastructure.Sync;
using Financial.Investment.Infrastructure.Repositories;
using FluentAssertions;

namespace Financial.Investment.Infrastructure.Tests.Repositories;

public class JsonRepositoryTests
{
    private readonly JSONRepository _sut = CreateRepository(TestDataPaths.DataJsonFile);

    private static JSONRepository CreateRepository(string dataFile)
    {
        var storage = new LocalJsonStorage(dataFile);
        var serializer = new InvestmentsSerializerAdapter();
        return new JSONRepository(InvestmentsLoader.LoadSync(storage, serializer), storage, serializer);
    }

    [Fact]
    public void Constructor_WithNullInvestments_Throws()
    {
        var storage = new LocalJsonStorage(TestDataPaths.DataJsonFile);
        var serializer = new InvestmentsSerializerAdapter();

        Action act = () => new JSONRepository(null!, storage, serializer);

        act.Should().Throw<ArgumentNullException>().WithParameterName("investments");
    }

    [Fact]
    public void Constructor_WithNullStorage_Throws()
    {
        var investments = InvestmentsLoader.LoadSync(new LocalJsonStorage(TestDataPaths.DataJsonFile), new InvestmentsSerializerAdapter());

        Action act = () => new JSONRepository(investments, null!, new InvestmentsSerializerAdapter());

        act.Should().Throw<ArgumentNullException>().WithParameterName("storage");
    }

    [Fact]
    public void Constructor_WithNullSerializer_Throws()
    {
        var storage = new LocalJsonStorage(TestDataPaths.DataJsonFile);
        var investments = InvestmentsLoader.LoadSync(storage, new InvestmentsSerializerAdapter());

        Action act = () => new JSONRepository(investments, storage, null!);

        act.Should().Throw<ArgumentNullException>().WithParameterName("serializer");
    }

    [Fact]
    public void GetStatus_WhenStorageIsNotASyncStatusProvider_ReturnsIdleWithNoError()
    {
        var status = ((ISyncStatusProvider)_sut).GetStatus();

        status.Should().Be(new SyncStatus(SyncState.Idle, null, null));
    }

    [Fact]
    public void GetStatus_WhenStorageIsASyncStatusProvider_DelegatesToIt()
    {
        var expectedStatus = new SyncStatus(SyncState.Failed, "Drive unreachable", null);
        var storage = new FakeSyncStatusStorage { Status = expectedStatus };
        var repository = new JSONRepository(Investments.Create(), storage, new InvestmentsSerializerAdapter());

        var status = ((ISyncStatusProvider)repository).GetStatus();

        status.Should().Be(expectedStatus);
    }

    [Fact]
    public async Task FlushAsync_WhenStorageIsNotASyncStatusProvider_CompletesWithoutError()
    {
        var act = async () => await ((ISyncStatusProvider)_sut).FlushAsync();

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task FlushAsync_WhenStorageIsASyncStatusProvider_DelegatesToIt()
    {
        var storage = new FakeSyncStatusStorage();
        var repository = new JSONRepository(Investments.Create(), storage, new InvestmentsSerializerAdapter());

        await ((ISyncStatusProvider)repository).FlushAsync();

        storage.FlushAsyncCallCount.Should().Be(1);
    }

    [Theory]
    [InlineData(null, 0)]
    [InlineData("", 0)]
    [InlineData("NOTEXIST", 0)]
    [InlineData("XPI", 1)]
    public void GetAssets_By_BrokerTest(string? name, int records)
    {
        var result = _sut.GetAssetsByBroker(name ?? string.Empty);
        result.Should().HaveCount(records);
    }

    [Fact]
    public void GetAssetsByBroker_DefaultScope_ReturnsActiveOnly()
    {
        var repository = CreateRepositoryWithBothScopes(out _, out _);

        var result = repository.GetAssetsByBroker("XPI");

        result.Should().ContainSingle().Which.Name.Should().Be("ACTIVE_ASSET");
    }

    [Fact]
    public void GetAssetsByBroker_HistoricScope_ReturnsHistoricOnly()
    {
        var repository = CreateRepositoryWithBothScopes(out _, out _);

        var result = repository.GetAssetsByBroker("XPI", InvestmentScope.Historic);

        result.Should().ContainSingle().Which.Name.Should().Be("HISTORIC_ASSET");
    }

    [Fact]
    public void GetBrokerList_ActiveAndHistoricScopes_ReturnIndependentLists()
    {
        var repository = CreateRepositoryWithBothScopes(out var activeBroker, out var historicBroker);

        var activeResult = repository.GetBrokerList();
        var historicResult = repository.GetBrokerList(InvestmentScope.Historic);

        activeResult.Should().ContainSingle().Which.Should().BeSameAs(activeBroker);
        historicResult.Should().ContainSingle().Which.Should().BeSameAs(historicBroker);
    }

    private static JSONRepository CreateRepositoryWithBothScopes(out Broker activeBroker, out Broker historicBroker)
    {
        var investments = Investments.Create();

        activeBroker = Broker.Create("XPI", "BRL");
        activeBroker.AddPortfolio("Default").AddAsset(Asset.Create("ACTIVE_ASSET", "ISIN1", "BVMF", "ACTIVE_ASSET"));
        investments.AddActiveBroker(activeBroker);

        historicBroker = Broker.Create("XPI", "BRL");
        historicBroker.AddPortfolio("Uncategorized").AddAsset(Asset.Create("HISTORIC_ASSET", "ISIN2", "BVMF", "HISTORIC_ASSET"));
        investments.AddHistoricBroker(historicBroker);

        var storage = new LocalJsonStorage(TestDataPaths.DataJsonFile);
        var serializer = new InvestmentsSerializerAdapter();
        return new JSONRepository(investments, storage, serializer);
    }

    private sealed class FakeSyncStatusStorage : IJsonStorage, ISyncStatusProvider
    {
        internal SyncStatus Status { get; set; } = new(SyncState.Idle, null, null);

        internal int FlushAsyncCallCount { get; private set; }

        public Task<string> ReadAsync() => Task.FromResult("{}");

        public Task WriteAsync(string json) => Task.CompletedTask;

        public SyncStatus GetStatus() => Status;

        public Task FlushAsync()
        {
            FlushAsyncCallCount++;
            return Task.CompletedTask;
        }
    }
}
