using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Financial.Application.DTOs;
using Financial.Application.Services;
using Financial.Infrastructure.Persistence;
using Financial.Infrastructure.Repositories;
using FluentAssertions;

namespace Financial.Infrastructure.Tests.Services;

public class TransactionServiceTests
{
    [Fact]
    public async Task AddTransaction_WithValidRequest_ReturnsDetailsWithNewTransaction()
    {
        var (service, tempFile) = CreateService();
        try
        {
            var request = new TransactionCreateDTO
            {
                BrokerName = "XPI",
                PortfolioName = "Default",
                AssetName = "BCIA11",
                Date = new DateTime(2024, 1, 2),
                Type = "Buy",
                Quantity = 1.5m,
                UnitPrice = 100.25m,
                Fees = 2.5m
            };

            var result = await service.AddTransactionAsync(request);

            result.Should().NotBeNull();
            result!.Transactions.Should().ContainSingle(t =>
                t.Date == request.Date &&
                t.Type == request.Type &&
                t.Quantity == request.Quantity &&
                t.UnitPrice == request.UnitPrice &&
                t.Fees == request.Fees &&
                t.Id != Guid.Empty);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public async Task UpdateTransaction_WithValidRequest_UpdatesTransaction()
    {
        var (service, tempFile) = CreateService();
        try
        {
            var created = await service.AddTransactionAsync(new TransactionCreateDTO
            {
                BrokerName = "XPI",
                PortfolioName = "Default",
                AssetName = "BCIA11",
                Date = new DateTime(2024, 1, 3),
                Type = "Buy",
                Quantity = 2m,
                UnitPrice = 50m,
                Fees = 1m
            });

            var transactionId = created!.Transactions.First(t => t.Date == new DateTime(2024, 1, 3)).Id;

            var updated = await service.UpdateTransactionAsync(new TransactionUpdateDTO
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

            updated.Should().NotBeNull();
            var updatedTransaction = updated!.Transactions.Single(t => t.Id == transactionId);
            updatedTransaction.Quantity.Should().Be(3m);
            updatedTransaction.UnitPrice.Should().Be(55m);
            updatedTransaction.Fees.Should().Be(1.5m);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public async Task DeleteTransaction_WithValidRequest_RemovesTransaction()
    {
        var (service, tempFile) = CreateService();
        try
        {
            var created = await service.AddTransactionAsync(new TransactionCreateDTO
            {
                BrokerName = "XPI",
                PortfolioName = "Default",
                AssetName = "BCIA11",
                Date = new DateTime(2024, 1, 4),
                Type = "Sell",
                Quantity = 1m,
                UnitPrice = 120m,
                Fees = 0m
            });

            var transactionId = created!.Transactions.First(t => t.Date == new DateTime(2024, 1, 4)).Id;

            var updated = await service.DeleteTransactionAsync(new TransactionDeleteDTO
            {
                BrokerName = "XPI",
                PortfolioName = "Default",
                AssetName = "BCIA11",
                Id = transactionId
            });

            updated.Should().NotBeNull();
            updated!.Transactions.Should().NotContain(t => t.Id == transactionId);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    private static (TransactionService Service, string TempFile) CreateService()
    {
        var tempFile = Path.Combine(Path.GetTempPath(), $"data.test.{Guid.NewGuid():N}.json");
        File.Copy(TestDataPaths.DataJsonFile, tempFile, true);

        var repository = new JSONRepository(new LocalJsonStorage(tempFile));
        var navigationService = new NavigationService(repository);
        var service = new TransactionService(repository, navigationService);

        return (service, tempFile);
    }
}
