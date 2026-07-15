using Financial.Application.Interfaces;
using Financial.Domain.Entities;
using Financial.Infrastructure.Persistence;
using Financial.Infrastructure.Repositories;
using FluentAssertions;

namespace Financial.Infrastructure.Tests.Repositories;

public class JsonRepositoryTests
{
    private readonly JSONRepository _sut = CreateRepository(TestDataPaths.DataJsonFile);

    private static JSONRepository CreateRepository(string dataFile)
    {
        var storage = new LocalJsonStorage(dataFile);
        var serializer = new InvestmentsSerializerAdapter();
        return new JSONRepository(InvestmentsLoader.LoadSync(storage, serializer), storage, serializer);
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
}
