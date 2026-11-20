using Financial.Investment.Domain.Entities;
using Financial.Integrations.GoogleDrive;
using Financial.Investment.Infrastructure.Persistence;
using Financial.Investment.Infrastructure.Repositories;
using Financial.Shared.Abstractions.Configuration;
using Financial.Shared.Abstractions.Observability;
using Financial.Shared.Abstractions.Persistence;
using Financial.Shared.Abstractions.Sync;
using Financial.Shared.Infrastructure.Persistence;
using Financial.TestUtilities;
using FluentAssertions;
using System.IO;

namespace Financial.Investment.Infrastructure.Tests.Repositories;

[Trait("Category", "Integration")]
public class InvestmentRepositoryFactoryTests
{
    /// <summary>The same factory over a stubbed remote-file client, for the tests that must not reach Google Drive.</summary>
    private readonly InvestmentRepositoryFactory _stubbedFactory;

    public InvestmentRepositoryFactoryTests()
    {
        _stubbedFactory = new InvestmentRepositoryFactory(
            new InvestmentSerializerAdapter(), new JsonStorageFactory(new StubRemoteFileClientFactory(new StubRemoteFileClient(EmptyFile)), NoOpTelemetryTracer.Instance), TestClock.At());
    }

    private static readonly InvestmentRepositoryFactory Factory =
        new(new InvestmentSerializerAdapter(), new JsonStorageFactory(new GoogleFileClientFactory(), NoOpTelemetryTracer.Instance), TestClock.At());

    [Fact]
    public void Create_WithNullOptions_Throws()
    {
        Action act = () => Factory.Create(null!);
        act.Should().Throw<ArgumentNullException>().WithParameterName("options");
    }

    [Fact]
    public void Create_WithGoogleDriveProvider_CredentialsPathDoesNotExist_ThrowsFileNotFoundExceptionWithResolvedPath()
    {
        var options = new JsonRepositorySelection(
            JsonRepositoryProvider.GoogleDriveJson,
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
        var options = new JsonRepositorySelection(
            JsonRepositoryProvider.GoogleDriveJson,
            null,
            TestDataPaths.DataJsonFile,
            "Pessoais/Gleison/Financeiros");

        Action act = () => Factory.Create(options);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*credential*");
    }

    [Fact]
    public void Create_WithLocalJsonProvider_ReturnsJsonRepository()
    {
        var options = new JsonRepositorySelection(
            JsonRepositoryProvider.LocalJson,
            TestDataPaths.DataJsonFile,
            null,
            null);

        var result = Factory.Create(options);

        result.Should().BeOfType<InvestmentJsonRepository>();
    }

    [Fact]
    public void Create_WithGoogleDriveProvider_WithoutCredentials_ThrowsFileNotFoundException()
    {
        var options = new JsonRepositorySelection(
            JsonRepositoryProvider.GoogleDriveJson,
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
        var options = new JsonRepositorySelection(
            (JsonRepositoryProvider)999,
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
            new InvestmentSerializerAdapter(), new JsonStorageFactory(null, NoOpTelemetryTracer.Instance), TestClock.At());
        var options = new JsonRepositorySelection(
            JsonRepositoryProvider.GoogleDriveJson,
            null,
            TestDataPaths.DataJsonFile,
            "Pessoais/Gleison/Financeiros");

        Action act = () => factoryWithoutRemoteFileClient.Create(options);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*IRemoteFileClientFactory*");
    }

    [Fact]
    public async Task Create_WithGoogleDriveProvider_ResultImplementsISyncStatusProvider_ReportingPendingAfterAWrite()
    {
        var options = new JsonRepositorySelection(
            JsonRepositoryProvider.GoogleDriveJson,
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
        var options = new JsonRepositorySelection(
            JsonRepositoryProvider.LocalJson,
            TestDataPaths.DataJsonFile,
            null,
            null);

        var result = Factory.Create(options);

        var status = ((ISyncStatusProvider)result).GetStatus();
        status.Should().Be(new SyncStatus(SyncState.Idle, null, null));
    }

    [Fact]
    public async Task Create_WithGoogleDriveProvider_QueuedMutationIsUploadedOnFlush()
    {
        var remoteFileClient = new RecordingRemoteFileClient(EmptyFile);
        var factory = new InvestmentRepositoryFactory(
            new InvestmentSerializerAdapter(), new JsonStorageFactory(new StubRemoteFileClientFactory(remoteFileClient), NoOpTelemetryTracer.Instance), TestClock.At());
        var options = new JsonRepositorySelection(
            JsonRepositoryProvider.GoogleDriveJson,
            null,
            TestDataPaths.DataJsonFile,
            "Pessoais/Gleison/Financeiros");

        var repository = factory.Create(options);

        await repository.ApplyAndSaveAsync(() => true);

        remoteFileClient.UploadCallCount.Should().Be(0, "the write is only queued until the debounce window or a flush");
        await ((ISyncStatusProvider)repository).FlushAsync();

        remoteFileClient.UploadCallCount.Should().Be(1);
        ((ISyncStatusProvider)repository).GetStatus().State.Should().Be(SyncState.Idle);
        ((ISyncStatusProvider)repository).GetStatus().LastSuccessfulSaveUtc.Should().NotBeNull();
    }

    [Fact]
    public async Task Create_WithGoogleDriveProvider_TwoInstancesFromSeparateCreateCalls_NeverShareStatus()
    {
        var options = new JsonRepositorySelection(
            JsonRepositoryProvider.GoogleDriveJson,
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
    public async Task Create_WithGoogleDriveProviderAndTracer_RecordsTheSaveAndUploadSpansOnFlush()
    {
        var remoteFileClient = new RecordingRemoteFileClient(EmptyFile);
        var tracer = new RecordingTelemetryTracer();
        var factory = new InvestmentRepositoryFactory(
            new InvestmentSerializerAdapter(), new JsonStorageFactory(new StubRemoteFileClientFactory(remoteFileClient), tracer), TestClock.At());
        var options = new JsonRepositorySelection(
            JsonRepositoryProvider.GoogleDriveJson,
            null,
            TestDataPaths.DataJsonFile,
            "Pessoais/Gleison/Financeiros");

        var repository = factory.Create(options);

        await repository.ApplyAndSaveAsync(() => true);
        await ((ISyncStatusProvider)repository).FlushAsync();

        tracer.Spans.Should().Contain(s => s.Name == "JsonStorage.Save");
        tracer.Spans.Should().Contain(s => s.Name == "RemoteStorage.Upload");
    }

    private static string EmptyFile() => new InvestmentSerializerAdapter().Serialize(Investments.Create());
}
