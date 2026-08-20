using Financial.Investment.Application.DTOs;
using Financial.Investment.Application.Enums;
using Financial.Investment.Application.Interfaces;
using Financial.Investment.Application.Services;
using Financial.Investment.Domain.Entities;
using Financial.Investment.Infrastructure.Persistence;
using Financial.Shared.Abstractions;
using Financial.Shared.Infrastructure.Persistence;
using Financial.Investment.Infrastructure.Repositories;
using Financial.TestUtilities;
using FluentAssertions;
using System.IO;
using System.Linq;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Financial.Investment.Infrastructure.Tests.Services;

public class PriceServiceTests
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
            await repository.SaveChangesAsync();

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

    [Fact]
    public async Task GetCurrentPriceAsync_LiveFetchSucceeds_RecordsAutomaticEntryAndReturnsIsManualFalse()
    {
        var (service, repository, tempFile) = CreateServiceWithAssetPriceService(StubAssetPriceService.Success(123.45m));
        try
        {
            var result = await service.GetCurrentPriceAsync(BuildRequest());

            result.Price.Should().Be(123.45m);
            result.IsManual.Should().BeFalse();

            var today = DateOnly.FromDateTime(DateTime.Today);
            var entry = repository.GetAsset(BrokerName, PortfolioName, AssetName)!.GetPriceForDate(today);
            entry.Should().NotBeNull();
            entry!.Price.Should().Be(123.45m);
            entry.IsManual.Should().BeFalse();
            repository.SaveCount.Should().Be(1);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public async Task GetCurrentPriceAsync_LiveFetchSucceeds_SameAutomaticPriceAlreadyRecorded_SkipsSave()
    {
        var (service, repository, tempFile) = CreateServiceWithAssetPriceService(StubAssetPriceService.Success(50m));
        try
        {
            await service.GetCurrentPriceAsync(BuildRequest());
            repository.SaveCount.Should().Be(1);

            var result = await service.GetCurrentPriceAsync(BuildRequest());

            result.Price.Should().Be(50m);
            repository.SaveCount.Should().Be(1);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public async Task GetCurrentPriceAsync_ManualEntryForToday_KeepsItInsteadOfOverwriting()
    {
        var (service, repository, tempFile) = CreateServiceWithAssetPriceService(StubAssetPriceService.Success(200m));
        try
        {
            var today = DateOnly.FromDateTime(DateTime.Today);
            repository.GetAsset(BrokerName, PortfolioName, AssetName)!.SetPrice(today, 100m, isManual: true);
            await repository.SaveChangesAsync();

            var result = await service.GetCurrentPriceAsync(BuildRequest());

            result.Price.Should().Be(100m, "the manual price outranks the scraped one");
            result.IsManual.Should().BeTrue("so the (Manual) badge is shown");

            var entry = repository.GetAsset(BrokerName, PortfolioName, AssetName)!.GetPriceForDate(today);
            entry!.Price.Should().Be(100m);
            entry.IsManual.Should().BeTrue();
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public async Task GetCurrentPriceAsync_ManualEntryForToday_WritesNothing()
    {
        var (service, repository, tempFile) = CreateServiceWithAssetPriceService(StubAssetPriceService.Success(200m));
        try
        {
            repository.GetAsset(BrokerName, PortfolioName, AssetName)!
                .SetPrice(DateOnly.FromDateTime(DateTime.Today), 100m, isManual: true);
            await repository.SaveChangesAsync();
            var savesBefore = repository.SaveCount;

            await service.GetCurrentPriceAsync(BuildRequest());

            repository.SaveCount.Should().Be(savesBefore);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    /// <summary>
    /// The fetch is skipped entirely, since its result would be discarded. Deleting the manual
    /// entry - which the price endpoints do allow - restores automatic pricing.
    /// </summary>
    [Fact]
    public async Task GetCurrentPriceAsync_ManualEntryForToday_DoesNotFetchAtAll()
    {
        var (service, repository, tempFile) = CreateServiceWithAssetPriceService(StubAssetPriceService.NotUsed());
        try
        {
            repository.GetAsset(BrokerName, PortfolioName, AssetName)!
                .SetPrice(DateOnly.FromDateTime(DateTime.Today), 100m, isManual: true);
            await repository.SaveChangesAsync();

            var result = await service.GetCurrentPriceAsync(BuildRequest());

            result.Price.Should().Be(100m);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public async Task GetCurrentPriceAsync_ManualEntryForAnEarlierDate_StillRecordsTodayAutomatically()
    {
        var (service, repository, tempFile) = CreateServiceWithAssetPriceService(StubAssetPriceService.Success(200m));
        try
        {
            var today = DateOnly.FromDateTime(DateTime.Today);
            repository.GetAsset(BrokerName, PortfolioName, AssetName)!
                .SetPrice(today.AddDays(-1), 100m, isManual: true);
            await repository.SaveChangesAsync();

            var result = await service.GetCurrentPriceAsync(BuildRequest());

            result.Price.Should().Be(200m);
            result.IsManual.Should().BeFalse();

            var entry = repository.GetAsset(BrokerName, PortfolioName, AssetName)!.GetPriceForDate(today);
            entry!.Price.Should().Be(200m);
            entry.IsManual.Should().BeFalse();
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public async Task GetCurrentPriceAsync_LiveFetchFails_ManualEntryExistsForToday_ReturnsFallbackIsManualTrue()
    {
        var (service, repository, tempFile) = CreateServiceWithAssetPriceService(StubAssetPriceService.Failure());
        try
        {
            var today = DateOnly.FromDateTime(DateTime.Today);
            repository.GetAsset(BrokerName, PortfolioName, AssetName)!.SetPrice(today, 321.5m, isManual: true);
            await repository.SaveChangesAsync();

            var result = await service.GetCurrentPriceAsync(BuildRequest());

            result.Price.Should().Be(321.5m);
            result.IsManual.Should().BeTrue();
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public async Task GetCurrentPriceAsync_LiveFetchFails_AutomaticEntryExistsForToday_ReturnsFallbackIsManualFalse()
    {
        var (service, repository, tempFile) = CreateServiceWithAssetPriceService(StubAssetPriceService.Failure());
        try
        {
            var today = DateOnly.FromDateTime(DateTime.Today);
            repository.GetAsset(BrokerName, PortfolioName, AssetName)!.SetPrice(today, 88m, isManual: false);
            await repository.SaveChangesAsync();

            var result = await service.GetCurrentPriceAsync(BuildRequest());

            result.Price.Should().Be(88m);
            result.IsManual.Should().BeFalse();
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    /// <summary>
    /// The fallback branch is the only one that swallows the fetch failure, so it is the only
    /// place the failure can still be reported. A whole portfolio grid of failed scrapes was
    /// previously invisible.
    /// </summary>
    [Fact]
    public async Task GetCurrentPriceAsync_LiveFetchFails_AutomaticEntryExistsForToday_LogsTheErrorType()
    {
        var (service, repository, logger, tempFile) = CreateRecordingServiceOverRepository(StubAssetPriceService.Failure());
        try
        {
            var today = DateOnly.FromDateTime(DateTime.Today);
            repository.GetAsset(BrokerName, PortfolioName, AssetName)!.SetPrice(today, 88m, isManual: false);
            await repository.SaveChangesAsync();

            await service.GetCurrentPriceAsync(BuildRequest());

            var entry = logger.Entries.Should().ContainSingle(recorded => recorded.Level == LogLevel.Warning).Subject;
            entry.Message.Should().Contain(nameof(InvalidOperationException));
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    /// <summary>
    /// A provider's message can quote the ticker and the price it was fetching, and neither may
    /// reach the log stream. Only the exception type does.
    /// </summary>
    [Fact]
    public async Task GetCurrentPriceAsync_LiveFetchFallsBack_LogsNeitherTheExceptionMessageNorThePrice()
    {
        var (service, repository, logger, tempFile) = CreateRecordingServiceOverRepository(StubAssetPriceService.Failure());
        try
        {
            var today = DateOnly.FromDateTime(DateTime.Today);
            repository.GetAsset(BrokerName, PortfolioName, AssetName)!.SetPrice(today, 88m, isManual: false);
            await repository.SaveChangesAsync();

            await service.GetCurrentPriceAsync(BuildRequest());

            var entry = logger.Entries.Should().ContainSingle(recorded => recorded.Level == LogLevel.Warning).Subject;
            entry.Message.Should().NotContain("No asset price fetcher is registered").And.NotContain("88");
            entry.Exception.Should().BeNull();
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    /// <summary>
    /// The rethrow branch hands the failure to the caller, so logging here as well would report
    /// every such failure twice.
    /// </summary>
    [Fact]
    public async Task GetCurrentPriceAsync_LiveFetchFails_NoEntryForToday_DoesNotLog()
    {
        var (service, _, logger, tempFile) = CreateRecordingServiceOverRepository(StubAssetPriceService.Failure());
        try
        {
            Func<Task> act = () => service.GetCurrentPriceAsync(BuildRequest());

            await act.Should().ThrowAsync<InvalidOperationException>();
            logger.Entries.Should().NotContain(recorded => recorded.Level == LogLevel.Warning);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public async Task GetCurrentPriceAsync_LiveFetchFails_NoEntryForToday_RethrowsOriginalException()
    {
        var (service, _, tempFile) = CreateServiceWithAssetPriceService(StubAssetPriceService.Failure());
        try
        {
            Func<Task> act = () => service.GetCurrentPriceAsync(BuildRequest());

            await act.Should().ThrowAsync<InvalidOperationException>();
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public async Task GetCurrentPriceAsync_MissingPortfolioName_SkipsHistoryEntirely()
    {
        var (service, repository, tempFile) = CreateServiceWithAssetPriceService(StubAssetPriceService.Success(15m));
        try
        {
            var request = BuildRequest();
            request.PortfolioName = null;

            var result = await service.GetCurrentPriceAsync(request);

            result.Price.Should().Be(15m);
            repository.SaveCount.Should().Be(0);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    private static AssetPriceRequestDTO BuildRequest() => new()
    {
        Exchange = "BVMF",
        Ticker = AssetName,
        BrokerName = BrokerName,
        PortfolioName = PortfolioName,
        AssetName = AssetName
    };

    [Fact]
    public async Task GetCurrentPriceAsync_WhenPersistenceFails_DoesNotReportSuccess()
    {
        var (service, tracer, _, tempFile) = CreateServiceWithFailingStorage(StubAssetPriceService.Success(123.45m));
        try
        {
            await service.GetCurrentPriceAsync(BuildRequest());

            var span = tracer.Spans.Single(recorded => recorded.Name.EndsWith("GetCurrentPrice", StringComparison.Ordinal));
            span.Attributes[TelemetryAttributeKeys.OperationResult].Should().Be(TelemetryOperationResults.Failed);
            span.RecordedException.Should().NotBeNull();
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public async Task GetCurrentPriceAsync_WhenPersistenceFails_LogsAnErrorNamingTheAsset()
    {
        var (service, _, logger, tempFile) = CreateServiceWithFailingStorage(StubAssetPriceService.Success(123.45m));
        try
        {
            await service.GetCurrentPriceAsync(BuildRequest());

            var entry = logger.Entries.Should().ContainSingle(recorded => recorded.Level == LogLevel.Error).Subject;
            entry.Message.Should().Contain(BrokerName).And.Contain(PortfolioName).And.Contain(AssetName);
            entry.Exception.Should().NotBeNull();
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    /// <summary>
    /// The caller still gets the live price, so a storage fault does not blank out the value on
    /// screen - but the operation reports failed rather than claiming a record was made.
    /// </summary>
    [Fact]
    public async Task GetCurrentPriceAsync_WhenPersistenceFails_StillReturnsTheFetchedPrice()
    {
        var (service, _, _, tempFile) = CreateServiceWithFailingStorage(StubAssetPriceService.Success(123.45m));
        try
        {
            var result = await service.GetCurrentPriceAsync(BuildRequest());

            result.Price.Should().Be(123.45m);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    /// <summary>
    /// The pre-existing record test reads back from the in-memory graph, so it passes even when
    /// the save throws. This one goes to the file, which is what the user actually inspects.
    /// </summary>
    [Fact]
    public async Task GetCurrentPriceAsync_LiveFetchSucceeds_PersistsTheEntryToTheDataFile()
    {
        var (service, _, tempFile) = CreateServiceWithAssetPriceService(StubAssetPriceService.Success(321.5m));
        try
        {
            await service.GetCurrentPriceAsync(BuildRequest());

            var entry = ReloadAssetFromDisk(tempFile)!.GetPriceForDate(DateOnly.FromDateTime(DateTime.Today));
            entry.Should().NotBeNull();
            entry!.Price.Should().Be(321.5m);
            entry.IsManual.Should().BeFalse();
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public async Task GetCurrentPriceAsync_WhenPersistenceFails_WritesNothingToTheDataFile()
    {
        var (service, _, _, tempFile) = CreateServiceWithFailingStorage(StubAssetPriceService.Success(123.45m));
        try
        {
            await service.GetCurrentPriceAsync(BuildRequest());

            ReloadAssetFromDisk(tempFile)!.GetPriceForDate(DateOnly.FromDateTime(DateTime.Today))
                .Should().BeNull("the save threw, so nothing reached the file");
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public async Task GetCurrentPriceAsync_WhenAssetContextResolvesNothing_LogsAWarningNamingTheTriple()
    {
        var (service, _, logger, tempFile) = CreateRecordingService(StubAssetPriceService.Success(10m));
        try
        {
            var request = BuildRequest();
            request.AssetName = "NOT-A-REAL-ASSET";

            await service.GetCurrentPriceAsync(request);

            var entry = logger.Entries.Should().ContainSingle(recorded => recorded.Level == LogLevel.Warning).Subject;
            entry.Message.Should().Contain(BrokerName).And.Contain(PortfolioName).And.Contain("NOT-A-REAL-ASSET");
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    /// <summary>
    /// A blank portfolio or asset name is a deliberate lookup-only request, not a misroute, so it
    /// must not produce a warning on every batch price check.
    /// </summary>
    [Fact]
    public async Task GetCurrentPriceAsync_WithoutAssetContext_DoesNotWarn()
    {
        var (service, _, logger, tempFile) = CreateRecordingService(StubAssetPriceService.Success(10m));
        try
        {
            var request = BuildRequest();
            request.PortfolioName = null;
            request.AssetName = null;

            await service.GetCurrentPriceAsync(request);

            logger.Entries.Should().NotContain(recorded => recorded.Level == LogLevel.Warning);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    private static Asset? ReloadAssetFromDisk(string tempFile)
    {
        var storage = new LocalJsonStorage(tempFile);
        var serializer = new InvestmentsSerializerAdapter();
        var repository = new InvestmentJsonRepository(InvestmentsLoader.LoadSync(storage, serializer), storage, serializer);
        return repository.GetAsset(BrokerName, PortfolioName, AssetName);
    }

    private static (PriceService Service, RecordingTelemetryTracer Tracer, RecordingLogger<PriceService> Logger, string TempFile)
        CreateRecordingService(IAssetPriceService assetPriceService, bool failWrites = false)
    {
        var tempFile = Path.Combine(Path.GetTempPath(), $"data.test.{Guid.NewGuid():N}.json");
        File.Copy(TestDataPaths.DataJsonFile, tempFile, true);

        IJsonStorage storage = new LocalJsonStorage(tempFile);
        var serializer = new InvestmentsSerializerAdapter();
        var investments = InvestmentsLoader.LoadSync(storage, serializer);
        if (failWrites)
        {
            storage = new WriteFailingJsonStorage(storage);
        }

        var tracer = new RecordingTelemetryTracer();
        var logger = new RecordingLogger<PriceService>();
        var repository = new InvestmentJsonRepository(investments, storage, serializer);
        var navigationService = new NavigationService(repository, tracer, NullLogger<NavigationService>.Instance);

        return (new PriceService(repository, navigationService, assetPriceService, tracer, logger), tracer, logger, tempFile);
    }

    private static (PriceService Service, RecordingTelemetryTracer Tracer, RecordingLogger<PriceService> Logger, string TempFile)
        CreateServiceWithFailingStorage(IAssetPriceService assetPriceService) =>
        CreateRecordingService(assetPriceService, failWrites: true);

    /// <summary>Stands in for the storage faults the real providers raise - a locked file locally,
    /// or a refused upload against Google Drive.</summary>
    private sealed class WriteFailingJsonStorage : IJsonStorage
    {
        private readonly IJsonStorage _inner;

        public WriteFailingJsonStorage(IJsonStorage inner)
        {
            _inner = inner;
        }

        public Task<string> ReadAsync() => _inner.ReadAsync();

        public Task WriteAsync(string json) =>
            throw new IOException("The process cannot access the file because it is being used by another process.");
    }

    private static (PriceService Service, string TempFile) CreateService()
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
        var serializer = new InvestmentsSerializerAdapter();
        return (new InvestmentJsonRepository(InvestmentsLoader.LoadSync(storage, serializer), storage, serializer),
            new RecordingTelemetryTracer(), tempFile);
    }

    /// <summary>Same private temp copy as the other helpers, but with a recording logger and the
    /// repository exposed, for tests that seed price history before exercising the service.</summary>
    private static (PriceService Service, InvestmentJsonRepository Repository, RecordingLogger<PriceService> Logger, string TempFile)
        CreateRecordingServiceOverRepository(IAssetPriceService assetPriceService)
    {
        var (repository, tracer, tempFile) = CreateRepositoryOverTempCopy();
        var logger = new RecordingLogger<PriceService>();
        var navigationService = new NavigationService(repository, tracer, NullLogger<NavigationService>.Instance);

        return (new PriceService(repository, navigationService, assetPriceService, tracer, logger), repository, logger, tempFile);
    }

    private static (PriceService Service, InvestmentJsonRepository Repository, string TempFile) CreateServiceWithRepository()
    {
        var (repository, tracer, tempFile) = CreateRepositoryOverTempCopy();
        var navigationService = new NavigationService(repository, tracer, NullLogger<NavigationService>.Instance);
        var service = new PriceService(repository, navigationService, StubAssetPriceService.NotUsed(), tracer, NullLogger<PriceService>.Instance);

        return (service, repository, tempFile);
    }

    private static (PriceService Service, CountingRepository Repository, string TempFile) CreateServiceWithAssetPriceService(IAssetPriceService assetPriceService)
    {
        var (innerRepository, tracer, tempFile) = CreateRepositoryOverTempCopy();
        var repository = new CountingRepository(innerRepository);
        var navigationService = new NavigationService(repository, tracer, NullLogger<NavigationService>.Instance);
        var service = new PriceService(repository, navigationService, assetPriceService, tracer, NullLogger<PriceService>.Instance);

        return (service, repository, tempFile);
    }

    private sealed class StubAssetPriceService : IAssetPriceService
    {
        private readonly Func<AssetPriceRequestDTO, AssetPriceDTO> _handler;

        private StubAssetPriceService(Func<AssetPriceRequestDTO, AssetPriceDTO> handler)
        {
            _handler = handler;
        }

        public static StubAssetPriceService Success(decimal price) =>
            new(request => new AssetPriceDTO { Exchange = request.Exchange, Ticker = request.Ticker, Price = price });

        public static StubAssetPriceService Failure() =>
            new(_ => throw new InvalidOperationException("No asset price fetcher is registered."));

        public static StubAssetPriceService NotUsed() =>
            new(_ => throw new NotImplementedException("Not expected to be called in this test."));

        public AssetPriceDTO GetCurrentPrice(AssetPriceRequestDTO request) => _handler(request);
    }

    private sealed class CountingRepository : IInvestmentRepository
    {
        private readonly IInvestmentRepository _inner;

        public CountingRepository(IInvestmentRepository inner)
        {
            _inner = inner;
        }

        public int SaveCount { get; private set; }

        public IEnumerable<Asset> GetAssetsByBroker(string name, InvestmentScope scope = InvestmentScope.Active) =>
            _inner.GetAssetsByBroker(name, scope);

        public IEnumerable<Asset> GetAssetsByBrokerPortfolio(string broker, string portfolio, InvestmentScope scope = InvestmentScope.Active) =>
            _inner.GetAssetsByBrokerPortfolio(broker, portfolio, scope);

        public IEnumerable<Broker> GetBrokerList(InvestmentScope scope = InvestmentScope.Active) =>
            _inner.GetBrokerList(scope);

        public Asset? GetAsset(string brokerName, string portfolioName, string assetName, InvestmentScope scope = InvestmentScope.Active) =>
            _inner.GetAsset(brokerName, portfolioName, assetName, scope);

        public Task SaveChangesAsync()
        {
            SaveCount++;
            return _inner.SaveChangesAsync();
        }
    }
}
