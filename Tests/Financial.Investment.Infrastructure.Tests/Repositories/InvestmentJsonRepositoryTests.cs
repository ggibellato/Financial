using Financial.Investment.Application.Enums;
using Financial.Investment.Domain.Entities;
using Financial.Investment.Infrastructure.Persistence;
using Financial.Shared.Infrastructure.Persistence;
using Financial.Shared.Infrastructure.Sync;
using Financial.Investment.Infrastructure.Repositories;
using Financial.TestUtilities;
using FluentAssertions;

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
