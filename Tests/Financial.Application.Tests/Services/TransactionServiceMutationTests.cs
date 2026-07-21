using Financial.Application.DTOs;
using Financial.Application.Enums;
using Financial.Application.Interfaces;
using Financial.Application.Services;
using Financial.Domain.Entities;
using FluentAssertions;

namespace Financial.Application.Tests.Services;

public class TransactionServiceMutationTests
{
    private readonly StubRepository _repository = new();

    [Fact]
    public void Constructor_WithNullNavigationService_Throws()
    {
        Action act = () => new TransactionService(_repository, null!);
        act.Should().Throw<ArgumentNullException>().WithParameterName("navigationService");
    }

    [Fact]
    public async Task AddTransactionAsync_ValidRequest_AddsTransactionAndReturnsAssetDetails()
    {
        var asset = MakeAsset();
        _repository.Asset = asset;

        var result = await CreateService().AddTransactionAsync(new TransactionCreateDTO
        {
            BrokerName = "XPI",
            PortfolioName = "Default",
            AssetName = "AAAA",
            Date = new DateTime(2024, 1, 1),
            Type = "Buy",
            Quantity = 10m,
            UnitPrice = 5m,
            Fees = 0m
        });

        result.Should().NotBeNull();
        asset.Transactions.Should().ContainSingle();
        _repository.SaveChangesCallCount.Should().Be(1);
    }

    [Fact]
    public async Task AddTransactionAsync_InvalidTransactionType_ReturnsNull()
    {
        _repository.Asset = MakeAsset();

        var result = await CreateService().AddTransactionAsync(new TransactionCreateDTO
        {
            BrokerName = "XPI",
            PortfolioName = "Default",
            AssetName = "AAAA",
            Type = "NotARealType",
            Quantity = 10m,
            UnitPrice = 5m
        });

        result.Should().BeNull();
        _repository.SaveChangesCallCount.Should().Be(0);
    }

    [Fact]
    public async Task AddTransactionAsync_BlankAssetName_ReturnsNull()
    {
        _repository.Asset = MakeAsset();

        var result = await CreateService().AddTransactionAsync(new TransactionCreateDTO
        {
            BrokerName = "XPI",
            PortfolioName = "Default",
            AssetName = "",
            Type = "Buy",
            Quantity = 10m,
            UnitPrice = 5m
        });

        result.Should().BeNull();
    }

    [Fact]
    public async Task AddTransactionAsync_AssetNotFound_ReturnsNull()
    {
        _repository.Asset = null;

        var result = await CreateService().AddTransactionAsync(new TransactionCreateDTO
        {
            BrokerName = "XPI",
            PortfolioName = "Default",
            AssetName = "UNKNOWN",
            Type = "Buy",
            Quantity = 10m,
            UnitPrice = 5m
        });

        result.Should().BeNull();
    }

    [Fact]
    public async Task UpdateTransactionAsync_EmptyId_ReturnsNull()
    {
        _repository.Asset = MakeAsset();

        var result = await CreateService().UpdateTransactionAsync(new TransactionUpdateDTO
        {
            BrokerName = "XPI",
            PortfolioName = "Default",
            AssetName = "AAAA",
            Id = Guid.Empty,
            Type = "Buy",
            Quantity = 10m,
            UnitPrice = 5m
        });

        result.Should().BeNull();
    }

    [Fact]
    public async Task UpdateTransactionAsync_ExistingId_UpdatesAndReturnsAssetDetails()
    {
        var asset = MakeAsset();
        var txId = Guid.NewGuid();
        asset.AddTransaction(Transaction.CreateWithId(txId, new DateTime(2024, 1, 1), Transaction.TransactionType.Buy, 10m, 5m, 0m));
        _repository.Asset = asset;

        var result = await CreateService().UpdateTransactionAsync(new TransactionUpdateDTO
        {
            BrokerName = "XPI",
            PortfolioName = "Default",
            AssetName = "AAAA",
            Id = txId,
            Date = new DateTime(2024, 1, 1),
            Type = "Buy",
            Quantity = 20m,
            UnitPrice = 5m
        });

        result.Should().NotBeNull();
        asset.Quantity.Should().Be(20m);
    }

    [Fact]
    public async Task UpdateTransactionAsync_UnknownId_ReturnsNull()
    {
        _repository.Asset = MakeAsset();

        var result = await CreateService().UpdateTransactionAsync(new TransactionUpdateDTO
        {
            BrokerName = "XPI",
            PortfolioName = "Default",
            AssetName = "AAAA",
            Id = Guid.NewGuid(),
            Type = "Buy",
            Quantity = 10m,
            UnitPrice = 5m
        });

        result.Should().BeNull();
        _repository.SaveChangesCallCount.Should().Be(0);
    }

    [Fact]
    public async Task DeleteTransactionAsync_EmptyId_ReturnsNull()
    {
        _repository.Asset = MakeAsset();

        var result = await CreateService().DeleteTransactionAsync(new TransactionDeleteDTO
        {
            BrokerName = "XPI",
            PortfolioName = "Default",
            AssetName = "AAAA",
            Id = Guid.Empty
        });

        result.Should().BeNull();
    }

    [Fact]
    public async Task DeleteTransactionAsync_ExistingId_RemovesAndReturnsAssetDetails()
    {
        var asset = MakeAsset();
        var txId = Guid.NewGuid();
        asset.AddTransaction(Transaction.CreateWithId(txId, new DateTime(2024, 1, 1), Transaction.TransactionType.Buy, 10m, 5m, 0m));
        _repository.Asset = asset;

        var result = await CreateService().DeleteTransactionAsync(new TransactionDeleteDTO
        {
            BrokerName = "XPI",
            PortfolioName = "Default",
            AssetName = "AAAA",
            Id = txId
        });

        result.Should().NotBeNull();
        asset.Transactions.Should().BeEmpty();
    }

    [Fact]
    public async Task DeleteTransactionAsync_UnknownId_ReturnsNull()
    {
        _repository.Asset = MakeAsset();

        var result = await CreateService().DeleteTransactionAsync(new TransactionDeleteDTO
        {
            BrokerName = "XPI",
            PortfolioName = "Default",
            AssetName = "AAAA",
            Id = Guid.NewGuid()
        });

        result.Should().BeNull();
    }

    private TransactionService CreateService() => new(_repository, new NavigationService(_repository));

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
