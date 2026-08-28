using Financial.Investment.Application.DTOs;
using Financial.Investment.Application.Services;
using Financial.Investment.Infrastructure.Persistence;
using Financial.Shared.Infrastructure.Persistence;
using Financial.Investment.Infrastructure.Repositories;
using Financial.TestUtilities;
using FluentAssertions;
using System.IO;
using Microsoft.Extensions.Logging.Abstractions;

namespace Financial.Investment.Infrastructure.Tests.Services;

public class AssetPriceHistoryServiceTests
{
    private const string BrokerName = "XPI";
    private const string PortfolioName = "Default";
    private const string AssetName = "BCIA11";

    [Fact]
    public async Task SetPriceAsync_NewDate_AddsManualEntry()
    {
        var (service, tempFile) = CreateService();
        try
        {
            var date = new DateOnly(2026, 8, 15);

            var result = await service.SetPriceAsync(new SetAssetPriceDTO
            {
                BrokerName = BrokerName,
                PortfolioName = PortfolioName,
                AssetName = AssetName,
                Date = date,
                Price = 1234.56m
            });

            result.Should().NotBeNull();
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public async Task SetPriceAsync_ExistingDate_ReplacesEntry()
    {
        var (service, tempFile) = CreateService();
        try
        {
            var date = new DateOnly(2026, 8, 15);
            await service.SetPriceAsync(new SetAssetPriceDTO
            {
                BrokerName = BrokerName,
                PortfolioName = PortfolioName,
                AssetName = AssetName,
                Date = date,
                Price = 100m
            });

            var result = await service.SetPriceAsync(new SetAssetPriceDTO
            {
                BrokerName = BrokerName,
                PortfolioName = PortfolioName,
                AssetName = AssetName,
                Date = date,
                Price = 150m
            });

            result.Should().NotBeNull();
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public async Task SetPriceAsync_UnknownAsset_ReturnsNull()
    {
        var (service, tempFile) = CreateService();
        try
        {
            var result = await service.SetPriceAsync(new SetAssetPriceDTO
            {
                BrokerName = BrokerName,
                PortfolioName = PortfolioName,
                AssetName = "NoSuchAsset",
                Date = new DateOnly(2026, 8, 15),
                Price = 100m
            });

            result.Should().BeNull();
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public async Task SetPriceAsync_ZeroPrice_Throws()
    {
        var (service, tempFile) = CreateService();
        try
        {
            Func<Task> act = () => service.SetPriceAsync(new SetAssetPriceDTO
            {
                BrokerName = BrokerName,
                PortfolioName = PortfolioName,
                AssetName = AssetName,
                Date = new DateOnly(2026, 8, 15),
                Price = 0m
            });

            await act.Should().ThrowAsync<ArgumentException>().WithMessage("Price must be greater than zero.");
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public async Task SetPriceAsync_FutureDate_Throws()
    {
        var (service, tempFile) = CreateService();
        try
        {
            Func<Task> act = () => service.SetPriceAsync(new SetAssetPriceDTO
            {
                BrokerName = BrokerName,
                PortfolioName = PortfolioName,
                AssetName = AssetName,
                Date = DateOnly.FromDateTime(DateTime.Today).AddDays(1),
                Price = 100m
            });

            await act.Should().ThrowAsync<ArgumentException>().WithMessage("Price date cannot be in the future.");
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public async Task DeletePriceAsync_ManualEntry_RemovesIt()
    {
        var (service, tempFile) = CreateService();
        try
        {
            var date = new DateOnly(2026, 8, 15);
            await service.SetPriceAsync(new SetAssetPriceDTO
            {
                BrokerName = BrokerName,
                PortfolioName = PortfolioName,
                AssetName = AssetName,
                Date = date,
                Price = 100m
            });

            var result = await service.DeletePriceAsync(new DeleteAssetPriceDTO
            {
                BrokerName = BrokerName,
                PortfolioName = PortfolioName,
                AssetName = AssetName,
                Date = date
            });

            result.Should().NotBeNull();
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public async Task DeletePriceAsync_NoEntryForDate_IsNoOpAndReturnsDetails()
    {
        var (service, tempFile) = CreateService();
        try
        {
            var result = await service.DeletePriceAsync(new DeleteAssetPriceDTO
            {
                BrokerName = BrokerName,
                PortfolioName = PortfolioName,
                AssetName = AssetName,
                Date = new DateOnly(2026, 8, 15)
            });

            result.Should().NotBeNull();
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public async Task DeletePriceAsync_AutomaticEntry_Throws()
    {
        var (service, repository, tempFile) = CreateServiceWithRepository();
        try
        {
            var date = new DateOnly(2026, 8, 15);
            repository.GetAsset(BrokerName, PortfolioName, AssetName)!.SetPrice(date, 100m, isManual: false);
            await repository.ApplyAndSaveAsync(() => true);

            Func<Task> act = () => service.DeletePriceAsync(new DeleteAssetPriceDTO
            {
                BrokerName = BrokerName,
                PortfolioName = PortfolioName,
                AssetName = AssetName,
                Date = date
            });

            await act.Should().ThrowAsync<ArgumentException>()
                .WithMessage("Automatic price entries can't be edited directly*");
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public async Task DeletePriceAsync_UnknownAsset_ReturnsNull()
    {
        var (service, tempFile) = CreateService();
        try
        {
            var result = await service.DeletePriceAsync(new DeleteAssetPriceDTO
            {
                BrokerName = BrokerName,
                PortfolioName = PortfolioName,
                AssetName = "NoSuchAsset",
                Date = new DateOnly(2026, 8, 15)
            });

            result.Should().BeNull();
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    private static (AssetPriceHistoryService Service, string TempFile) CreateService()
    {
        var (service, _, tempFile) = CreateServiceWithRepository();
        return (service, tempFile);
    }

    /// <summary>Loads a repository over a private copy of the test data file, so a test that mutates
    /// it cannot affect any other.</summary>
    private static (InvestmentJsonRepository Repository, RecordingTelemetryTracer Tracer, string TempFile) CreateRepositoryOverTempCopy()
    {
        var tempFile = Path.Combine(Path.GetTempPath(), $"data.test.{Guid.NewGuid():N}.json");
        File.Copy(TestDataPaths.DataJsonFile, tempFile, true);

        var storage = new LocalJsonStorage(tempFile);
        var serializer = new InvestmentSerializerAdapter();
        return (new InvestmentJsonRepository(InvestmentLoader.LoadSync(storage, serializer), storage, serializer),
            new RecordingTelemetryTracer(), tempFile);
    }

    private static (AssetPriceHistoryService Service, InvestmentJsonRepository Repository, string TempFile) CreateServiceWithRepository()
    {
        var (repository, tracer, tempFile) = CreateRepositoryOverTempCopy();
        var navigationService = new NavigationService(repository, tracer, NullLogger<NavigationService>.Instance);
        var service = new AssetPriceHistoryService(repository, navigationService, tracer, NullLogger<AssetPriceHistoryService>.Instance);

        return (service, repository, tempFile);
    }
}
