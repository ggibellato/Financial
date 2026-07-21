using Financial.Investment.Domain.Entities;
using FluentAssertions;

namespace Financial.Investment.Domain.Tests;

public class PortfolioTests
{
    [Fact]
    public void AddAsset_AddsToCollection()
    {
        var broker = Broker.Create("Broker A", "USD");
        var portfolio = broker.AddPortfolio("Default");
        var asset = Asset.Create("Asset A", "ISIN123", "NYSE", "AAA");

        portfolio.AddAsset(asset);

        portfolio.Assets.Should().ContainSingle().Which.Should().Be(asset);
    }
}
