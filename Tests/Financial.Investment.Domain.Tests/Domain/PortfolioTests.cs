using System.Collections.Generic;
using Financial.Investment.Domain.Entities;
using Financial.Investment.Domain.Exceptions;
using FluentAssertions;

namespace Financial.Investment.Domain.Tests;

public class PortfolioTests
{
    [Fact]
    public void RegisterAsset_WithUniqueName_AddsToCollection()
    {
        var portfolio = Broker.Create("Broker A", "USD").AddPortfolio("Default");
        var asset = Asset.Create("Asset A", "ISIN123", "NYSE", "AAA");

        portfolio.RegisterAsset(asset);

        portfolio.Assets.Should().ContainSingle().Which.Should().Be(asset);
    }

    [Fact]
    public void RegisterAsset_WithDuplicateName_Throws()
    {
        var portfolio = Broker.Create("Broker A", "USD").AddPortfolio("Default");
        portfolio.RegisterAsset(Asset.Create("Asset A", "ISIN123", "NYSE", "AAA"));

        var act = () => portfolio.RegisterAsset(Asset.Create("Asset A", "ISIN456", "LSE", "BBB"));

        act.Should().Throw<InvestmentRuleViolationException>()
            .WithMessage("*already has an asset named \"Asset A\"*");
    }

    [Fact]
    public void UpdateAssetIdentity_WithNewName_RenamesAndKeepsOtherFieldsCurrent()
    {
        var portfolio = Broker.Create("Broker A", "USD").AddPortfolio("Default");
        portfolio.RegisterAsset(Asset.Create("Asset A", "ISIN123", "NYSE", "AAA"));

        var updated = portfolio.UpdateAssetIdentity(
            "Asset A", "Asset A2", "ISIN999", "LSE", "ZZZ", CountryCode.UK, "Stock", GlobalAssetClass.Equity);

        updated.Name.Should().Be("Asset A2");
        updated.ISIN.Should().Be("ISIN999");
        updated.Exchange.Should().Be("LSE");
        updated.Ticker.Should().Be("ZZZ");
        updated.Country.Should().Be(CountryCode.UK);
        updated.LocalTypeCode.Should().Be("Stock");
        updated.Class.Should().Be(GlobalAssetClass.Equity);
        portfolio.FindAsset("Asset A").Should().BeNull();
        portfolio.FindAsset("Asset A2").Should().BeSameAs(updated);
    }

    [Fact]
    public void UpdateAssetIdentity_ToItsOwnCurrentName_Succeeds()
    {
        var portfolio = Broker.Create("Broker A", "USD").AddPortfolio("Default");
        portfolio.RegisterAsset(Asset.Create("Asset A", "ISIN123", "NYSE", "AAA"));

        var act = () => portfolio.UpdateAssetIdentity(
            "Asset A", "Asset A", "ISIN123", "NYSE", "AAA", CountryCode.Unknown, "", GlobalAssetClass.Unknown);

        act.Should().NotThrow();
    }

    [Fact]
    public void UpdateAssetIdentity_ToAnotherAssetsName_Throws()
    {
        var portfolio = Broker.Create("Broker A", "USD").AddPortfolio("Default");
        portfolio.RegisterAsset(Asset.Create("Asset A", "ISIN123", "NYSE", "AAA"));
        portfolio.RegisterAsset(Asset.Create("Asset B", "ISIN456", "NYSE", "BBB"));

        var act = () => portfolio.UpdateAssetIdentity(
            "Asset A", "Asset B", "ISIN123", "NYSE", "AAA", CountryCode.Unknown, "", GlobalAssetClass.Unknown);

        act.Should().Throw<InvestmentRuleViolationException>()
            .WithMessage("*already has an asset named \"Asset B\"*");
    }

    [Fact]
    public void UpdateAssetIdentity_WhenAssetMissing_ThrowsKeyNotFound()
    {
        var portfolio = Broker.Create("Broker A", "USD").AddPortfolio("Default");

        var act = () => portfolio.UpdateAssetIdentity(
            "Missing", "Missing", "", "", "", CountryCode.Unknown, "", GlobalAssetClass.Unknown);

        act.Should().Throw<KeyNotFoundException>();
    }

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
