using Financial.Application.DTOs;
using Financial.Application.Enums;
using Financial.Application.Interfaces;
using Financial.Application.Services;
using Financial.Domain.Entities;
using FluentAssertions;

namespace Financial.Application.Tests.Services;

public class CreditServiceTests
{
    private readonly StubRepository _repository = new();

    [Fact]
    public void Constructor_WithNullRepository_Throws()
    {
        Action act = () => new CreditService(null!, new NavigationService(_repository));
        act.Should().Throw<ArgumentNullException>().WithParameterName("repository");
    }

    [Fact]
    public void Constructor_WithNullNavigationService_Throws()
    {
        Action act = () => new CreditService(_repository, null!);
        act.Should().Throw<ArgumentNullException>().WithParameterName("navigationService");
    }

    [Fact]
    public async Task AddCreditAsync_ValidRequest_AddsCreditAndReturnsAssetDetails()
    {
        var asset = MakeAsset();
        _repository.Asset = asset;

        var result = await CreateService().AddCreditAsync(new CreditCreateDTO
        {
            BrokerName = "XPI",
            PortfolioName = "Default",
            AssetName = "AAAA",
            Date = new DateTime(2024, 1, 1),
            Type = "Dividend",
            Value = 10m
        });

        result.Should().NotBeNull();
        asset.Credits.Should().ContainSingle(c => c.Value == 10m);
        _repository.SaveChangesCallCount.Should().Be(1);
    }

    [Fact]
    public async Task AddCreditAsync_InvalidCreditType_ReturnsNull()
    {
        _repository.Asset = MakeAsset();

        var result = await CreateService().AddCreditAsync(new CreditCreateDTO
        {
            BrokerName = "XPI",
            PortfolioName = "Default",
            AssetName = "AAAA",
            Type = "NotARealType",
            Value = 10m
        });

        result.Should().BeNull();
        _repository.SaveChangesCallCount.Should().Be(0);
    }

    [Fact]
    public async Task AddCreditAsync_BlankBrokerName_ReturnsNull()
    {
        _repository.Asset = MakeAsset();

        var result = await CreateService().AddCreditAsync(new CreditCreateDTO
        {
            BrokerName = "",
            PortfolioName = "Default",
            AssetName = "AAAA",
            Type = "Dividend",
            Value = 10m
        });

        result.Should().BeNull();
    }

    [Fact]
    public async Task AddCreditAsync_AssetNotFound_ReturnsNull()
    {
        _repository.Asset = null;

        var result = await CreateService().AddCreditAsync(new CreditCreateDTO
        {
            BrokerName = "XPI",
            PortfolioName = "Default",
            AssetName = "UNKNOWN",
            Type = "Dividend",
            Value = 10m
        });

        result.Should().BeNull();
    }

    [Fact]
    public async Task UpdateCreditAsync_EmptyId_ReturnsNull()
    {
        _repository.Asset = MakeAsset();

        var result = await CreateService().UpdateCreditAsync(new CreditUpdateDTO
        {
            BrokerName = "XPI",
            PortfolioName = "Default",
            AssetName = "AAAA",
            Id = Guid.Empty,
            Type = "Dividend",
            Value = 10m
        });

        result.Should().BeNull();
    }

    [Fact]
    public async Task UpdateCreditAsync_ExistingId_UpdatesAndReturnsAssetDetails()
    {
        var asset = MakeAsset();
        var creditId = Guid.NewGuid();
        asset.AddCredit(Credit.CreateWithId(creditId, new DateTime(2024, 1, 1), Credit.CreditType.Dividend, 5m));
        _repository.Asset = asset;

        var result = await CreateService().UpdateCreditAsync(new CreditUpdateDTO
        {
            BrokerName = "XPI",
            PortfolioName = "Default",
            AssetName = "AAAA",
            Id = creditId,
            Date = new DateTime(2024, 1, 1),
            Type = "Dividend",
            Value = 25m
        });

        result.Should().NotBeNull();
        asset.Credits.Should().ContainSingle().Which.Value.Should().Be(25m);
    }

    [Fact]
    public async Task UpdateCreditAsync_UnknownId_ReturnsNull()
    {
        _repository.Asset = MakeAsset();

        var result = await CreateService().UpdateCreditAsync(new CreditUpdateDTO
        {
            BrokerName = "XPI",
            PortfolioName = "Default",
            AssetName = "AAAA",
            Id = Guid.NewGuid(),
            Type = "Dividend",
            Value = 10m
        });

        result.Should().BeNull();
        _repository.SaveChangesCallCount.Should().Be(0);
    }

    [Fact]
    public async Task DeleteCreditAsync_EmptyId_ReturnsNull()
    {
        _repository.Asset = MakeAsset();

        var result = await CreateService().DeleteCreditAsync(new CreditDeleteDTO
        {
            BrokerName = "XPI",
            PortfolioName = "Default",
            AssetName = "AAAA",
            Id = Guid.Empty
        });

        result.Should().BeNull();
    }

    [Fact]
    public async Task DeleteCreditAsync_ExistingId_RemovesAndReturnsAssetDetails()
    {
        var asset = MakeAsset();
        var creditId = Guid.NewGuid();
        asset.AddCredit(Credit.CreateWithId(creditId, new DateTime(2024, 1, 1), Credit.CreditType.Dividend, 5m));
        _repository.Asset = asset;

        var result = await CreateService().DeleteCreditAsync(new CreditDeleteDTO
        {
            BrokerName = "XPI",
            PortfolioName = "Default",
            AssetName = "AAAA",
            Id = creditId
        });

        result.Should().NotBeNull();
        asset.Credits.Should().BeEmpty();
    }

    [Fact]
    public async Task DeleteCreditAsync_UnknownId_ReturnsNull()
    {
        _repository.Asset = MakeAsset();

        var result = await CreateService().DeleteCreditAsync(new CreditDeleteDTO
        {
            BrokerName = "XPI",
            PortfolioName = "Default",
            AssetName = "AAAA",
            Id = Guid.NewGuid()
        });

        result.Should().BeNull();
    }

    [Fact]
    public void GetCreditsByBroker_ReturnsCreditsFromAsset()
    {
        var asset = MakeAsset();
        asset.AddTransaction(Transaction.Create(DateTime.Today, Transaction.TransactionType.Buy, 1m, 10m, 0m));
        asset.AddCredit(Credit.Create(new DateTime(2024, 1, 1), Credit.CreditType.Dividend, 5m));
        _repository.AssetsByBroker = [asset];

        var result = CreateService().GetCreditsByBroker("XPI");

        result.Should().ContainSingle(c => c.Value == 5m);
    }

    [Fact]
    public void GetCreditsByBroker_IncludesCreditsFromFlatAndShortAssets()
    {
        var flatAsset = MakeAsset("FLAT");
        flatAsset.AddTransaction(Transaction.Create(DateTime.Today, Transaction.TransactionType.Buy, 1m, 10m, 0m));
        flatAsset.AddTransaction(Transaction.Create(DateTime.Today, Transaction.TransactionType.Sell, 1m, 10m, 0m));
        flatAsset.AddCredit(Credit.Create(new DateTime(2024, 1, 1), Credit.CreditType.Dividend, 3m));

        var shortAsset = MakeAsset("SHORT");
        shortAsset.AddTransaction(Transaction.Create(DateTime.Today, Transaction.TransactionType.Sell, 1m, 10m, 0m));
        shortAsset.AddCredit(Credit.Create(new DateTime(2024, 1, 2), Credit.CreditType.Rent, 7m));

        _repository.AssetsByBroker = [flatAsset, shortAsset];

        var result = CreateService().GetCreditsByBroker("XPI");

        result.Should().Contain(c => c.Value == 3m);
        result.Should().Contain(c => c.Value == 7m);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void GetCreditsByBroker_ReturnsEmptyOnNullOrWhitespaceBrokerName(string? brokerName)
    {
        var result = CreateService().GetCreditsByBroker(brokerName!);

        result.Should().BeEmpty();
    }

    [Fact]
    public void GetCreditsByPortfolio_ReturnsCreditsFromAsset()
    {
        var asset = MakeAsset();
        asset.AddTransaction(Transaction.Create(DateTime.Today, Transaction.TransactionType.Buy, 1m, 10m, 0m));
        asset.AddCredit(Credit.Create(new DateTime(2024, 1, 1), Credit.CreditType.Dividend, 5m));
        _repository.AssetsByBrokerPortfolio = [asset];

        var result = CreateService().GetCreditsByPortfolio("XPI", "Default");

        result.Should().ContainSingle(c => c.Value == 5m);
    }

    [Fact]
    public void GetCreditsByPortfolio_IncludesCreditsFromFlatAndShortAssets()
    {
        var flatAsset = MakeAsset("FLAT");
        flatAsset.AddTransaction(Transaction.Create(DateTime.Today, Transaction.TransactionType.Buy, 1m, 10m, 0m));
        flatAsset.AddTransaction(Transaction.Create(DateTime.Today, Transaction.TransactionType.Sell, 1m, 10m, 0m));
        flatAsset.AddCredit(Credit.Create(new DateTime(2024, 1, 1), Credit.CreditType.Dividend, 3m));

        var shortAsset = MakeAsset("SHORT");
        shortAsset.AddTransaction(Transaction.Create(DateTime.Today, Transaction.TransactionType.Sell, 1m, 10m, 0m));
        shortAsset.AddCredit(Credit.Create(new DateTime(2024, 1, 2), Credit.CreditType.Rent, 7m));

        _repository.AssetsByBrokerPortfolio = [flatAsset, shortAsset];

        var result = CreateService().GetCreditsByPortfolio("XPI", "Default");

        result.Should().Contain(c => c.Value == 3m);
        result.Should().Contain(c => c.Value == 7m);
    }

    [Theory]
    [InlineData(null, "Default")]
    [InlineData("XPI", null)]
    public void GetCreditsByPortfolio_ReturnsEmptyOnNullOrWhitespaceParameters(string? brokerName, string? portfolioName)
    {
        var result = CreateService().GetCreditsByPortfolio(brokerName!, portfolioName!);

        result.Should().BeEmpty();
    }

    private CreditService CreateService() => new(_repository, new NavigationService(_repository));

    private static Asset MakeAsset(string name = "AAAA") =>
        Asset.Create(name, "ISIN", "BVMF", name);

    private sealed class StubRepository : IRepository
    {
        public Asset? Asset { get; set; }
        public IEnumerable<Asset> AssetsByBroker { get; set; } = [];
        public IEnumerable<Asset> AssetsByBrokerPortfolio { get; set; } = [];
        public IEnumerable<Broker> Brokers { get; set; } = [];
        public int SaveChangesCallCount { get; private set; }

        public IEnumerable<Asset> GetAssetsByBroker(string name, InvestmentScope scope = InvestmentScope.Active) => AssetsByBroker;
        public IEnumerable<Asset> GetAssetsByBrokerPortfolio(string broker, string portfolio, InvestmentScope scope = InvestmentScope.Active) => AssetsByBrokerPortfolio;
        public IEnumerable<Broker> GetBrokerList(InvestmentScope scope = InvestmentScope.Active) => Brokers;
        public Asset? GetAsset(string brokerName, string portfolioName, string assetName, InvestmentScope scope = InvestmentScope.Active) => Asset;

        public Task SaveChangesAsync()
        {
            SaveChangesCallCount++;
            return Task.CompletedTask;
        }
    }
}
