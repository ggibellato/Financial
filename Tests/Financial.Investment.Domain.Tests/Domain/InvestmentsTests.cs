using Financial.Investment.Domain.Entities;
using Financial.Investment.Domain.Exceptions;
using FluentAssertions;
using FluentAssertions.Execution;

namespace Financial.Investment.Domain.Tests;

public class InvestmentsTests
{
    [Fact]
    public void AddActiveBroker_AddsBrokerToActiveCollection()
    {
        var investments = Investments.Create();
        var broker = Broker.Create("Broker A", "USD");

        investments.AddActiveBroker(broker);

        investments.ActiveBrokers.Should().ContainSingle().Which.Name.Should().Be("Broker A");
        investments.HistoricBrokers.Should().BeEmpty();
    }

    [Fact]
    public void AddHistoricBroker_AddsBrokerToHistoricCollection()
    {
        var investments = Investments.Create();
        var broker = Broker.Create("Broker A", "USD");

        investments.AddHistoricBroker(broker);

        investments.HistoricBrokers.Should().ContainSingle().Which.Name.Should().Be("Broker A");
        investments.ActiveBrokers.Should().BeEmpty();
    }

    [Fact]
    public void ActiveAndHistoricBrokers_AreIndependentCollections()
    {
        var investments = Investments.Create();

        investments.AddActiveBroker(Broker.Create("Active Broker", "USD"));
        investments.AddHistoricBroker(Broker.Create("Historic Broker", "USD"));

        investments.ActiveBrokers.Should().ContainSingle().Which.Name.Should().Be("Active Broker");
        investments.HistoricBrokers.Should().ContainSingle().Which.Name.Should().Be("Historic Broker");
    }

    [Fact]
    public void FindActiveBroker_ResolvesOnlyWithinItsOwnScope()
    {
        var investments = Investments.Create();
        investments.AddActiveBroker(Broker.Create("XPI", "BRL"));

        using (new AssertionScope())
        {
            investments.FindActiveBroker("XPI").Should().NotBeNull();
            investments.FindHistoricBroker("XPI").Should().BeNull();
        }
    }

    [Fact]
    public void ArchiveAsset_MovesAClosedAssetIntoAnExistingHistoricPortfolio()
    {
        var investments = CreateInvestmentsWithClosedAsset(withHistoricCounterpart: true);
        investments.FindHistoricBroker("XPI")!.AddPortfolio("Closed 2024");

        investments.ArchiveAsset("XPI", "Default", "VOD", "Closed 2024");

        using (new AssertionScope())
        {
            investments.FindHistoricBroker("XPI")!.FindPortfolio("Closed 2024")!
                .Assets.Should().ContainSingle().Which.Name.Should().Be("VOD");
            investments.FindActiveBroker("XPI")!.FindPortfolio("Default")!.Assets.Should().BeEmpty();
        }
    }

    [Fact]
    public void ArchiveAsset_CarriesTheClosedPositionsRecordWithIt()
    {
        // The history is the whole reason a closed asset is kept; archiving must not disturb it.
        var investments = CreateInvestmentsWithClosedAsset(withHistoricCounterpart: true);
        var asset = investments.FindActiveBroker("XPI")!.FindPortfolio("Default")!.FindAsset("VOD")!;
        var realized = asset.RealizedGainLoss;

        investments.ArchiveAsset("XPI", "Default", "VOD", "Closed");

        var archived = investments.FindHistoricBroker("XPI")!.FindPortfolio("Closed")!.FindAsset("VOD")!;
        using (new AssertionScope())
        {
            archived.Should().BeSameAs(asset);
            archived.Transactions.Count.Should().Be(2);
            archived.Credits.Should().ContainSingle();
            archived.RealizedGainLoss.Should().Be(realized);
            archived.Quantity.Should().Be(0);
        }
    }

    [Fact]
    public void ArchiveAsset_WhenTheBrokerHasNoHistoricRecord_CreatesItWithTheSameNameAndCurrency()
    {
        // FR-043. Verified against the live data: Coinbase trades in Active with nothing closed yet,
        // so archiving its first closed holding is what brings its Historic record into being.
        var investments = CreateInvestmentsWithClosedAsset(withHistoricCounterpart: false);

        investments.ArchiveAsset("XPI", "Default", "VOD", "Closed");

        var historic = investments.FindHistoricBroker("XPI");
        using (new AssertionScope())
        {
            historic.Should().NotBeNull();
            historic!.Currency.Should().Be("BRL", "the Historic record is the same real-world broker");
            historic.FindPortfolio("Closed")!.Assets.Should().ContainSingle().Which.Name.Should().Be("VOD");
            investments.HistoricBrokers.Should().ContainSingle();
        }
    }

    [Theory]
    [InlineData(5)]
    [InlineData(-5)]
    public void ArchiveAsset_WhenTheAssetStillHoldsAPosition_IsRefusedAndChangesNothing(int openQuantity)
    {
        // A short position is an open position: the rule is "not zero", not "not positive".
        var investments = CreateInvestmentsWithClosedAsset(withHistoricCounterpart: true);
        var asset = investments.FindActiveBroker("XPI")!.FindPortfolio("Default")!.FindAsset("VOD")!;
        asset.AddTransaction(Transaction.Create(
            new DateTime(2024, 4, 1),
            openQuantity > 0 ? Transaction.TransactionType.Buy : Transaction.TransactionType.Sell,
            Math.Abs(openQuantity),
            3m,
            0m));

        var act = () => investments.ArchiveAsset("XPI", "Default", "VOD", "Closed");

        act.Should().Throw<InvestmentRuleViolationException>().WithMessage("*fully closed*");
        using (new AssertionScope())
        {
            investments.FindActiveBroker("XPI")!.FindPortfolio("Default")!.Assets.Should().ContainSingle();
            investments.FindHistoricBroker("XPI")!.FindPortfolio("Closed").Should().BeNull();
        }
    }

    [Fact]
    public void ArchiveAsset_WhenTheHistoricDestinationAlreadyHoldsThatName_IsRefusedAndChangesNothing()
    {
        var investments = CreateInvestmentsWithClosedAsset(withHistoricCounterpart: true);
        investments.FindHistoricBroker("XPI")!.AddPortfolio("Closed")
            .AddAsset(Asset.Create("VOD", "ISIN999", "LSE", "ZZZ"));

        var act = () => investments.ArchiveAsset("XPI", "Default", "VOD", "Closed");

        act.Should().Throw<InvestmentRuleViolationException>().WithMessage("*already holds an asset named*");
        investments.FindActiveBroker("XPI")!.FindPortfolio("Default")!.Assets.Should().ContainSingle();
    }

    [Fact]
    public void ArchiveAsset_IntoAHistoricPortfolioNamedLikeTheSource_IsAllowed()
    {
        // Unlike a move within one broker, the source name is no obstacle across scopes: a Historic
        // "Default" is a perfectly good home for an asset leaving an Active "Default".
        var investments = CreateInvestmentsWithClosedAsset(withHistoricCounterpart: true);

        investments.ArchiveAsset("XPI", "Default", "VOD", "Default");

        investments.FindHistoricBroker("XPI")!.FindPortfolio("Default")!
            .Assets.Should().ContainSingle().Which.Name.Should().Be("VOD");
    }

    [Fact]
    public void ArchiveAsset_WhenANewNameDiffersOnlyByCase_IsRefused()
    {
        var investments = CreateInvestmentsWithClosedAsset(withHistoricCounterpart: true);
        investments.FindHistoricBroker("XPI")!.AddPortfolio("Closed");

        var act = () => investments.ArchiveAsset("XPI", "Default", "VOD", "CLOSED");

        act.Should().Throw<InvestmentRuleViolationException>().WithMessage("*already has a portfolio named*");
    }

    [Fact]
    public void ArchiveAsset_WhenTheActiveBrokerIsUnknown_ThrowsNotFound()
    {
        var investments = CreateInvestmentsWithClosedAsset(withHistoricCounterpart: true);

        var act = () => investments.ArchiveAsset("Nope", "Default", "VOD", "Closed");

        act.Should().Throw<KeyNotFoundException>();
    }

    [Fact]
    public void ArchiveAsset_WhenTheAssetIsUnknown_ThrowsNotFoundAndCreatesNothing()
    {
        var investments = CreateInvestmentsWithClosedAsset(withHistoricCounterpart: false);

        var act = () => investments.ArchiveAsset("XPI", "Default", "Nope", "Closed");

        act.Should().Throw<KeyNotFoundException>();
        investments.HistoricBrokers.Should().BeEmpty("a refused archive must not leave a broker behind");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void ArchiveAsset_WithABlankDestinationName_ThrowsArgumentException(string destination)
    {
        var investments = CreateInvestmentsWithClosedAsset(withHistoricCounterpart: true);

        var act = () => investments.ArchiveAsset("XPI", "Default", "VOD", destination);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void ArchiveAsset_TrimsANewDestinationName()
    {
        var investments = CreateInvestmentsWithClosedAsset(withHistoricCounterpart: true);

        investments.ArchiveAsset("XPI", "Default", "VOD", "  Closed 2024  ");

        investments.FindHistoricBroker("XPI")!.FindPortfolio("Closed 2024").Should().NotBeNull();
    }

    [Fact]
    public void CreateActiveBroker_AddsANewActiveBroker()
    {
        var investments = Investments.Create();

        var broker = investments.CreateActiveBroker("XPI", "BRL");

        using (new AssertionScope())
        {
            broker.Name.Should().Be("XPI");
            broker.Currency.Should().Be("BRL");
            investments.ActiveBrokers.Should().ContainSingle().Which.Should().BeSameAs(broker);
        }
    }

    [Fact]
    public void CreateActiveBroker_DuplicateOfAnActiveBroker_ThrowsAndAddsNothing()
    {
        var investments = Investments.Create();
        investments.AddActiveBroker(Broker.Create("XPI", "BRL"));

        var act = () => investments.CreateActiveBroker("XPI", "GBP");

        act.Should().Throw<InvestmentRuleViolationException>().WithMessage("*XPI*already exists*");
        investments.ActiveBrokers.Should().ContainSingle();
    }

    [Fact]
    public void CreateActiveBroker_DuplicateOfAHistoricBroker_ThrowsAndAddsNothing()
    {
        var investments = Investments.Create();
        investments.AddHistoricBroker(Broker.Create("XPI", "BRL"));

        var act = () => investments.CreateActiveBroker("XPI", "GBP");

        act.Should().Throw<InvestmentRuleViolationException>();
        investments.ActiveBrokers.Should().BeEmpty();
    }

    [Fact]
    public void RenameBroker_ChangesNameAndCurrency()
    {
        var investments = Investments.Create();
        investments.AddActiveBroker(Broker.Create("XPI", "BRL"));

        var broker = investments.RenameBroker("XPI", "XP Investimentos", "USD");

        using (new AssertionScope())
        {
            broker.Name.Should().Be("XP Investimentos");
            broker.Currency.Should().Be("USD");
            investments.FindActiveBroker("XP Investimentos").Should().NotBeNull();
        }
    }

    [Fact]
    public void RenameBroker_KeepingTheSameName_OnlyChangesCurrency()
    {
        var investments = Investments.Create();
        investments.AddActiveBroker(Broker.Create("XPI", "BRL"));

        var broker = investments.RenameBroker("XPI", "XPI", "USD");

        broker.Currency.Should().Be("USD");
    }

    [Fact]
    public void RenameBroker_ToAnotherExistingBrokersName_ThrowsAndChangesNothing()
    {
        var investments = Investments.Create();
        investments.AddActiveBroker(Broker.Create("XPI", "BRL"));
        investments.AddActiveBroker(Broker.Create("Avenue", "USD"));

        var act = () => investments.RenameBroker("XPI", "Avenue", "BRL");

        act.Should().Throw<InvestmentRuleViolationException>();
        investments.FindActiveBroker("XPI").Should().NotBeNull();
    }

    [Fact]
    public void RenameBroker_UnknownBroker_ThrowsKeyNotFound()
    {
        var investments = Investments.Create();

        var act = () => investments.RenameBroker("Nope", "New Name", "BRL");

        act.Should().Throw<KeyNotFoundException>();
    }

    [Fact]
    public void DeleteBroker_ActiveAndEmpty_MovesItToHistoric()
    {
        var investments = Investments.Create();
        investments.AddActiveBroker(Broker.Create("XPI", "BRL"));

        investments.DeleteBroker("XPI");

        using (new AssertionScope())
        {
            investments.FindActiveBroker("XPI").Should().BeNull();
            investments.FindHistoricBroker("XPI").Should().NotBeNull();
        }
    }

    [Fact]
    public void DeleteBroker_ActiveAndEmptyWithAnExistingHistoricNamesake_RemovesFromActiveWithoutDuplicating()
    {
        var investments = Investments.Create();
        investments.AddActiveBroker(Broker.Create("XPI", "BRL"));
        var existingHistoric = Broker.Create("XPI", "BRL");
        investments.AddHistoricBroker(existingHistoric);

        investments.DeleteBroker("XPI");

        using (new AssertionScope())
        {
            investments.FindActiveBroker("XPI").Should().BeNull();
            investments.HistoricBrokers.Should().ContainSingle().Which.Should().BeSameAs(existingHistoric);
        }
    }

    [Fact]
    public void DeleteBroker_HistoricAndEmpty_RemovesItPermanently()
    {
        var investments = Investments.Create();
        investments.AddHistoricBroker(Broker.Create("XPI", "BRL"));

        investments.DeleteBroker("XPI");

        investments.FindHistoricBroker("XPI").Should().BeNull();
    }

    [Fact]
    public void DeleteBroker_WithPortfolios_ThrowsAndChangesNothing()
    {
        var investments = Investments.Create();
        var broker = Broker.Create("XPI", "BRL");
        broker.AddPortfolio("Default");
        investments.AddActiveBroker(broker);

        var act = () => investments.DeleteBroker("XPI");

        act.Should().Throw<InvestmentRuleViolationException>().WithMessage("*still has portfolios*");
        investments.FindActiveBroker("XPI").Should().NotBeNull();
    }

    [Fact]
    public void DeleteBroker_UnknownBroker_ThrowsKeyNotFound()
    {
        var investments = Investments.Create();

        var act = () => investments.DeleteBroker("Nope");

        act.Should().Throw<KeyNotFoundException>();
    }

    /// <summary>
    /// Active XPI holding "VOD", bought then fully sold with a dividend along the way: closed, but
    /// with a record worth keeping.
    /// </summary>
    private static Investments CreateInvestmentsWithClosedAsset(bool withHistoricCounterpart)
    {
        var investments = Investments.Create();

        var active = Broker.Create("XPI", "BRL");
        var asset = Asset.Create("VOD", "ISIN123", "LSE", "VOD");
        asset.AddTransaction(Transaction.Create(new DateTime(2024, 1, 1), Transaction.TransactionType.Buy, 10m, 5m, 0m));
        asset.AddTransaction(Transaction.Create(new DateTime(2024, 3, 1), Transaction.TransactionType.Sell, 10m, 7m, 0m));
        asset.AddCredit(Credit.Create(new DateTime(2024, 2, 1), Credit.CreditType.Dividend, 4m));
        active.AddPortfolio("Default").AddAsset(asset);
        investments.AddActiveBroker(active);

        if (withHistoricCounterpart)
        {
            investments.AddHistoricBroker(Broker.Create("XPI", "BRL"));
        }

        return investments;
    }
}
