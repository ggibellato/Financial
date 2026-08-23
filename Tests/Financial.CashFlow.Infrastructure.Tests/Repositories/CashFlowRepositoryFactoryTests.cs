using System.Diagnostics;
using Financial.CashFlow.Domain.Entities;
using Financial.CashFlow.Infrastructure.Persistence;
using Financial.CashFlow.Infrastructure.Repositories;
using Financial.Shared.Abstractions.Observability;
using Financial.Shared.Abstractions.Persistence;
using Financial.Shared.Abstractions.Sync;
using Financial.Shared.Infrastructure.Persistence;
using Financial.TestUtilities;
using FluentAssertions;

namespace Financial.CashFlow.Infrastructure.Tests.Repositories;

public class CashFlowRepositoryFactoryTests
{
    private static readonly CashFlowRepositoryFactory Factory =
        new(new CashFlowSerializerAdapter(), new JsonStorageFactory(new StubRemoteFileClientFactory(), NoOpTelemetryTracer.Instance));

    [Fact]
    public void Constructor_WithNullSerializer_Throws()
    {
        Action act = () => new CashFlowRepositoryFactory(null!, new JsonStorageFactory(null, NoOpTelemetryTracer.Instance));
        act.Should().Throw<ArgumentNullException>().WithParameterName("serializer");
    }

    [Fact]
    public void Constructor_WithNullStorageFactory_Throws()
    {
        Action act = () => new CashFlowRepositoryFactory(new CashFlowSerializerAdapter(), null!);
        act.Should().Throw<ArgumentNullException>().WithParameterName("storageFactory");
    }

    [Fact]
    public void Create_WithNullOptions_Throws()
    {
        Action act = () => Factory.Create(null!);
        act.Should().Throw<ArgumentNullException>().WithParameterName("options");
    }

    [Fact]
    public void Create_WithLocalJsonProvider_ReturnsCashFlowJsonRepository()
    {
        var missingPath = Path.Combine(Path.GetTempPath(), $"cashflow-factory-{Guid.NewGuid()}.json");
        var options = new CashFlowRepositorySelectionOptions(
            CashFlowRepositoryProvider.LocalJson,
            missingPath,
            null,
            null);

        var result = Factory.Create(options);

        result.Should().BeOfType<CashFlowJsonRepository>();
    }

    [Fact]
    public void Create_WithLocalJsonProvider_ResultReportsIdleStatus()
    {
        var missingPath = Path.Combine(Path.GetTempPath(), $"cashflow-factory-{Guid.NewGuid()}.json");
        var options = new CashFlowRepositorySelectionOptions(
            CashFlowRepositoryProvider.LocalJson,
            missingPath,
            null,
            null);

        var result = Factory.Create(options);

        var status = ((ISyncStatusProvider)result).GetStatus();
        status.Should().Be(new SyncStatus(SyncState.Idle, null, null));
    }

    [Fact]
    public void Create_WithGoogleDriveProvider_WithoutCredentials_ThrowsFileNotFoundException()
    {
        var options = new CashFlowRepositorySelectionOptions(
            CashFlowRepositoryProvider.GoogleDriveJson,
            null,
            null,
            "Pessoais/Gleison/Financeiros");

        Action act = () => Factory.Create(options);

        act.Should().Throw<FileNotFoundException>()
            .WithMessage("*Remote storage credentials file path is required*");
    }

    [Fact]
    public void Create_WithUnsupportedProvider_ThrowsArgumentOutOfRangeException()
    {
        var options = new CashFlowRepositorySelectionOptions(
            (CashFlowRepositoryProvider)999,
            null,
            null,
            null);

        Action act = () => Factory.Create(options);

        act.Should().Throw<ArgumentOutOfRangeException>()
            .WithParameterName("Provider");
    }

    [Fact]
    public void Create_WithGoogleDriveProvider_NoRemoteFileClientFactoryRegistered_ThrowsInvalidOperationException()
    {
        var credentialsPath = Path.GetTempFileName();
        try
        {
            var factoryWithoutRemoteFileClient = new CashFlowRepositoryFactory(
                new CashFlowSerializerAdapter(), new JsonStorageFactory(null, NoOpTelemetryTracer.Instance));
            var options = new CashFlowRepositorySelectionOptions(
                CashFlowRepositoryProvider.GoogleDriveJson,
                null,
                credentialsPath,
                "Pessoais/Gleison/Financeiros");

            Action act = () => factoryWithoutRemoteFileClient.Create(options);

            act.Should().Throw<InvalidOperationException>()
                .WithMessage("*IRemoteFileClientFactory*");
        }
        finally
        {
            File.Delete(credentialsPath);
        }
    }

    [Fact]
    public void Create_WithGoogleDriveProvider_ValidAbsoluteCredentialsPath_ReturnsCashFlowJsonRepository()
    {
        var credentialsPath = Path.GetTempFileName();
        try
        {
            var options = new CashFlowRepositorySelectionOptions(
                CashFlowRepositoryProvider.GoogleDriveJson,
                null,
                credentialsPath,
                "Pessoais/Gleison/Financeiros");

            var result = Factory.Create(options);

            result.Should().BeOfType<CashFlowJsonRepository>();
        }
        finally
        {
            File.Delete(credentialsPath);
        }
    }

    [Fact]
    public void Create_WithGoogleDriveProvider_RelativeCredentialsPath_ResolvesRelativeToBaseDirectory()
    {
        var fileName = $"cashflow-credentials-{Guid.NewGuid()}.json";
        var absolutePath = Path.Combine(AppContext.BaseDirectory, fileName);
        File.WriteAllText(absolutePath, "{}");
        try
        {
            var options = new CashFlowRepositorySelectionOptions(
                CashFlowRepositoryProvider.GoogleDriveJson,
                null,
                fileName,
                "Pessoais/Gleison/Financeiros");

            var result = Factory.Create(options);

            result.Should().BeOfType<CashFlowJsonRepository>();
        }
        finally
        {
            File.Delete(absolutePath);
        }
    }

    [Fact]
    public async Task Create_WithGoogleDriveProvider_ApplyAndSaveAsync_ReturnsWithoutWaitingOnUpload()
    {
        var credentialsPath = Path.GetTempFileName();
        try
        {
            var factory = new CashFlowRepositoryFactory(
                new CashFlowSerializerAdapter(), new JsonStorageFactory(new StubRemoteFileClientFactory(), NoOpTelemetryTracer.Instance));
            var options = new CashFlowRepositorySelectionOptions(
                CashFlowRepositoryProvider.GoogleDriveJson,
                null,
                credentialsPath,
                "Pessoais/Gleison/Financeiros");

            var repository = factory.Create(options);
            repository.AddExpense(Expense.Create(
                new DateOnly(2026, 7, 1), "Test expense", 10m, Category.Create("Casa"), Bank.Create("Chase", roundUpEnabled: true), null));

            var stopwatch = Stopwatch.StartNew();
            await repository.ApplyAndSaveAsync(() => true);
            stopwatch.Stop();

            // The debounce window is a real 10 seconds in production; returning near-instantly proves
            // the write was only queued, not actually uploaded through StubRemoteFileClient (which
            // throws NotSupportedException on upload if ever actually invoked).
            stopwatch.ElapsedMilliseconds.Should().BeLessThan(1000);
        }
        finally
        {
            File.Delete(credentialsPath);
        }
    }

    [Fact]
    public async Task Create_WithGoogleDriveProvider_ResultImplementsISyncStatusProvider_ReportingPendingAfterAWrite()
    {
        var credentialsPath = Path.GetTempFileName();
        try
        {
            var factory = new CashFlowRepositoryFactory(
                new CashFlowSerializerAdapter(), new JsonStorageFactory(new StubRemoteFileClientFactory(), NoOpTelemetryTracer.Instance));
            var options = new CashFlowRepositorySelectionOptions(
                CashFlowRepositoryProvider.GoogleDriveJson,
                null,
                credentialsPath,
                "Pessoais/Gleison/Financeiros");

            var repository = factory.Create(options);
            repository.AddExpense(Expense.Create(
                new DateOnly(2026, 7, 1), "Test expense", 10m, Category.Create("Casa"), Bank.Create("Chase", roundUpEnabled: true), null));

            await repository.ApplyAndSaveAsync(() => true);

            ((ISyncStatusProvider)repository).GetStatus().State.Should().Be(SyncState.Pending);
        }
        finally
        {
            File.Delete(credentialsPath);
        }
    }

    [Fact]
    public async Task Create_WithGoogleDriveProvider_MutationEventuallyUploadsQueuedChangeViaDebouncedStorage()
    {
        var credentialsPath = Path.GetTempFileName();
        try
        {
            var remoteFileClient = new RecordingRemoteFileClient();
            var factory = new CashFlowRepositoryFactory(
                new CashFlowSerializerAdapter(), new JsonStorageFactory(new RecordingRemoteFileClientFactory(remoteFileClient), NoOpTelemetryTracer.Instance));
            var options = new CashFlowRepositorySelectionOptions(
                CashFlowRepositoryProvider.GoogleDriveJson,
                null,
                credentialsPath,
                "Pessoais/Gleison/Financeiros");

            var repository = factory.Create(options);
            repository.AddExpense(Expense.Create(
                new DateOnly(2026, 7, 1), "Debounced upload test expense", 10m, Category.Create("Casa"), Bank.Create("Chase", roundUpEnabled: true), null));

            await repository.ApplyAndSaveAsync(() => true);

            // The real production debounce window is 10 seconds (CashFlowRepositoryFactory.DebounceWindow,
            // not configurable by design). Waiting past it with margin proves the queued write actually
            // reaches the wrapped storage's upload call — not just that ApplyAndSaveAsync() returns quickly.
            // Wait for Idle rather than just UploadCallCount > 0: the status only flips to Idle once
            // HandleSaveSuccess runs after the upload, so polling on the call count alone races with that.
            await WaitForAsync(
                () => ((ISyncStatusProvider)repository).GetStatus().State == SyncState.Idle,
                TimeSpan.FromSeconds(15));

            remoteFileClient.UploadCallCount.Should().Be(1);
            remoteFileClient.LastUploadedContent.Should().Contain("Debounced upload test expense");
            ((ISyncStatusProvider)repository).GetStatus().State.Should().Be(SyncState.Idle);
            ((ISyncStatusProvider)repository).GetStatus().LastSuccessfulSaveUtc.Should().NotBeNull();
        }
        finally
        {
            File.Delete(credentialsPath);
        }
    }

    [Fact]
    public async Task Create_WithGoogleDriveProviderAndTracer_RecordsGoogleDriveUploadSpanOnEventualUpload()
    {
        var credentialsPath = Path.GetTempFileName();
        try
        {
            var remoteFileClient = new RecordingRemoteFileClient();
            var tracer = new RecordingTelemetryTracer();
            var factory = new CashFlowRepositoryFactory(
                new CashFlowSerializerAdapter(), new JsonStorageFactory(new RecordingRemoteFileClientFactory(remoteFileClient), tracer));
            var options = new CashFlowRepositorySelectionOptions(
                CashFlowRepositoryProvider.GoogleDriveJson,
                null,
                credentialsPath,
                "Pessoais/Gleison/Financeiros");

            var repository = factory.Create(options);
            repository.AddExpense(Expense.Create(
                new DateOnly(2026, 7, 1), "Tracer test expense", 10m, Category.Create("Casa"), Bank.Create("Chase", roundUpEnabled: true), null));

            await repository.ApplyAndSaveAsync(() => true);

            await WaitForAsync(() => tracer.Spans.Any(s => s.Name == "RemoteStorage.Upload"), TimeSpan.FromSeconds(15));

            tracer.Spans.Should().Contain(s => s.Name == "JsonStorage.Save");
            tracer.Spans.Should().Contain(s => s.Name == "RemoteStorage.Upload");
        }
        finally
        {
            File.Delete(credentialsPath);
        }
    }

    [Fact]
    public void Create_WithGoogleDriveProvider_CredentialsFileDoesNotExist_ThrowsFileNotFoundException()
    {
        var missingCredentialsPath = Path.Combine(Path.GetTempPath(), $"cashflow-credentials-{Guid.NewGuid()}.json");
        var options = new CashFlowRepositorySelectionOptions(
            CashFlowRepositoryProvider.GoogleDriveJson,
            null,
            missingCredentialsPath,
            "Pessoais/Gleison/Financeiros");

        Action act = () => Factory.Create(options);

        act.Should().Throw<FileNotFoundException>()
            .WithMessage("*Remote storage credentials file not found*");
    }

    private static async Task WaitForAsync(Func<bool> condition, TimeSpan timeout)
    {
        var deadline = DateTime.UtcNow + timeout;

        while (!condition())
        {
            if (DateTime.UtcNow >= deadline)
            {
                throw new TimeoutException($"Condition not met within {timeout}.");
            }

            await Task.Delay(50);
        }
    }

    private sealed class StubRemoteFileClientFactory : IRemoteFileClientFactory
    {
        public IRemoteFileClient Create(string credentialsPath) => new StubRemoteFileClient();
    }

    private sealed class StubRemoteFileClient : IRemoteFileClient
    {
        public string DownloadFileContent(string path) =>
            new CashFlowSerializerAdapter().Serialize(CashFlowData.Create());

        public void UploadFileContent(string path, string content) => throw new NotSupportedException();
    }

    private sealed class RecordingRemoteFileClientFactory : IRemoteFileClientFactory
    {
        private readonly IRemoteFileClient _client;

        public RecordingRemoteFileClientFactory(IRemoteFileClient client) => _client = client;

        public IRemoteFileClient Create(string credentialsPath) => _client;
    }

    private sealed class RecordingRemoteFileClient : IRemoteFileClient
    {
        public int UploadCallCount { get; private set; }

        public string? LastUploadedContent { get; private set; }

        public string DownloadFileContent(string path) =>
            new CashFlowSerializerAdapter().Serialize(CashFlowData.Create());

        public void UploadFileContent(string path, string content)
        {
            UploadCallCount++;
            LastUploadedContent = content;
        }
    }
}
