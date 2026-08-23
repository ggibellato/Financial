using System.Diagnostics;
using Financial.Investment.Domain.Entities;
using Financial.Investment.Infrastructure.Integrations.GoogleFinancialSupport;
using Financial.Investment.Infrastructure.Persistence;
using Financial.Investment.Infrastructure.Repositories;
using Financial.Shared.Abstractions.Observability;
using Financial.Shared.Abstractions.Persistence;
using Financial.Shared.Abstractions.Sync;
using Financial.Shared.Infrastructure.Persistence;
using Financial.TestUtilities;
using FluentAssertions;
using System.IO;

namespace Financial.Investment.Infrastructure.Tests.Repositories;

public class InvestmentRepositoryFactoryTests
{
    /// <summary>The same factory over a stubbed remote-file client, for the tests that must not reach Google Drive.</summary>
    private readonly InvestmentRepositoryFactory _stubbedFactory;

    public InvestmentRepositoryFactoryTests()
    {
        _stubbedFactory = new InvestmentRepositoryFactory(
            new InvestmentsSerializerAdapter(), new JsonStorageFactory(new StubRemoteFileClientFactory(), NoOpTelemetryTracer.Instance));
    }

    private static readonly InvestmentRepositoryFactory Factory =
        new(new InvestmentsSerializerAdapter(), new JsonStorageFactory(new GoogleFileClientFactory(), NoOpTelemetryTracer.Instance));

    [Fact]
    public void Constructor_WithNullSerializer_Throws()
    {
        Action act = () => new InvestmentRepositoryFactory(null!, new JsonStorageFactory(null, NoOpTelemetryTracer.Instance));
        act.Should().Throw<ArgumentNullException>().WithParameterName("serializer");
    }

    [Fact]
    public void Constructor_WithNullStorageFactory_Throws()
    {
        Action act = () => new InvestmentRepositoryFactory(new InvestmentsSerializerAdapter(), null!);
        act.Should().Throw<ArgumentNullException>().WithParameterName("storageFactory");
    }

    [Fact]
    public void Create_WithNullOptions_Throws()
    {
        Action act = () => Factory.Create(null!);
        act.Should().Throw<ArgumentNullException>().WithParameterName("options");
    }

    [Fact]
    public void Create_WithGoogleDriveProvider_CredentialsPathDoesNotExist_ThrowsFileNotFoundExceptionWithResolvedPath()
    {
        var options = new InvestmentRepositorySelectionOptions(
            InvestmentRepositoryProvider.GoogleDriveJson,
            null,
            "nonexistent/credentials.json",
            "Pessoais/Gleison/Financeiros");

        Action act = () => Factory.Create(options);

        act.Should().Throw<FileNotFoundException>()
            .WithMessage("*credentials file not found at*");
    }

    [Fact]
    public void Create_WithGoogleDriveProvider_CredentialsPathExists_ResolvesPastFileCheck()
    {
        // The credentials file exists but isn't a valid Google service-account JSON, so this
        // exercises the successful path-resolution branch; the eventual credential-parsing
        // failure happens later and purely locally, no network call is made.
        var options = new InvestmentRepositorySelectionOptions(
            InvestmentRepositoryProvider.GoogleDriveJson,
            null,
            TestDataPaths.DataJsonFile,
            "Pessoais/Gleison/Financeiros");

        Action act = () => Factory.Create(options);

        act.Should().Throw<Exception>()
            .Which.Message.Should().NotContain("credentials file not found");
    }

    [Fact]
    public void Create_WithLocalJsonProvider_ReturnsJsonRepository()
    {
        var options = new InvestmentRepositorySelectionOptions(
            InvestmentRepositoryProvider.LocalJson,
            TestDataPaths.DataJsonFile,
            null,
            null);

        var result = Factory.Create(options);

        result.Should().BeOfType<InvestmentJsonRepository>();
    }

    [Fact]
    public void Create_WithGoogleDriveProvider_WithoutCredentials_ThrowsFileNotFoundException()
    {
        var options = new InvestmentRepositorySelectionOptions(
            InvestmentRepositoryProvider.GoogleDriveJson,
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
        var options = new InvestmentRepositorySelectionOptions(
            (InvestmentRepositoryProvider)999,
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
        var factoryWithoutRemoteFileClient = new InvestmentRepositoryFactory(
            new InvestmentsSerializerAdapter(), new JsonStorageFactory(null, NoOpTelemetryTracer.Instance));
        var options = new InvestmentRepositorySelectionOptions(
            InvestmentRepositoryProvider.GoogleDriveJson,
            null,
            TestDataPaths.DataJsonFile,
            "Pessoais/Gleison/Financeiros");

        Action act = () => factoryWithoutRemoteFileClient.Create(options);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*IRemoteFileClientFactory*");
    }

    [Fact]
    public async Task Create_WithGoogleDriveProvider_ApplyAndSaveAsync_ReturnsWithoutWaitingOnUpload()
    {
        var options = new InvestmentRepositorySelectionOptions(
            InvestmentRepositoryProvider.GoogleDriveJson,
            null,
            TestDataPaths.DataJsonFile,
            "Pessoais/Gleison/Financeiros");

        var repository = _stubbedFactory.Create(options);

        var stopwatch = Stopwatch.StartNew();
        await repository.ApplyAndSaveAsync(() => true);
        stopwatch.Stop();

        // The debounce window is a real 10 seconds in production; returning near-instantly proves
        // the write was only queued, not actually uploaded through StubRemoteFileClient (which
        // throws NotSupportedException on upload if ever actually invoked).
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(1000);
    }

    [Fact]
    public async Task Create_WithGoogleDriveProvider_ResultImplementsISyncStatusProvider_ReportingPendingAfterAWrite()
    {
        var options = new InvestmentRepositorySelectionOptions(
            InvestmentRepositoryProvider.GoogleDriveJson,
            null,
            TestDataPaths.DataJsonFile,
            "Pessoais/Gleison/Financeiros");

        var repository = _stubbedFactory.Create(options);

        await repository.ApplyAndSaveAsync(() => true);

        ((ISyncStatusProvider)repository).GetStatus().State.Should().Be(SyncState.Pending);
    }

    [Fact]
    public void Create_WithLocalJsonProvider_ResultReportsIdleStatus()
    {
        var options = new InvestmentRepositorySelectionOptions(
            InvestmentRepositoryProvider.LocalJson,
            TestDataPaths.DataJsonFile,
            null,
            null);

        var result = Factory.Create(options);

        var status = ((ISyncStatusProvider)result).GetStatus();
        status.Should().Be(new SyncStatus(SyncState.Idle, null, null));
    }

    [Fact]
    public async Task Create_WithGoogleDriveProvider_MutationEventuallyUploadsQueuedChangeViaDebouncedStorage()
    {
        var remoteFileClient = new RecordingRemoteFileClient();
        var factory = new InvestmentRepositoryFactory(
            new InvestmentsSerializerAdapter(), new JsonStorageFactory(new RecordingRemoteFileClientFactory(remoteFileClient), NoOpTelemetryTracer.Instance));
        var options = new InvestmentRepositorySelectionOptions(
            InvestmentRepositoryProvider.GoogleDriveJson,
            null,
            TestDataPaths.DataJsonFile,
            "Pessoais/Gleison/Financeiros");

        var repository = factory.Create(options);

        await repository.ApplyAndSaveAsync(() => true);

        // The real production debounce window is 10 seconds (InvestmentRepositoryFactory.DebounceWindow, not
        // configurable by design). Waiting past it with margin proves the queued write actually
        // reaches the wrapped storage's upload call — not just that ApplyAndSaveAsync() returns quickly.
        // Independence from any CashFlow activity is already established structurally: this instance's
        // DebouncedJsonStorage is entirely separate from CashFlow's (see
        // Create_WithGoogleDriveProvider_TwoInstancesFromSeparateCreateCalls_NeverShareStatus below, and
        // CashFlowInvestmentRepositoryFactoryTests' own equivalent eventual-upload test), so nothing CashFlow does
        // could affect this repository's timer, retry state, or upload call.
        await WaitForAsync(() => remoteFileClient.UploadCallCount > 0, TimeSpan.FromSeconds(15));

        remoteFileClient.UploadCallCount.Should().Be(1);
        ((ISyncStatusProvider)repository).GetStatus().State.Should().Be(SyncState.Idle);
        ((ISyncStatusProvider)repository).GetStatus().LastSuccessfulSaveUtc.Should().NotBeNull();
    }

    [Fact]
    public async Task Create_WithGoogleDriveProvider_TwoInstancesFromSeparateCreateCalls_NeverShareStatus()
    {
        var options = new InvestmentRepositorySelectionOptions(
            InvestmentRepositoryProvider.GoogleDriveJson,
            null,
            TestDataPaths.DataJsonFile,
            "Pessoais/Gleison/Financeiros");

        var repositoryA = _stubbedFactory.Create(options);
        var repositoryB = _stubbedFactory.Create(options);

        await repositoryA.ApplyAndSaveAsync(() => true);

        // Writing to A must never mark B as dirty/Pending - each Create() call produces a fully
        // independent DebouncedJsonStorage with no shared dirty flag, generation counter, or status.
        ((ISyncStatusProvider)repositoryA).GetStatus().State.Should().Be(SyncState.Pending);
        ((ISyncStatusProvider)repositoryB).GetStatus().State.Should().Be(SyncState.Idle);
    }

    [Fact]
    public async Task Create_WithGoogleDriveProviderAndTracer_RecordsGoogleDriveUploadSpanOnEventualUpload()
    {
        var remoteFileClient = new RecordingRemoteFileClient();
        var tracer = new RecordingTelemetryTracer();
        var factory = new InvestmentRepositoryFactory(
            new InvestmentsSerializerAdapter(), new JsonStorageFactory(new RecordingRemoteFileClientFactory(remoteFileClient), tracer));
        var options = new InvestmentRepositorySelectionOptions(
            InvestmentRepositoryProvider.GoogleDriveJson,
            null,
            TestDataPaths.DataJsonFile,
            "Pessoais/Gleison/Financeiros");

        var repository = factory.Create(options);

        await repository.ApplyAndSaveAsync(() => true);

        await WaitForAsync(() => tracer.Spans.Any(s => s.Name == "RemoteStorage.Upload"), TimeSpan.FromSeconds(15));

        tracer.Spans.Should().Contain(s => s.Name == "JsonStorage.Save");
        tracer.Spans.Should().Contain(s => s.Name == "RemoteStorage.Upload");
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
            new InvestmentsSerializerAdapter().Serialize(Investments.Create());

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

        public string DownloadFileContent(string path) =>
            new InvestmentsSerializerAdapter().Serialize(Investments.Create());

        public void UploadFileContent(string path, string content) => UploadCallCount++;
    }
}
