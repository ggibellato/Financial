using Financial.CashFlow.Domain.Entities;
using Financial.CashFlow.Domain.Entities.Transactions;
using Financial.CashFlow.Infrastructure.Persistence;
using Financial.CashFlow.Infrastructure.Repositories;
using Financial.Shared.Abstractions.Configuration;
using Financial.Shared.Abstractions.Observability;
using Financial.Shared.Abstractions.Sync;
using Financial.Shared.Infrastructure.Persistence;
using Financial.TestUtilities;
using FluentAssertions;

namespace Financial.CashFlow.Infrastructure.Tests.Repositories;

[Trait("Category", "Integration")]
public class CashFlowRepositoryFactoryTests
{
    private static readonly CashFlowRepositoryFactory Factory =
        new(new CashFlowSerializerAdapter(), new JsonStorageFactory(new StubRemoteFileClientFactory(new StubRemoteFileClient(EmptyFile)), NoOpTelemetryTracer.Instance));

    [Fact]
    public async Task Create_FromAFileTheRepositoryWrote_ThenSaved_WritesTheSameBytes()
    {
        var written = TestCashFlowSeed.AsWrittenBy(new CashFlowSerializerAdapter());
        var path = Path.Combine(Path.GetTempPath(), $"cashflow-factory-{Guid.NewGuid()}.json");
        File.WriteAllText(path, written);
        try
        {
            var repository = Factory.Create(new JsonRepositorySelection(JsonRepositoryProvider.LocalJson, path, null, null));

            await repository.ApplyAndSaveAsync(() => true);

            File.ReadAllText(path).Should().Be(written);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public async Task Create_WithGoogleDriveProvider_QueuedMutationIsUploadedOnFlush()
    {
        var credentialsPath = Path.GetTempFileName();
        try
        {
            var remoteFileClient = new RecordingRemoteFileClient(EmptyFile);
            var factory = new CashFlowRepositoryFactory(
                new CashFlowSerializerAdapter(), new JsonStorageFactory(new StubRemoteFileClientFactory(remoteFileClient), NoOpTelemetryTracer.Instance));
            var options = new JsonRepositorySelection(
                JsonRepositoryProvider.GoogleDriveJson,
                null,
                credentialsPath,
                "Pessoais/Gleison/Financeiros");

            var repository = factory.Create(options);
            repository.AddExpense(Expense.Create(
                new DateOnly(2026, 7, 1), "Debounced upload test expense", 10m, Category.Create("Casa"), TestAccounts.Bank("Chase", roundUpEnabled: true), null));

            await repository.ApplyAndSaveAsync(() => true);

            remoteFileClient.UploadCallCount.Should().Be(0, "the write is only queued until the debounce window or a flush");
            ((ISyncStatusProvider)repository).GetStatus().State.Should().Be(SyncState.Pending);
            await ((ISyncStatusProvider)repository).FlushAsync();

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

    private static string EmptyFile() => new CashFlowSerializerAdapter().Serialize(CashFlowData.Create());
}
