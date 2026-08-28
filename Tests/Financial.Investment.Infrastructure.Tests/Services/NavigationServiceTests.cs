using Financial.Investment.Application.DTOs;
using Financial.Investment.Application.Interfaces;
using Financial.Investment.Application.Services;
using Financial.Investment.Domain.Entities;
using Financial.Investment.Infrastructure.Persistence;
using Financial.Shared.Abstractions.Observability;
using Financial.Shared.Infrastructure.Persistence;
using Financial.Investment.Infrastructure.Repositories;
using Financial.TestUtilities;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;

namespace Financial.Investment.Infrastructure.Tests.Services;

public class NavigationServiceTests
{
    private readonly IInvestmentRepository _repository = CreateRepository();

    private static IInvestmentRepository CreateRepository()
    {
        var storage = new LocalJsonStorage(TestDataPaths.DataJsonFile);
        var serializer = new InvestmentSerializerAdapter();
        return new InvestmentJsonRepository(InvestmentLoader.LoadSync(storage, serializer), storage, serializer);
    }
    private readonly ITelemetryTracer _tracer = new RecordingTelemetryTracer();
    private readonly NavigationService _sut;
    private readonly CreditService _creditSut;

    public NavigationServiceTests()
    {
        _sut = new NavigationService(_repository, _tracer, NullLogger<NavigationService>.Instance);
        _creditSut = new CreditService(_repository, _sut, _tracer, NullLogger<CreditService>.Instance);
    }

    [Fact]
    public void Constructor_WithNullRepository_ThrowsArgumentNullException()
    {
        Action act = () => new NavigationService(null!, new RecordingTelemetryTracer(), NullLogger<NavigationService>.Instance);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("repository");
    }

    [Fact]
    public void Constructor_WithNullTracer_ThrowsArgumentNullException()
    {
        Action act = () => new NavigationService(_repository, null!, NullLogger<NavigationService>.Instance);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("tracer");
    }

    [Fact]
    public void GetNavigationTree_ShouldReturnRootNode()
    {
        var result = _sut.GetNavigationTree();

        result.Should().NotBeNull();
        result.NodeType.Should().Be(TreeNodeType.Investments);
        result.DisplayName.Should().Be("All Investments");
        result.Children.Should().NotBeEmpty();
    }

    [Fact]
    public void GetNavigationTree_ShouldContainBrokerNodes()
    {
        var result = _sut.GetNavigationTree();

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
        var result = _sut.GetNavigationTree();
        var brokerNode = result.Children.First();

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
        var result = _sut.GetNavigationTree();
        var brokerNode = result.Children.First();
        var portfolioNode = brokerNode.Children.First();

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
        var result = _sut.GetBrokers().ToList();

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
        var result = _sut.GetBrokers().ToList();
        var broker = result.First();

        broker.Portfolios.Should().NotBeEmpty();
        broker.PortfolioCount.Should().Be(broker.Portfolios.Count);
    }

    [Fact]
    public void GetBrokers_PortfoliosShouldHaveAssets()
    {
        var result = _sut.GetBrokers().ToList();
        var broker = result.First();
        var portfolio = broker.Portfolios.First();

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
        var result = _sut.GetAssetDetails(broker!, portfolio!, asset!);

        result.Should().BeNull();
    }

    [Fact]
    public void GetAssetDetails_WithNonExistentAsset_ReturnsNull()
    {
        var result = _sut.GetAssetDetails("XPI", "Default", "NONEXISTENT");

        result.Should().BeNull();
    }

    [Fact]
    public void GetAssetDetails_WithValidParameters_ReturnsAssetDetails()
    {
        const string brokerName = "XPI";
        const string portfolioName = "Default";
        const string assetName = "BCIA11";

        var result = _sut.GetAssetDetails(brokerName, portfolioName, assetName);

        result.Should().NotBeNull();
        result!.Name.Should().Be(assetName);
        result.BrokerName.Should().Be(brokerName);
        result.PortfolioName.Should().Be(portfolioName);
        result.Ticker.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void GetAssetDetails_ShouldIncludeTransactions()
    {
        const string brokerName = "XPI";
        const string portfolioName = "Default";
        const string assetName = "BCIA11";

        var result = _sut.GetAssetDetails(brokerName, portfolioName, assetName);

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
        const string brokerName = "XPI";
        const string portfolioName = "Default";
        const string assetName = "BCIA11";

        var result = _sut.GetAssetDetails(brokerName, portfolioName, assetName);

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
        const string brokerName = "XPI";
        const string portfolioName = "Default";
        const string assetName = "BCIA11";

        var result = _sut.GetAssetDetails(brokerName, portfolioName, assetName);

        result.Should().NotBeNull();
        result!.CashFlowsWithCredits.Should().HaveCount(result.Transactions.Count + result.Credits.Count);
    }

    [Fact]
    public void GetAssetDetails_ShouldIncludeCashFlowsWithoutCredits_ExcludingCredits()
    {
        const string brokerName = "XPI";
        const string portfolioName = "Default";
        const string assetName = "BCIA11";

        var result = _sut.GetAssetDetails(brokerName, portfolioName, assetName);

        result.Should().NotBeNull();
        result!.CashFlowsWithoutCredits.Should().HaveCount(result.Transactions.Count);
    }

    [Fact]
    public void GetAssetDetails_ShouldIncludePriceHistory()
    {
        const string brokerName = "XPI";
        const string portfolioName = "Default";
        const string assetName = "BCIA11";
        var date = new DateOnly(2026, 8, 15);
        _repository.GetAsset(brokerName, portfolioName, assetName)!.SetPrice(date, 123.45m, isManual: true);

        var result = _sut.GetAssetDetails(brokerName, portfolioName, assetName);

        result.Should().NotBeNull();
        result!.PriceHistory.Should().ContainSingle(p => p.Date == date && p.Price == 123.45m && p.IsManual);
    }

    [Fact]
    public void GetAssetDetails_WithNoPriceHistory_ReturnsEmptyList()
    {
        const string brokerName = "XPI";
        const string portfolioName = "Default";
        const string assetName = "BCIA11";

        var result = _sut.GetAssetDetails(brokerName, portfolioName, assetName);

        result.Should().NotBeNull();
        result!.PriceHistory.Should().BeEmpty();
    }

    [Fact]
    public void GetAssetDetails_ShouldCalculateTotalsCorrectly()
    {
        const string brokerName = "XPI";
        const string portfolioName = "Default";
        const string assetName = "BCIA11";

        var result = _sut.GetAssetDetails(brokerName, portfolioName, assetName);

        result.Should().NotBeNull();
        result!.TotalBought.Should().BeGreaterThan(0);
        result.TotalCredits.Should().BeGreaterThan(0);
        var expectedTotalCredits = result.Credits.Sum(c => c.Value);
        result.TotalCredits.Should().Be(expectedTotalCredits);
    }

    [Fact]
    public void GetAssetDetails_TransactionsShouldBeOrderedByDateDescending()
    {
        const string brokerName = "XPI";
        const string portfolioName = "Default";
        const string assetName = "BCIA11";

        var result = _sut.GetAssetDetails(brokerName, portfolioName, assetName);

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
        const string brokerName = "XPI";
        const string portfolioName = "Default";
        const string assetName = "BCIA11";

        var result = _sut.GetAssetDetails(brokerName, portfolioName, assetName);

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
        var result = _creditSut.GetCreditsByBroker(brokerName!);

        result.Should().BeEmpty();
    }

    [Theory]
    [InlineData(null, "Default")]
    [InlineData("", "Default")]
    [InlineData("XPI", null)]
    [InlineData("XPI", "")]
    public void GetCreditsByPortfolio_WithInvalidParameters_ReturnsEmpty(string? brokerName, string? portfolioName)
    {
        var result = _creditSut.GetCreditsByPortfolio(brokerName!, portfolioName!);

        result.Should().BeEmpty();
    }

    [Fact]
    public void GetCreditsByBroker_ShouldReturnCredits()
    {
        const string brokerName = "XPI";

        var result = _creditSut.GetCreditsByBroker(brokerName);

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
        const string brokerName = "XPI";
        const string portfolioName = "Default";

        var result = _creditSut.GetCreditsByPortfolio(brokerName, portfolioName);

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
        const string brokerName = "XPI";

        var result = _creditSut.GetCreditsByBroker(brokerName);

        if (result.Count > 1)
        {
            for (int i = 0; i < result.Count - 1; i++)
            {
                result[i].Date.Should().BeOnOrAfter(result[i + 1].Date);
            }
        }
    }

}

