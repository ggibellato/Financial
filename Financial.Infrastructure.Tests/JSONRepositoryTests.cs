using FinancialModel.Infrastructure;
using FluentAssertions;

namespace Financial.Infrastructure.Tests;

public class JSONRepositoryTests
{
    private readonly JSONRepository _sut = new JSONRepository();

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
    [InlineData("FreeTrade", 14)]
    public void GetAssets_By_Broker(string name, int records)
    {
        var result = _sut.GetAssetsByBroker(name);
        result.Should().HaveCount(records);
    }

    [Theory]
    [InlineData(null, 0)]
    [InlineData("", 0)]
    [InlineData("NOTEXIST", 0)]
    [InlineData("Gold", 1)]
    public void GetAssets_By_Portifolio(string name, int records)
    {
        var result = _sut.GetAssetsByPortifolio(name);
        result.Should().HaveCount(records);
    }

    [Theory]
    [InlineData(null, 0)]
    [InlineData("", 0)]
    [InlineData("NOTEXIST", 0)]
    [InlineData("FTSE100", 1)]
    public void GetAssets_By_Name(string name, int records)
    {
        var result = _sut.GetAssetsByAssetName(name);
        result.Should().HaveCount(records);
    }
}