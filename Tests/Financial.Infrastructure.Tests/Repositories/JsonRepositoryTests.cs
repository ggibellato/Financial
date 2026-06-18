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

    [Fact]
    public void GetAllAssetsFullName_ShouldReturn_Values()
    {
        var result = _sut.GetAllAssetsFullName();
        result.Should().NotBeEmpty();
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

    [Theory]
    [InlineData(null, 0)]
    [InlineData("", 0)]
    [InlineData("NOTEXIST", 0)]
    [InlineData("Default", 1)]
    public void GetAssets_By_PortfolioTest(string? name, int records)
    {
        var result = _sut.GetAssetsByPortfolio(name ?? string.Empty);
        result.Should().HaveCount(records);
    }

    [Theory]
    [InlineData(null, 0)]
    [InlineData("", 0)]
    [InlineData("NOTEXIST", 0)]
    [InlineData("BCIA11", 1)]
    public void GetAssets_By_NameTest(string? name, int records)
    {
        var result = _sut.GetAssetsByAssetName(name ?? string.Empty);
        result.Should().HaveCount(records);
    }

}

