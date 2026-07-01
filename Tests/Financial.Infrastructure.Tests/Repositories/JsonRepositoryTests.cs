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

}

