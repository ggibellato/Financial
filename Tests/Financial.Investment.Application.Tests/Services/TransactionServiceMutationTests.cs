using Financial.Investment.Application.DTOs;
using Financial.Investment.Application.Enums;
using Financial.Investment.Application.Services;
using Financial.Shared.Abstractions.Observability;
using Financial.TestUtilities;
using Financial.Investment.Domain.Entities;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;

namespace Financial.Investment.Application.Tests.Services;

public class TransactionServiceMutationTests
{
    private static readonly ITelemetryTracer Tracer = new RecordingTelemetryTracer();

    private readonly StubInvestmentRepository _repository = new();

    [Fact]
    public void Constructor_WithNullNavigationService_Throws()
    {
        Action act = () => new TransactionService(_repository, null!, Tracer, NullLogger<TransactionService>.Instance);
        act.Should().Throw<ArgumentNullException>().WithParameterName("navigationService");
    }

    [Fact]
    public void Constructor_WithNullTracer_Throws()
    {
        Action act = () => new TransactionService(_repository, new NavigationService(_repository, Tracer, NullLogger<NavigationService>.Instance), null!, NullLogger<TransactionService>.Instance);
        act.Should().Throw<ArgumentNullException>().WithParameterName("tracer");
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
        _repository.WriteCallCount.Should().Be(1);
    }

    [Fact]
    public async Task AddTransactionAsync_ValidRequest_RecordsSuccessfulSpan()
    {
        _repository.Asset = MakeAsset();
        var tracer = new RecordingTelemetryTracer();
        var service = new TransactionService(_repository, new NavigationService(_repository, Tracer, NullLogger<NavigationService>.Instance), tracer, NullLogger<TransactionService>.Instance);

        await service.AddTransactionAsync(new TransactionCreateDTO
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

        var span = tracer.Spans.Should().ContainSingle().Which;
        span.Name.Should().Be("Investment.TransactionService.AddTransaction");
        span.Attributes[TelemetryAttributeKeys.BoundedContext].Should().Be("Investment");
        span.Attributes[TelemetryAttributeKeys.EntityType].Should().Be("Transaction");
        span.Attributes[TelemetryAttributeKeys.OperationResult].Should().Be(TelemetryOperationResults.Success);
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
        _repository.WriteCallCount.Should().Be(0);
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
        _repository.WriteCallCount.Should().Be(0);
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

    private TransactionService CreateService() => new(_repository, new NavigationService(_repository, Tracer, NullLogger<NavigationService>.Instance), Tracer, NullLogger<TransactionService>.Instance);

    private static Asset MakeAsset(string name = "AAAA") =>
        Asset.Create(name, "ISIN", "BVMF", name);


    [Fact]
    public void Constructor_WithNullLogger_Throws()
    {
        Action act = () => new TransactionService(_repository, new NavigationService(_repository, Tracer, NullLogger<NavigationService>.Instance), Tracer, null!);

        act.Should().Throw<ArgumentNullException>();
    }
}
