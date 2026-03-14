using System;
using System.IO;
using System.Linq;
using Financial.Application.DTOs;
using Financial.Infrastructure.Persistence;
using Financial.Infrastructure.Repositories;
using FluentAssertions;

namespace Financial.Infrastructure.Tests;

public class OperationServiceTests
{
    [Fact]
    public void AddOperation_WithValidRequest_ReturnsDetailsWithNewOperation()
    {
        var (service, tempFile) = CreateService();
        try
        {
            var request = new OperationCreateDTO
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

            var result = service.AddOperation(request);

            result.Should().NotBeNull();
            result!.Operations.Should().ContainSingle(op =>
                op.Date == request.Date &&
                op.Type == request.Type &&
                op.Quantity == request.Quantity &&
                op.UnitPrice == request.UnitPrice &&
                op.Fees == request.Fees &&
                op.Id != Guid.Empty);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public void UpdateOperation_WithValidRequest_UpdatesOperation()
    {
        var (service, tempFile) = CreateService();
        try
        {
            var created = service.AddOperation(new OperationCreateDTO
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

            var operationId = created!.Operations.First(op => op.Date == new DateTime(2024, 1, 3)).Id;

            var updated = service.UpdateOperation(new OperationUpdateDTO
            {
                BrokerName = "XPI",
                PortfolioName = "Default",
                AssetName = "BCIA11",
                Id = operationId,
                Date = new DateTime(2024, 1, 3),
                Type = "Buy",
                Quantity = 3m,
                UnitPrice = 55m,
                Fees = 1.5m
            });

            updated.Should().NotBeNull();
            var updatedOperation = updated!.Operations.Single(op => op.Id == operationId);
            updatedOperation.Quantity.Should().Be(3m);
            updatedOperation.UnitPrice.Should().Be(55m);
            updatedOperation.Fees.Should().Be(1.5m);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public void DeleteOperation_WithValidRequest_RemovesOperation()
    {
        var (service, tempFile) = CreateService();
        try
        {
            var created = service.AddOperation(new OperationCreateDTO
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

            var operationId = created!.Operations.First(op => op.Date == new DateTime(2024, 1, 4)).Id;

            var updated = service.DeleteOperation(new OperationDeleteDTO
            {
                BrokerName = "XPI",
                PortfolioName = "Default",
                AssetName = "BCIA11",
                Id = operationId
            });

            updated.Should().NotBeNull();
            updated!.Operations.Should().NotContain(op => op.Id == operationId);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    private static (OperationService Service, string TempFile) CreateService()
    {
        var tempFile = Path.Combine(Path.GetTempPath(), $"data.test.{Guid.NewGuid():N}.json");
        File.Copy(TestDataPaths.DataJsonFile, tempFile, true);

        var repository = new JSONRepository(new LocalJsonStorage(tempFile));
        var navigationService = new NavigationService(repository);
        var service = new OperationService(repository, navigationService);

        return (service, tempFile);
    }
}
