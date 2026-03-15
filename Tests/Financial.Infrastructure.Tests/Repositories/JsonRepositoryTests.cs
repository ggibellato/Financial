using System;
using Financial.Infrastructure.Repositories;
using Financial.Infrastructure.Persistence;
using FluentAssertions;

namespace Financial.Infrastructure.Tests.Repositories;

public class JsonRepositoryTests
{
    private readonly JSONRepository _sut = new JSONRepository(new LocalJsonStorage(TestDataPaths.DataJsonFile));

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

    [Fact]
    public void GetBrokerInfo_WithKnownData_ReturnsExpectedTotals()
    {
        var result = _sut.GetBrokerInfo("XPI");

        result.TotalBought.Should().Be(1000m);
        result.TotalSold.Should().Be(220m);
        result.TotalCredits.Total.Should().Be(11m);
        result.TotalBoughtActive.Should().Be(1000m);
        result.TotalSoldActive.Should().Be(220m);
        result.TotalCreditsActive.Total.Should().Be(11m);
        result.PortfoliosActive.Should().ContainSingle(p => p.Name == "Default" && p.Assets.Contains("BCIA11"));
    }

    [Fact]
    public void GetAssetInfo_WithKnownData_ReturnsInvestedHistory()
    {
        var result = _sut.GetAssetInfo("XPI", "Default", "BCIA11");

        result.InvestedHistory.Should().HaveCount(2);
        result.InvestedHistory.Should().ContainKey(new DateOnly(2024, 1, 1)).WhoseValue.Should().Be(1000m);
        result.InvestedHistory.Should().ContainKey(new DateOnly(2024, 6, 1)).WhoseValue.Should().Be(780m);
    }
}

