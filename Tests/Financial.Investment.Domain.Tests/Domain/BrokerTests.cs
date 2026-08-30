using Financial.Investment.Domain.Entities;
using Financial.Investment.Domain.Exceptions;
using FluentAssertions;
using FluentAssertions.Execution;

namespace Financial.Investment.Domain.Tests;

public class BrokerTests
{
    [Fact]
    public void Update_ChangesNameAndCurrency()
    {
        var broker = Broker.Create("Broker A", "USD");

        broker.Update("Broker A Renamed", "BRL");

        using (new AssertionScope())
        {
            broker.Name.Should().Be("Broker A Renamed");
            broker.Currency.Should().Be("BRL");
        }
    }

    [Fact]
    public void AddPortfolio_SameName_ReturnsExisting()
    {
        var broker = Broker.Create("Broker A", "USD");

        var first = broker.AddPortfolio("Default");
        var second = broker.AddPortfolio("Default");

        first.Should().BeSameAs(second);
        broker.Portfolios.Should().ContainSingle();
    }

    [Fact]
    public void AddPortfolio_DifferentNames_AddsDistinct()
    {
        var broker = Broker.Create("Broker A", "USD");

        broker.AddPortfolio("Default");
        broker.AddPortfolio("Growth");

        broker.Portfolios.Should().HaveCount(2);
    }

    [Fact]
    public void FindPortfolio_WhenPresent_ReturnsTheSameInstance()
    {
        var broker = Broker.Create("Broker A", "USD");
        var portfolio = broker.AddPortfolio("Default");

        broker.FindPortfolio("Default").Should().BeSameAs(portfolio);
    }

    [Fact]
    public void FindPortfolio_WhenAbsent_ReturnsNull()
    {
        var broker = Broker.Create("Broker A", "USD");
        broker.AddPortfolio("Default");

        broker.FindPortfolio("Growth").Should().BeNull();
    }

    [Fact]
    public void MoveAsset_ToExistingPortfolio_MovesTheAssetAcross()
    {
        var broker = CreateBrokerWithAsset(out _);
        broker.AddPortfolio("ISA");

        broker.MoveAsset("Default", "Asset A", "ISA");

        using (new AssertionScope())
        {
            broker.FindPortfolio("ISA")!.Assets.Should().ContainSingle().Which.Name.Should().Be("Asset A");
            broker.FindPortfolio("Default")!.Assets.Should().BeEmpty();
            broker.FindPortfolio("Default")!.IsEmpty.Should().BeTrue();
        }
    }

    [Fact]
    public void MoveAsset_CarriesTheSameInstance_SoItsHistoryTravelsWithIt()
    {
        // The asset is relocated, never rebuilt - which is the whole reason no figure can drift.
        var broker = CreateBrokerWithAsset(out var asset);
        broker.AddPortfolio("ISA");

        broker.MoveAsset("Default", "Asset A", "ISA");

        broker.FindPortfolio("ISA")!.Assets.Should().ContainSingle().Which.Should().BeSameAs(asset);
    }

    [Fact]
    public void MoveAsset_LeavesEveryDerivedFigureUnchanged()
    {
        var broker = CreateBrokerWithAsset(out var asset);
        broker.AddPortfolio("ISA");

        var quantity = asset.Quantity;
        var averagePrice = asset.AveragePrice;
        var realizedGainLoss = asset.RealizedGainLoss;
        var transactionCount = asset.Transactions.Count;
        var creditCount = asset.Credits.Count;
        var priceCount = asset.PriceHistory.Count;

        broker.MoveAsset("Default", "Asset A", "ISA");

        var moved = broker.FindPortfolio("ISA")!.FindAsset("Asset A")!;
        using (new AssertionScope())
        {
            moved.Quantity.Should().Be(quantity);
            moved.AveragePrice.Should().Be(averagePrice);
            moved.RealizedGainLoss.Should().Be(realizedGainLoss);
            moved.Transactions.Count.Should().Be(transactionCount);
            moved.Credits.Count.Should().Be(creditCount);
            moved.PriceHistory.Count.Should().Be(priceCount);
            moved.GetPriceForDate(new DateOnly(2024, 3, 1))!.IsManual.Should().BeTrue();
        }
    }

    [Fact]
    public void MoveAsset_ToAPortfolioThatDoesNotExist_CreatesItHoldingOnlyThatAsset()
    {
        var broker = CreateBrokerWithAsset(out _);

        broker.MoveAsset("Default", "Asset A", "SIPP");

        using (new AssertionScope())
        {
            broker.FindPortfolio("SIPP").Should().NotBeNull();
            broker.FindPortfolio("SIPP")!.Assets.Should().ContainSingle().Which.Name.Should().Be("Asset A");
            broker.Portfolios.Should().HaveCount(2);
        }
    }

    [Fact]
    public void MoveAsset_MovesOnlyTheNamedAsset()
    {
        var broker = CreateBrokerWithAsset(out _);
        broker.FindPortfolio("Default")!.AddAsset(Asset.Create("Asset B", "ISIN456", "NYSE", "BBB"));

        broker.MoveAsset("Default", "Asset A", "ISA");

        using (new AssertionScope())
        {
            broker.FindPortfolio("Default")!.Assets.Should().ContainSingle().Which.Name.Should().Be("Asset B");
            broker.FindPortfolio("ISA")!.Assets.Should().ContainSingle().Which.Name.Should().Be("Asset A");
        }
    }

    [Fact]
    public void MoveAsset_ToItsOwnPortfolio_IsRefusedAndChangesNothing()
    {
        var broker = CreateBrokerWithAsset(out _);

        var act = () => broker.MoveAsset("Default", "Asset A", "Default");

        act.Should().Throw<InvestmentRuleViolationException>().WithMessage("*already the portfolio*");
        broker.FindPortfolio("Default")!.Assets.Should().ContainSingle();
    }

    [Fact]
    public void MoveAsset_WhenDestinationAlreadyHoldsThatName_IsRefusedAndChangesNothing()
    {
        var broker = CreateBrokerWithAsset(out _);
        broker.AddPortfolio("ISA").AddAsset(Asset.Create("Asset A", "ISIN999", "LSE", "ZZZ"));

        var act = () => broker.MoveAsset("Default", "Asset A", "ISA");

        act.Should().Throw<InvestmentRuleViolationException>().WithMessage("*already holds an asset named*");
        using (new AssertionScope())
        {
            broker.FindPortfolio("Default")!.Assets.Should().ContainSingle();
            broker.FindPortfolio("ISA")!.Assets.Should().ContainSingle();
        }
    }

    [Fact]
    public void MoveAsset_WhenSourcePortfolioIsUnknown_ThrowsNotFound()
    {
        var broker = CreateBrokerWithAsset(out _);

        var act = () => broker.MoveAsset("Nope", "Asset A", "ISA");

        act.Should().Throw<KeyNotFoundException>();
    }

    [Fact]
    public void MoveAsset_WhenAssetIsUnknown_ThrowsNotFoundAndCreatesNoPortfolio()
    {
        var broker = CreateBrokerWithAsset(out _);

        var act = () => broker.MoveAsset("Default", "Nope", "SIPP");

        act.Should().Throw<KeyNotFoundException>();
        broker.FindPortfolio("SIPP").Should().BeNull("a refused move must not leave a portfolio behind");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void MoveAsset_WithABlankDestinationName_ThrowsArgumentException(string destination)
    {
        var broker = CreateBrokerWithAsset(out _);

        var act = () => broker.MoveAsset("Default", "Asset A", destination);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void MoveAsset_TrimsANewDestinationName()
    {
        var broker = CreateBrokerWithAsset(out _);

        broker.MoveAsset("Default", "Asset A", "  SIPP  ");

        broker.FindPortfolio("SIPP").Should().NotBeNull();
    }

    [Fact]
    public void MoveAsset_WhenANewNameDiffersOnlyByCase_IsRefusedRatherThanCreatingALookalike()
    {
        var broker = CreateBrokerWithAsset(out _);
        broker.AddPortfolio("ISA");

        var act = () => broker.MoveAsset("Default", "Asset A", "isa");

        act.Should().Throw<InvestmentRuleViolationException>().WithMessage("*already has a portfolio named*");
        broker.Portfolios.Should().HaveCount(2, "no lookalike portfolio may be created");
    }

    [Fact]
    public void RemoveEmptyPortfolio_WhenEmpty_RemovesItAndReportsTrue()
    {
        var broker = Broker.Create("Broker A", "USD");
        broker.AddPortfolio("Default");
        broker.AddPortfolio("Stale");

        var removed = broker.RemoveEmptyPortfolio("Stale");

        using (new AssertionScope())
        {
            removed.Should().BeTrue();
            broker.FindPortfolio("Stale").Should().BeNull();
            broker.Portfolios.Should().ContainSingle().Which.Name.Should().Be("Default");
        }
    }

    [Fact]
    public void RemoveEmptyPortfolio_WhenItStillHoldsAssets_IsRefusedAndChangesNothing()
    {
        // The portfolio is the only record of where its assets live; removing it would take them
        // with it. Emptying it first is a move, which is a separate deliberate act.
        var broker = CreateBrokerWithAsset(out _);

        var act = () => broker.RemoveEmptyPortfolio("Default");

        act.Should().Throw<InvestmentRuleViolationException>().WithMessage("*Only an empty portfolio*");
        using (new AssertionScope())
        {
            broker.FindPortfolio("Default").Should().NotBeNull();
            broker.FindPortfolio("Default")!.Assets.Should().ContainSingle();
        }
    }

    [Fact]
    public void RemoveEmptyPortfolio_AfterAMoveEmptiesIt_IsAllowed()
    {
        // The sequence the feature exists for: the last asset leaves, then the portfolio can go.
        var broker = CreateBrokerWithAsset(out _);

        broker.MoveAsset("Default", "Asset A", "ISA");
        broker.RemoveEmptyPortfolio("Default");

        using (new AssertionScope())
        {
            broker.FindPortfolio("Default").Should().BeNull();
            broker.FindPortfolio("ISA")!.Assets.Should().ContainSingle();
        }
    }

    [Fact]
    public void RemoveEmptyPortfolio_WhenUnknown_ThrowsNotFound()
    {
        var broker = Broker.Create("Broker A", "USD");

        var act = () => broker.RemoveEmptyPortfolio("Nope");

        act.Should().Throw<KeyNotFoundException>();
    }

    /// <summary>An asset with transactions, credits and price history, so a move has something to lose.</summary>
    private static Broker CreateBrokerWithAsset(out Asset asset)
    {
        var broker = Broker.Create("Broker A", "USD");
        var portfolio = broker.AddPortfolio("Default");

        asset = Asset.Create("Asset A", "ISIN123", "NYSE", "AAA");
        asset.AddTransaction(Transaction.Create(new DateTime(2024, 1, 1), Transaction.TransactionType.Buy, 10m, 5m, 1m));
        asset.AddTransaction(Transaction.Create(new DateTime(2024, 2, 1), Transaction.TransactionType.Buy, 5m, 7m, 1m));
        asset.AddCredit(Credit.Create(new DateTime(2024, 2, 15), Credit.CreditType.Dividend, 3m));
        asset.SetPrice(new DateOnly(2024, 3, 1), 9m, isManual: true);

        portfolio.AddAsset(asset);
        return broker;
    }
}
