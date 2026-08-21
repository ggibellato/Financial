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

    [Fact]
    public void IsEmpty_WithNoAssets_IsTrue()
    {
        var portfolio = Broker.Create("Broker A", "USD").AddPortfolio("Default");

        portfolio.IsEmpty.Should().BeTrue();
    }

    [Fact]
    public void IsEmpty_AfterAddingAsset_IsFalse()
    {
        var portfolio = Broker.Create("Broker A", "USD").AddPortfolio("Default");

        portfolio.AddAsset(Asset.Create("Asset A", "ISIN123", "NYSE", "AAA"));

        portfolio.IsEmpty.Should().BeFalse();
    }

    [Fact]
    public void FindAsset_WhenPresent_ReturnsTheSameInstance()
    {
        var portfolio = Broker.Create("Broker A", "USD").AddPortfolio("Default");
        var asset = Asset.Create("Asset A", "ISIN123", "NYSE", "AAA");
        portfolio.AddAsset(asset);

        portfolio.FindAsset("Asset A").Should().BeSameAs(asset);
    }

    [Fact]
    public void FindAsset_WhenAbsent_ReturnsNull()
    {
        var portfolio = Broker.Create("Broker A", "USD").AddPortfolio("Default");
        portfolio.AddAsset(Asset.Create("Asset A", "ISIN123", "NYSE", "AAA"));

        portfolio.FindAsset("Asset B").Should().BeNull();
    }

    [Fact]
    public void FindAsset_DifferingByCase_ReturnsNull()
    {
        var portfolio = Broker.Create("Broker A", "USD").AddPortfolio("Default");
        portfolio.AddAsset(Asset.Create("Asset A", "ISIN123", "NYSE", "AAA"));

        portfolio.FindAsset("asset a").Should().BeNull();
    }

    [Fact]
    public void RemoveAsset_WhenPresent_DetachesItAndReportsTrue()
    {
        var portfolio = Broker.Create("Broker A", "USD").AddPortfolio("Default");
        portfolio.AddAsset(Asset.Create("Asset A", "ISIN123", "NYSE", "AAA"));

        var removed = portfolio.RemoveAsset("Asset A");

        removed.Should().BeTrue();
        portfolio.Assets.Should().BeEmpty();
        portfolio.IsEmpty.Should().BeTrue();
    }

    [Fact]
    public void RemoveAsset_WhenAbsent_ReportsFalseAndChangesNothing()
    {
        var portfolio = Broker.Create("Broker A", "USD").AddPortfolio("Default");
        portfolio.AddAsset(Asset.Create("Asset A", "ISIN123", "NYSE", "AAA"));

        var removed = portfolio.RemoveAsset("Asset B");

        removed.Should().BeFalse();
        portfolio.Assets.Should().ContainSingle();
    }

    [Fact]
    public void RemoveAsset_RemovesOnlyTheNamedAsset()
    {
        var portfolio = Broker.Create("Broker A", "USD").AddPortfolio("Default");
        portfolio.AddAsset(Asset.Create("Asset A", "ISIN123", "NYSE", "AAA"));
        portfolio.AddAsset(Asset.Create("Asset B", "ISIN456", "NYSE", "BBB"));

        portfolio.RemoveAsset("Asset A");

        portfolio.Assets.Should().ContainSingle().Which.Name.Should().Be("Asset B");
    }
}
