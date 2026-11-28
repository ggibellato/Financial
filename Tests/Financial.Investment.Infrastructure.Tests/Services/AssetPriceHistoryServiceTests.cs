using Financial.Investment.Application.DTOs;
using Financial.Investment.Application.Services;
using Financial.Investment.Domain.Entities;
using Financial.Investment.Infrastructure.Persistence;
using Financial.Shared.Infrastructure.Persistence;
using Financial.Investment.Infrastructure.Repositories;
using Financial.TestUtilities;
using FluentAssertions;
using System.IO;
using Microsoft.Extensions.Logging.Abstractions;

namespace Financial.Investment.Infrastructure.Tests.Services;

[Trait("Category", "Integration")]
public class AssetPriceHistoryServiceTests
{
    private const string BrokerName = "XPI";
    private const string PortfolioName = "Default";
    private const string AssetName = "BCIA11";
    private const string NewAssetName = "NEWASSET";

    [Fact]
    public async Task SetPriceAsync_NewDate_AddsManualEntryInTheGivenCurrency()
    {
        var (service, tracer, tempFile) = CreateService();
        try
        {
            var date = new DateOnly(2026, 8, 15);

            var result = await service.SetPriceAsync(new SetAssetPriceDTO
            {
                BrokerName = BrokerName,
                PortfolioName = PortfolioName,
                AssetName = AssetName,
                Date = date,
                Price = 1234.56m,
                Currency = "GBP"
            });

            var entry = result!.PriceSnapshots.Should().ContainSingle(p => p.Date == date).Subject;
            entry.Price.Should().Be(1234.56m);
            entry.IsManual.Should().BeTrue();
            entry.Currency.Should().Be("GBP");
            tracer.ShouldHaveSucceededSpan("Investment.AssetPriceHistoryService.SetPrice");
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public async Task SetPriceAsync_ExistingDate_ReplacesEntry()
    {
        var (service, _, tempFile) = CreateService();
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

            result!.PriceSnapshots.Should().ContainSingle(p => p.Date == date).Which.Price.Should().Be(150m);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public async Task SetPriceAsync_UnknownAsset_ReturnsNull()
    {
        var (service, _, tempFile) = CreateService();
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

    [Theory]
    [InlineData(0, 0, "Price must be greater than zero.")]
    [InlineData(100, 1, "Price date cannot be in the future.")]
    public async Task SetPriceAsync_InvalidPriceOrDate_ThrowsAndRecordsAFailedSpan(int price, int daysAfterToday, string message)
    {
        var (service, tracer, tempFile) = CreateService();
        try
        {
            Func<Task> act = () => service.SetPriceAsync(new SetAssetPriceDTO
            {
                BrokerName = BrokerName,
                PortfolioName = PortfolioName,
                AssetName = AssetName,
                Date = TestClock.Today.AddDays(daysAfterToday),
                Price = price
            });

            await act.Should().ThrowAsync<ArgumentException>().WithMessage(message);
            tracer.ShouldHaveFailedSpan<ArgumentException>("Investment.AssetPriceHistoryService.SetPrice");
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public async Task DeletePriceAsync_ManualEntry_RemovesIt()
    {
        var (service, tracer, tempFile) = CreateService();
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

            result!.PriceSnapshots.Should().NotContain(p => p.Date == date);
            tracer.ShouldHaveSucceededSpan("Investment.AssetPriceHistoryService.DeletePrice");
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public async Task DeletePriceAsync_NoEntryForDate_IsNoOpAndReturnsDetails()
    {
        var (service, _, tempFile) = CreateService();
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
        var (service, repository, tracer, tempFile) = CreateServiceWithRepository();
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
            tracer.ShouldHaveFailedSpan<ArgumentException>("Investment.AssetPriceHistoryService.DeletePrice");
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public async Task DeletePriceAsync_UnknownAsset_ReturnsNull()
    {
        var (service, _, tempFile) = CreateService();
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

    [Theory]
    [InlineData(ValuationMethod.ProviderValue)]
    [InlineData(ValuationMethod.Manual)]
    public async Task SetPriceAsync_BalanceTrackedHolding_RecordsProviderValuationSource(ValuationMethod valuationMethod)
    {
        var (service, repository, _, tempFile) = CreateServiceWithRepository();
        try
        {
            var asset = Asset.Create(NewAssetName, string.Empty, "BVMF", NewAssetName);
            asset.SetValuationMethod(valuationMethod);
            repository.GetInvestments().GetBroker(BrokerName, preferHistoric: false).GetPortfolio(PortfolioName).RegisterAsset(asset);

            var result = await service.SetPriceAsync(new SetAssetPriceDTO
            {
                BrokerName = BrokerName,
                PortfolioName = PortfolioName,
                AssetName = NewAssetName,
                Date = new DateOnly(2026, 8, 15),
                Price = 5000m
            });

            result!.PriceSnapshots.Should().ContainSingle(p => p.Date == new DateOnly(2026, 8, 15) && p.Source == PriceSource.ProviderValuation && p.IsManual);

            var deleted = await service.DeletePriceAsync(new DeleteAssetPriceDTO
            {
                BrokerName = BrokerName,
                PortfolioName = PortfolioName,
                AssetName = NewAssetName,
                Date = new DateOnly(2026, 8, 15)
            });

            deleted!.PriceSnapshots.Should().BeEmpty();
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public async Task SetPriceAsync_AssetLeftAtDefaultValuationMethod_RecordsManualSource()
    {
        var (service, _, tempFile) = CreateService();
        try
        {
            var result = await service.SetPriceAsync(new SetAssetPriceDTO
            {
                BrokerName = BrokerName,
                PortfolioName = PortfolioName,
                AssetName = AssetName,
                Date = new DateOnly(2026, 8, 15),
                Price = 100m
            });

            result!.PriceSnapshots.Should().ContainSingle(p => p.Date == new DateOnly(2026, 8, 15) && p.Source == PriceSource.Manual && p.Currency == string.Empty);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    private static (AssetPriceHistoryService Service, RecordingTelemetryTracer Tracer, string TempFile) CreateService()
    {
        var (service, _, tracer, tempFile) = CreateServiceWithRepository();
        return (service, tracer, tempFile);
    }

    /// <summary>Loads a repository over a private copy of the test data file, so a test that mutates
    /// it cannot affect any other.</summary>
    private static (InvestmentJsonRepository Repository, RecordingTelemetryTracer Tracer, string TempFile) CreateRepositoryOverTempCopy()
    {
        var tempFile = Path.Combine(Path.GetTempPath(), $"data.test.{Guid.NewGuid():N}.json");
        File.Copy(TestDataPaths.DataJsonFile, tempFile, true);

        var storage = new LocalJsonStorage(tempFile);
        var serializer = new InvestmentSerializerAdapter();
        return (new InvestmentJsonRepository(InvestmentLoader.LoadSync(storage, serializer, TestClock.At()), storage, serializer),
            new RecordingTelemetryTracer(), tempFile);
    }

    private static (AssetPriceHistoryService Service, InvestmentJsonRepository Repository, RecordingTelemetryTracer Tracer, string TempFile) CreateServiceWithRepository()
    {
        var (repository, tracer, tempFile) = CreateRepositoryOverTempCopy();
        var navigationService = new NavigationService(repository, TestHoldingValuationService.Create(), tracer, NullLogger<NavigationService>.Instance);
        var service = new AssetPriceHistoryService(repository, navigationService, TestClock.At(), tracer, NullLogger<AssetPriceHistoryService>.Instance);

        return (service, repository, tracer, tempFile);
    }
}
