using Financial.Investment.Application.DTOs;
using Financial.Investment.Application.Services;
using Financial.Investment.Domain.Entities;
using Financial.Shared.Abstractions.Currencies;
using Financial.TestUtilities;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;

namespace Financial.Investment.Infrastructure.Tests.Services;

[Trait("Category", "Integration")]
public class TransactionServiceTests : IDisposable
{
    private readonly PersistedInvestmentFile _file = new();
    private readonly TransactionService _service;

    public TransactionServiceTests()
    {
        var repository = _file.OpenRepository();
        var tracer = new RecordingTelemetryTracer();
        var navigationService = new NavigationService(repository, TestHoldingValuationService.Create(), tracer, NullLogger<NavigationService>.Instance);
        IExchangeRateProvider exchangeRateProvider = new StubExchangeRateProvider(0.15m);
        _service = new TransactionService(repository, navigationService, exchangeRateProvider, new StubReportingCurrencyProvider(), TestClock.At(), tracer, NullLogger<TransactionService>.Instance);
    }

    public void Dispose() => _file.Dispose();

    private static TransactionCreateDTO NewTransaction(DateTime date, string type, decimal quantity, decimal unitPrice, decimal fees) => new()
    {
        BrokerName = "XPI",
        PortfolioName = "Default",
        AssetName = "BCIA11",
        Date = date,
        Type = type,
        Quantity = quantity,
        UnitPrice = unitPrice,
        Fees = fees
    };

    private Asset PersistedAsset() => _file.ReloadAsset("XPI", "Default", "BCIA11");

    [Fact]
    public async Task AddTransaction_PersistsTheTransactionToDisk()
    {
        var result = await _service.AddTransactionAsync(NewTransaction(new DateTime(2024, 1, 2), "Buy", 1.5m, 100.25m, 2.5m));

        var transactionId = result!.Transactions.Single(t => t.Date == new DateTime(2024, 1, 2)).Id;
        var persisted = PersistedAsset().Transactions.Single(t => t.Id == transactionId);
        persisted.Type.Should().Be(Transaction.TransactionType.Buy);
        persisted.Quantity.Should().Be(1.5m);
        persisted.UnitPrice.Should().Be(100.25m);
        persisted.Fees.Should().Be(2.5m);
    }

    [Fact]
    public async Task UpdateTransaction_PersistsTheChangedFieldsToDisk()
    {
        var created = await _service.AddTransactionAsync(NewTransaction(new DateTime(2024, 1, 3), "Buy", 2m, 50m, 1m));
        var transactionId = created!.Transactions.Single(t => t.Date == new DateTime(2024, 1, 3)).Id;

        await _service.UpdateTransactionAsync(new TransactionUpdateDTO
        {
            BrokerName = "XPI",
            PortfolioName = "Default",
            AssetName = "BCIA11",
            Id = transactionId,
            Date = new DateTime(2024, 1, 3),
            Type = "Buy",
            Quantity = 3m,
            UnitPrice = 55m,
            Fees = 1.5m
        });

        var persisted = PersistedAsset().Transactions.Single(t => t.Id == transactionId);
        persisted.Quantity.Should().Be(3m);
        persisted.UnitPrice.Should().Be(55m);
        persisted.Fees.Should().Be(1.5m);
    }

    [Fact]
    public async Task DeleteTransaction_RemovesTheTransactionFromDisk()
    {
        var created = await _service.AddTransactionAsync(NewTransaction(new DateTime(2024, 1, 4), "Sell", 1m, 120m, 0m));
        var transactionId = created!.Transactions.Single(t => t.Date == new DateTime(2024, 1, 4)).Id;
        PersistedAsset().Transactions.Should().Contain(t => t.Id == transactionId);

        await _service.DeleteTransactionAsync(new TransactionDeleteDTO
        {
            BrokerName = "XPI",
            PortfolioName = "Default",
            AssetName = "BCIA11",
            Id = transactionId
        });

        PersistedAsset().Transactions.Should().NotContain(t => t.Id == transactionId);
    }

    [Fact]
    public async Task AddTransaction_Sell_PersistsADisposalRecordUsingTheBrokersCostBasisMethod()
    {
        var disposalsBefore = PersistedAsset().DisposalRecords.Count;

        await _service.AddTransactionAsync(NewTransaction(new DateTime(2024, 1, 6), "Sell", 1m, 120m, 0m));

        var disposals = PersistedAsset().DisposalRecords;
        disposals.Should().HaveCountGreaterThan(disposalsBefore);
        disposals.Should().OnlyContain(record => record.Method == CostBasisMethod.AverageCost);
    }
}
