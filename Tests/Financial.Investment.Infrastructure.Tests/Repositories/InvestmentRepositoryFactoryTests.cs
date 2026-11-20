using Financial.Investment.Domain.Entities;
using Financial.Investment.Infrastructure.Persistence;
using Financial.Investment.Infrastructure.Repositories;
using Financial.Shared.Abstractions.Configuration;
using Financial.Shared.Abstractions.Observability;
using Financial.Shared.Abstractions.Sync;
using Financial.Shared.Infrastructure.Persistence;
using Financial.TestUtilities;
using FluentAssertions;

namespace Financial.Investment.Infrastructure.Tests.Repositories;

[Trait("Category", "Integration")]
public class InvestmentRepositoryFactoryTests
{
    private static readonly InvestmentRepositoryFactory Factory =
        new(new InvestmentSerializerAdapter(), new JsonStorageFactory(new StubRemoteFileClientFactory(new StubRemoteFileClient(EmptyFile)), NoOpTelemetryTracer.Instance), TestClock.At());

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

        var repositoryA = Factory.Create(options);
        var repositoryB = Factory.Create(options);

        await repositoryA.ApplyAndSaveAsync(() => true);

        // Writing to A must never mark B as dirty/Pending - each Create() call produces a fully
        // independent DebouncedJsonStorage with no shared dirty flag, generation counter, or status.
        ((ISyncStatusProvider)repositoryA).GetStatus().State.Should().Be(SyncState.Pending);
        ((ISyncStatusProvider)repositoryB).GetStatus().State.Should().Be(SyncState.Idle);
    }

    private static string EmptyFile() => new InvestmentSerializerAdapter().Serialize(Investments.Create());
}
