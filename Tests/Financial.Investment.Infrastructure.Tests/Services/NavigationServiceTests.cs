using Financial.Investment.Application.DTOs;
using Financial.Investment.Application.Interfaces;
using Financial.Investment.Application.Services;
using Financial.Investment.Domain.Entities;
using Financial.Investment.Infrastructure.Persistence;
using Financial.Shared.Infrastructure.Persistence;
using Financial.Investment.Infrastructure.Repositories;
using FluentAssertions;

namespace Financial.Investment.Infrastructure.Tests.Services;

public class NavigationServiceTests
{
    private readonly IRepository _repository = CreateRepository();

    private static IRepository CreateRepository()
    {
        var storage = new LocalJsonStorage(TestDataPaths.DataJsonFile);
        var serializer = new InvestmentsSerializerAdapter();
        return new JSONRepository(InvestmentsLoader.LoadSync(storage, serializer), storage, serializer);
    }
    private readonly NavigationService _sut;
    private readonly CreditService _creditSut;

    public NavigationServiceTests()
    {
        _sut = new NavigationService(_repository);
        _creditSut = new CreditService(_repository, _sut);
    }

    [Fact]
    public void Constructor_WithNullRepository_ThrowsArgumentNullException()
    {
        // Act
        Action act = () => new NavigationService(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("repository");
    }

    [Fact]
    public void GetNavigationTree_ShouldReturnRootNode()
    {
        // Act
        var result = _sut.GetNavigationTree();

        // Assert
        result.Should().NotBeNull();
        result.NodeType.Should().Be(TreeNodeType.Investments);
        result.DisplayName.Should().Be("All Investments");
        result.Children.Should().NotBeEmpty();
    }

    [Fact]
    public void GetNavigationTree_ShouldContainBrokerNodes()
    {
        // Act
        var result = _sut.GetNavigationTree();

        // Assert
        result.Children.Should().AllSatisfy(node =>
        {
            node.NodeType.Should().Be(TreeNodeType.Broker);
            node.Metadata.Should().ContainKey("BrokerName");
            node.Metadata.Should().ContainKey("Currency");
        });
    }

    [Fact]
    public void GetNavigationTree_BrokersShouldContainPortfolios()
    {
        // Act
        var result = _sut.GetNavigationTree();
        var brokerNode = result.Children.First();

        // Assert
        brokerNode.Children.Should().NotBeEmpty();
        brokerNode.Children.Should().AllSatisfy(node =>
        {
            node.NodeType.Should().Be(TreeNodeType.Portfolio);
            node.Metadata.Should().ContainKey("PortfolioName");
        });
    }

    [Fact]
    public void GetNavigationTree_PortfoliosShouldContainAssets()
    {
        // Act
        var result = _sut.GetNavigationTree();
        var brokerNode = result.Children.First();
        var portfolioNode = brokerNode.Children.First();

        // Assert
        portfolioNode.Children.Should().NotBeEmpty();
        portfolioNode.Children.Should().AllSatisfy(node =>
        {
            node.NodeType.Should().Be(TreeNodeType.Asset);
            node.Metadata.Should().ContainKey("AssetName");
            node.Metadata.Should().ContainKey("Ticker");
        });
    }

    [Fact]
    public void GetBrokers_ShouldReturnBrokerList()
    {
        // Act
        var result = _sut.GetBrokers().ToList();

        // Assert
        result.Should().NotBeEmpty();
        result.Should().AllSatisfy(broker =>
        {
            broker.Name.Should().NotBeNullOrWhiteSpace();
            broker.Currency.Should().NotBeNullOrWhiteSpace();
            broker.Portfolios.Should().NotBeNull();
        });
    }

    [Fact]
    public void GetBrokers_BrokersShouldHavePortfolios()
    {
        // Act
        var result = _sut.GetBrokers().ToList();
        var broker = result.First();

        // Assert
        broker.Portfolios.Should().NotBeEmpty();
        broker.PortfolioCount.Should().Be(broker.Portfolios.Count);
    }

    [Fact]
    public void GetBrokers_PortfoliosShouldHaveAssets()
    {
        // Act
        var result = _sut.GetBrokers().ToList();
        var broker = result.First();
        var portfolio = broker.Portfolios.First();

        // Assert
        portfolio.Assets.Should().NotBeEmpty();
        portfolio.AssetCount.Should().Be(portfolio.Assets.Count);
    }

    [Theory]
    [InlineData(null, "Default", "BCIA11")]
    [InlineData("", "Default", "BCIA11")]
    [InlineData("XPI", null, "BCIA11")]
    [InlineData("XPI", "", "BCIA11")]
    [InlineData("XPI", "Default", null)]
    [InlineData("XPI", "Default", "")]
    public void GetAssetDetails_WithInvalidParameters_ReturnsNull(string? broker, string? portfolio, string? asset)
    {
        // Act
        var result = _sut.GetAssetDetails(broker!, portfolio!, asset!);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void GetAssetDetails_WithNonExistentAsset_ReturnsNull()
    {
        // Act
        var result = _sut.GetAssetDetails("XPI", "Default", "NONEXISTENT");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void GetAssetDetails_WithValidParameters_ReturnsAssetDetails()
    {
        // Arrange
        // Using actual data from the JSON - XPI broker has BCIA11 in Default portfolio
        const string brokerName = "XPI";
        const string portfolioName = "Default";
        const string assetName = "BCIA11";

        // Act
        var result = _sut.GetAssetDetails(brokerName, portfolioName, assetName);

        // Assert
        result.Should().NotBeNull();
        result!.Name.Should().Be(assetName);
        result.BrokerName.Should().Be(brokerName);
        result.PortfolioName.Should().Be(portfolioName);
        result.Ticker.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void GetAssetDetails_ShouldIncludeTransactions()
    {
        // Arrange
        const string brokerName = "XPI";
        const string portfolioName = "Default";
        const string assetName = "BCIA11";

        // Act
        var result = _sut.GetAssetDetails(brokerName, portfolioName, assetName);

        // Assert
        result.Should().NotBeNull();
        result!.Transactions.Should().NotBeEmpty();
        result.Transactions.Should().AllSatisfy(t =>
        {
            t.Type.Should().NotBeNullOrWhiteSpace();
            t.Quantity.Should().BeGreaterThan(0);
        });
    }

    [Fact]
    public void GetAssetDetails_ShouldIncludeCredits()
    {
        // Arrange
        const string brokerName = "XPI";
        const string portfolioName = "Default";
        const string assetName = "BCIA11";

        // Act
        var result = _sut.GetAssetDetails(brokerName, portfolioName, assetName);

        // Assert
        result.Should().NotBeNull();
        result!.Credits.Should().NotBeEmpty();
        result.Credits.Should().AllSatisfy(credit =>
        {
            credit.Type.Should().NotBeNullOrWhiteSpace();
            credit.Value.Should().BeGreaterThan(0);
        });
    }

    [Fact]
    public void GetAssetDetails_ShouldIncludeCashFlowsWithCredits()
    {
        // Arrange
        const string brokerName = "XPI";
        const string portfolioName = "Default";
        const string assetName = "BCIA11";

        // Act
        var result = _sut.GetAssetDetails(brokerName, portfolioName, assetName);

        // Assert
        result.Should().NotBeNull();
        result!.CashFlowsWithCredits.Should().HaveCount(result.Transactions.Count + result.Credits.Count);
    }

    [Fact]
    public void GetAssetDetails_ShouldIncludeCashFlowsWithoutCredits_ExcludingCredits()
    {
        // Arrange
        const string brokerName = "XPI";
        const string portfolioName = "Default";
        const string assetName = "BCIA11";

        // Act
        var result = _sut.GetAssetDetails(brokerName, portfolioName, assetName);

        // Assert
        result.Should().NotBeNull();
        result!.CashFlowsWithoutCredits.Should().HaveCount(result.Transactions.Count);
    }

    [Fact]
    public void GetAssetDetails_ShouldCalculateTotalsCorrectly()
    {
        // Arrange
        const string brokerName = "XPI";
        const string portfolioName = "Default";
        const string assetName = "BCIA11";

        // Act
        var result = _sut.GetAssetDetails(brokerName, portfolioName, assetName);

        // Assert
        result.Should().NotBeNull();
        result!.TotalBought.Should().BeGreaterThan(0);
        result.TotalCredits.Should().BeGreaterThan(0);
        // Verify totals match sum of individual items
        var expectedTotalCredits = result.Credits.Sum(c => c.Value);
        result.TotalCredits.Should().Be(expectedTotalCredits);
    }

    [Fact]
    public void GetAssetDetails_TransactionsShouldBeOrderedByDateDescending()
    {
        // Arrange
        const string brokerName = "XPI";
        const string portfolioName = "Default";
        const string assetName = "BCIA11";

        // Act
        var result = _sut.GetAssetDetails(brokerName, portfolioName, assetName);

        // Assert
        result.Should().NotBeNull();
        if (result!.Transactions.Count > 1)
        {
            for (int i = 0; i < result.Transactions.Count - 1; i++)
            {
                result.Transactions[i].Date.Should().BeOnOrAfter(result.Transactions[i + 1].Date);
            }
        }
    }

    [Fact]
    public void GetAssetDetails_CreditsShouldBeOrderedByDateDescending()
    {
        // Arrange
        const string brokerName = "XPI";
        const string portfolioName = "Default";
        const string assetName = "BCIA11";

        // Act
        var result = _sut.GetAssetDetails(brokerName, portfolioName, assetName);

        // Assert
        result.Should().NotBeNull();
        if (result!.Credits.Count > 1)
        {
            for (int i = 0; i < result.Credits.Count - 1; i++)
            {
                result.Credits[i].Date.Should().BeOnOrAfter(result.Credits[i + 1].Date);
            }
        }
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void GetCreditsByBroker_WithInvalidParameters_ReturnsEmpty(string? brokerName)
    {
        // Act
        var result = _creditSut.GetCreditsByBroker(brokerName!);

        // Assert
        result.Should().BeEmpty();
    }

    [Theory]
    [InlineData(null, "Default")]
    [InlineData("", "Default")]
    [InlineData("XPI", null)]
    [InlineData("XPI", "")]
    public void GetCreditsByPortfolio_WithInvalidParameters_ReturnsEmpty(string? brokerName, string? portfolioName)
    {
        // Act
        var result = _creditSut.GetCreditsByPortfolio(brokerName!, portfolioName!);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void GetCreditsByBroker_ShouldReturnCredits()
    {
        // Arrange
        const string brokerName = "XPI";

        // Act
        var result = _creditSut.GetCreditsByBroker(brokerName);

        // Assert
        result.Should().NotBeEmpty();
        result.Should().HaveCount(2);
        result.Should().AllSatisfy(credit =>
        {
            credit.Type.Should().NotBeNullOrWhiteSpace();
            credit.Value.Should().BeGreaterThan(0);
        });
    }

    [Fact]
    public void GetCreditsByPortfolio_ShouldReturnCredits()
    {
        // Arrange
        const string brokerName = "XPI";
        const string portfolioName = "Default";

        // Act
        var result = _creditSut.GetCreditsByPortfolio(brokerName, portfolioName);

        // Assert
        result.Should().NotBeEmpty();
        result.Should().HaveCount(2);
        result.Should().AllSatisfy(credit =>
        {
            credit.Type.Should().NotBeNullOrWhiteSpace();
            credit.Value.Should().BeGreaterThan(0);
        });
    }

    [Fact]
    public void GetCreditsByBroker_CreditsShouldBeOrderedByDateDescending()
    {
        // Arrange
        const string brokerName = "XPI";

        // Act
        var result = _creditSut.GetCreditsByBroker(brokerName);

        // Assert
        if (result.Count > 1)
        {
            for (int i = 0; i < result.Count - 1; i++)
            {
                result[i].Date.Should().BeOnOrAfter(result[i + 1].Date);
            }
        }
    }

    [Fact]
    public void GetBrokers_ShouldOrderByNameAlphabetically()
    {
        // Arrange
        var repository = new StubRepository(new[]
        {
            BuildBroker("Zeta"),
            BuildBroker("Encerradas"),
            BuildBroker("Alpha")
        });
        var sut = new NavigationService(repository);

        // Act
        var brokerNames = sut.GetBrokers().Select(broker => broker.Name).ToList();

        // Assert
        brokerNames.Should().ContainInOrder("Alpha", "Encerradas", "Zeta");
    }

    [Fact]
    public void GetBrokers_PortfoliosShouldOrderByNameAlphabetically()
    {
        // Arrange
        var broker = BuildBroker("Broker", "USD",
            ("Zeta", new[] { "B" }),
            ("Encerradas", new[] { "C" }),
            ("Alpha", new[] { "A" }));
        var repository = new StubRepository(new[] { broker });
        var sut = new NavigationService(repository);

        // Act
        var portfolioNames = sut.GetBrokers().Single().Portfolios.Select(portfolio => portfolio.Name).ToList();

        // Assert
        portfolioNames.Should().ContainInOrder("Alpha", "Encerradas", "Zeta");
    }

    [Fact]
    public void GetBrokers_AssetsShouldOrderByNameAlphabetically()
    {
        // Arrange
        var broker = BuildBroker("Broker", "USD",
            ("Portfolio", new[] { "Zeta", "Encerradas", "Alpha" }));
        var repository = new StubRepository(new[] { broker });
        var sut = new NavigationService(repository);

        // Act
        var assetNames = sut.GetBrokers()
            .Single()
            .Portfolios.Single()
            .Assets.Select(asset => asset.Name)
            .ToList();

        // Assert
        assetNames.Should().ContainInOrder("Alpha", "Encerradas", "Zeta");
    }

    private static Broker BuildBroker(string name, string currency = "USD",
        params (string PortfolioName, string[] AssetNames)[] portfolios)
    {
        var broker = Broker.Create(name, currency);

        foreach (var (portfolioName, assetNames) in portfolios)
        {
            var portfolio = broker.AddPortfolio(portfolioName);
            foreach (var assetName in assetNames)
            {
                portfolio.AddAsset(Asset.Create(assetName, "ISIN", "EX", "TICKER"));
            }
        }

        return broker;
    }
}

