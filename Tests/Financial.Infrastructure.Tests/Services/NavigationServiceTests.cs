using Financial.Application.DTOs;
using Financial.Application.Interfaces;
using Financial.Domain.Entities;
using Financial.Infrastructure.Repositories;
using Financial.Infrastructure.Services;
using Financial.Infrastructure.Persistence;
using FluentAssertions;

namespace Financial.Infrastructure.Tests.Services;

public class NavigationServiceTests
{
    private readonly IRepository _repository = new JSONRepository(new LocalJsonStorage(TestDataPaths.DataJsonFile));
    private readonly NavigationService _sut;

    public NavigationServiceTests()
    {
        _sut = new NavigationService(_repository);
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
        result.NodeType.Should().Be("Investments");
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
            node.NodeType.Should().Be("Broker");
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
            node.NodeType.Should().Be("Portfolio");
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
            node.NodeType.Should().Be("Asset");
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
    public void GetAssetDetails_ShouldIncludeOperations()
    {
        // Arrange
        const string brokerName = "XPI";
        const string portfolioName = "Default";
        const string assetName = "BCIA11";

        // Act
        var result = _sut.GetAssetDetails(brokerName, portfolioName, assetName);

        // Assert
        result.Should().NotBeNull();
        result!.Operations.Should().NotBeEmpty();
        result.Operations.Should().AllSatisfy(op =>
        {
            op.Type.Should().NotBeNullOrWhiteSpace();
            op.Quantity.Should().BeGreaterThan(0);
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
    public void GetAssetDetails_OperationsShouldBeOrderedByDateDescending()
    {
        // Arrange
        const string brokerName = "XPI";
        const string portfolioName = "Default";
        const string assetName = "BCIA11";

        // Act
        var result = _sut.GetAssetDetails(brokerName, portfolioName, assetName);

        // Assert
        result.Should().NotBeNull();
        if (result!.Operations.Count > 1)
        {
            for (int i = 0; i < result.Operations.Count - 1; i++)
            {
                result.Operations[i].Date.Should().BeOnOrAfter(result.Operations[i + 1].Date);
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

    [Fact]
    public void GetBrokers_ShouldOrderByNameWithEncerradasLast()
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
        brokerNames.Should().ContainInOrder("Alpha", "Zeta", "Encerradas");
    }

    [Fact]
    public void GetBrokers_PortfoliosShouldOrderByNameWithEncerradasLast()
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
        portfolioNames.Should().ContainInOrder("Alpha", "Zeta", "Encerradas");
    }

    [Fact]
    public void GetBrokers_AssetsShouldOrderByNameWithEncerradasLast()
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
        assetNames.Should().ContainInOrder("Alpha", "Zeta", "Encerradas");
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

    private sealed class StubRepository : IRepository
    {
        private readonly List<Broker> _brokers;

        public StubRepository(IEnumerable<Broker> brokers)
        {
            _brokers = brokers.ToList();
        }

        public List<string> GetAllAssetsFullName() => throw new NotImplementedException();

        public IEnumerable<Asset> GetAssetsByBroker(string name) => throw new NotImplementedException();

        public IEnumerable<Asset> GetAssetsByBrokerPortfolio(string broker, string portfolio) => throw new NotImplementedException();

        public IEnumerable<Asset> GetAssetsByPortfolio(string name) => throw new NotImplementedException();

        public IEnumerable<Asset> GetAssetsByAssetName(string name) => throw new NotImplementedException();

        public IEnumerable<Broker> GetBrokerList() => _brokers;

        public Asset? GetAsset(string brokerName, string portfolioName, string assetName) => throw new NotImplementedException();

        public BrokerInfoDTO GetBrokerInfo(string brokerName) => throw new NotImplementedException();

        public AssetInfoDTO GetAssetInfo(string brokerName, string portfolio, string assetName) => throw new NotImplementedException();

        public void SaveChanges() => throw new NotImplementedException();
    }
}



