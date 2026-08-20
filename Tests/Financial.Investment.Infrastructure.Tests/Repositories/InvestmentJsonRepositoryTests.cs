using Financial.Investment.Application.Enums;
using Financial.Investment.Domain.Entities;
using Financial.Investment.Infrastructure.Persistence;
using Financial.Shared.Infrastructure.Persistence;
using Financial.Shared.Infrastructure.Sync;
using Financial.Investment.Infrastructure.Repositories;
using Financial.TestUtilities;
using FluentAssertions;
using System.IO;

namespace Financial.Investment.Infrastructure.Tests.Repositories;

public class InvestmentJsonRepositoryTests
{
    /// <summary>Every test round-trips through the same stateless serializer; only the backing
    /// storage differs. Static because the repository factories below are static too.</summary>
    private static readonly InvestmentsSerializerAdapter Serializer = new();

    /// <summary>Storage over the shared read-only test data file, which several tests open verbatim.</summary>
    private static readonly LocalJsonStorage TestDataStorage = new(TestDataPaths.DataJsonFile);

    private readonly InvestmentJsonRepository _sut = CreateRepository(TestDataPaths.DataJsonFile);

    private static InvestmentJsonRepository CreateRepository(string dataFile)
    {
        var storage = new LocalJsonStorage(dataFile);
        return new InvestmentJsonRepository(InvestmentsLoader.LoadSync(storage, Serializer), storage, Serializer);
    }

    [Fact]
    public void Constructor_WithNullInvestments_Throws()
    {
        Action act = () => new InvestmentJsonRepository(null!, TestDataStorage, Serializer);

        act.Should().Throw<ArgumentNullException>().WithParameterName("investments");
    }

    [Fact]
    public void Constructor_WithNullStorage_Throws()
    {
        var investments = InvestmentsLoader.LoadSync(TestDataStorage, Serializer);

        Action act = () => new InvestmentJsonRepository(investments, null!, new InvestmentsSerializerAdapter());

        act.Should().Throw<ArgumentNullException>().WithParameterName("storage");
    }

    [Fact]
    public void Constructor_WithNullSerializer_Throws()
    {
        var investments = InvestmentsLoader.LoadSync(TestDataStorage, new InvestmentsSerializerAdapter());

        Action act = () => new InvestmentJsonRepository(investments, TestDataStorage, null!);

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
        var repository = new InvestmentJsonRepository(Investments.Create(), storage, new InvestmentsSerializerAdapter());

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
        var repository = new InvestmentJsonRepository(Investments.Create(), storage, new InvestmentsSerializerAdapter());

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

    [Fact]
    public async Task ApplyAndSaveAsync_WhenApplyReportsAChange_WritesTheSerializedDocument()
    {
        var storage = new RecordingJsonStorage();
        var repository = new InvestmentJsonRepository(Investments.Create(), storage, Serializer);

        var saved = await repository.ApplyAndSaveAsync(() => true);

        saved.Should().BeTrue();
        storage.WriteCount.Should().Be(1);
    }

    [Fact]
    public async Task ApplyAndSaveAsync_WhenApplyReportsNoChange_WritesNothing()
    {
        var storage = new RecordingJsonStorage();
        var repository = new InvestmentJsonRepository(Investments.Create(), storage, Serializer);

        var saved = await repository.ApplyAndSaveAsync(() => false);

        saved.Should().BeFalse();
        storage.WriteCount.Should().Be(0);
    }

    [Fact]
    public async Task ApplyAndSaveAsync_WithNullApplyChanges_Throws()
    {
        var repository = new InvestmentJsonRepository(Investments.Create(), new RecordingJsonStorage(), Serializer);

        var act = async () => await repository.ApplyAndSaveAsync(null!);

        await act.Should().ThrowAsync<ArgumentNullException>().WithParameterName("applyChanges");
    }

    /// <summary>
    /// The contract is not "one write at a time" but "one mutation at a time": a second caller's
    /// change must not be applied while the first caller's document is still being written, because
    /// writing serializes the whole graph.
    /// </summary>
    [Fact]
    public async Task ApplyAndSaveAsync_WhileAnotherSaveIsWriting_DoesNotRunItsMutation()
    {
        var storage = new BlockingJsonStorage();
        var repository = new InvestmentJsonRepository(Investments.Create(), storage, Serializer);
        var firstApplied = false;
        var secondApplied = false;

        var first = repository.ApplyAndSaveAsync(() => { firstApplied = true; return true; });
        await storage.WriteEntered.Task;

        var second = repository.ApplyAndSaveAsync(() => { secondApplied = true; return true; });
        await Task.WhenAny(second, Task.Delay(300));

        second.IsCompleted.Should().BeFalse("the first save still holds the gate");
        secondApplied.Should().BeFalse("a mutation must not run while the graph is being serialized");
        firstApplied.Should().BeTrue();

        storage.ReleaseWrite();
        await Task.WhenAll(first, second);

        secondApplied.Should().BeTrue();
        storage.WriteCount.Should().Be(2);
    }

    /// <summary>
    /// The repository is a singleton, so a gate left held by a failed write would hang every later
    /// save for the lifetime of the process instead of throwing.
    /// </summary>
    [Fact]
    public async Task ApplyAndSaveAsync_WhenTheWriteThrows_LeavesTheGateAvailable()
    {
        var storage = new RecordingJsonStorage { FailNextWrite = true };
        var repository = new InvestmentJsonRepository(Investments.Create(), storage, Serializer);

        var failing = async () => await repository.ApplyAndSaveAsync(() => true);
        await failing.Should().ThrowAsync<IOException>();

        var next = repository.ApplyAndSaveAsync(() => true);

        (await Task.WhenAny(next, Task.Delay(2000))).Should().BeSameAs(next, "the gate was released");
        (await next).Should().BeTrue();
    }

    [Fact]
    public async Task ApplyAndSaveAsync_WhenApplyThrows_WritesNothingAndLeavesTheGateAvailable()
    {
        var storage = new RecordingJsonStorage();
        var repository = new InvestmentJsonRepository(Investments.Create(), storage, Serializer);

        var failing = async () => await repository.ApplyAndSaveAsync(() => throw new InvalidOperationException("boom"));
        await failing.Should().ThrowAsync<InvalidOperationException>();
        storage.WriteCount.Should().Be(0);

        var next = repository.ApplyAndSaveAsync(() => true);

        (await Task.WhenAny(next, Task.Delay(2000))).Should().BeSameAs(next, "the gate was released");
        (await next).Should().BeTrue();
    }

    /// <summary>
    /// The reported failure. The portfolio grid fetches every row at once, so one request appends to
    /// an asset's price history while another serializes the whole graph. Each asset carries enough
    /// history to make that walk long enough for the collision to be reliable rather than occasional.
    /// </summary>
    [Fact]
    public async Task ApplyAndSaveAsync_ConcurrentPriceWritesAcrossAssets_AllSucceedAndArePersisted()
    {
        const int AssetCount = 24;
        const int HistoryEntriesPerAsset = 300;

        var investments = Investments.Create();
        var broker = Broker.Create("XPI", "BRL");
        var portfolio = broker.AddPortfolio("Default");
        var seedDate = new DateOnly(2020, 1, 1);
        for (var i = 0; i < AssetCount; i++)
        {
            var asset = Asset.Create($"ASSET{i}", $"ISIN{i}", "BVMF", $"ASSET{i}");
            for (var day = 0; day < HistoryEntriesPerAsset; day++)
            {
                asset.SetPrice(seedDate.AddDays(day), 10m + day, isManual: false);
            }

            portfolio.AddAsset(asset);
        }

        investments.AddActiveBroker(broker);

        var storage = new RecordingJsonStorage();
        var repository = new InvestmentJsonRepository(investments, storage, Serializer);
        var today = DateOnly.FromDateTime(DateTime.Today);
        var assets = repository.GetAssetsByBrokerPortfolio("XPI", "Default").ToList();

        var saves = assets.Select((asset, index) => Task.Run(() =>
            repository.ApplyAndSaveAsync(() =>
            {
                asset.SetPrice(today, 100m + index, isManual: false);
                return true;
            })));

        var act = async () => await Task.WhenAll(saves);

        await act.Should().NotThrowAsync("serializing the graph must never observe a concurrent mutation");

        var persisted = Serializer.Deserialize(storage.LastJson!);
        persisted.ActiveBrokers.Single().Portfolios.Single().Assets
            .Should().OnlyContain(asset => asset.GetPriceForDate(today) != null,
                "the last document written must carry every recorded price");
    }

    private sealed class RecordingJsonStorage : IJsonStorage
    {
        public int WriteCount { get; private set; }

        public string? LastJson { get; private set; }

        public bool FailNextWrite { get; set; }

        public Task<string> ReadAsync() => throw new NotSupportedException("These tests build the graph directly.");

        public Task WriteAsync(string json)
        {
            if (FailNextWrite)
            {
                FailNextWrite = false;
                throw new IOException("The process cannot access the file because it is being used by another process.");
            }

            WriteCount++;
            LastJson = json;
            return Task.CompletedTask;
        }
    }

    /// <summary>Holds a write open until the test releases it, so "is the second caller blocked?" is
    /// answered by the gate rather than by timing.</summary>
    private sealed class BlockingJsonStorage : IJsonStorage
    {
        private readonly TaskCompletionSource _release = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public TaskCompletionSource WriteEntered { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public int WriteCount { get; private set; }

        public Task<string> ReadAsync() => throw new NotSupportedException("These tests build the graph directly.");

        public void ReleaseWrite() => _release.TrySetResult();

        public async Task WriteAsync(string json)
        {
            WriteCount++;
            WriteEntered.TrySetResult();
            await _release.Task;
        }
    }

    private static InvestmentJsonRepository CreateRepositoryWithBothScopes(out Broker activeBroker, out Broker historicBroker)
    {
        var investments = Investments.Create();

        activeBroker = Broker.Create("XPI", "BRL");
        activeBroker.AddPortfolio("Default").AddAsset(Asset.Create("ACTIVE_ASSET", "ISIN1", "BVMF", "ACTIVE_ASSET"));
        investments.AddActiveBroker(activeBroker);

        historicBroker = Broker.Create("XPI", "BRL");
        historicBroker.AddPortfolio("Uncategorized").AddAsset(Asset.Create("HISTORIC_ASSET", "ISIN2", "BVMF", "HISTORIC_ASSET"));
        investments.AddHistoricBroker(historicBroker);

        return new InvestmentJsonRepository(investments, TestDataStorage, Serializer);
    }
}
