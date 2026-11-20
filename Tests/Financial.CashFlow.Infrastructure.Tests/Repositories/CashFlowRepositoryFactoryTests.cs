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
    public void Create_WithNullOptions_Throws()
    {
        Action act = () => Factory.Create(null!);
        act.Should().Throw<ArgumentNullException>().WithParameterName("options");
    }

    [Fact]
    public void Create_WithLocalJsonProvider_ReturnsCashFlowJsonRepository()
    {
        var missingPath = Path.Combine(Path.GetTempPath(), $"cashflow-factory-{Guid.NewGuid()}.json");
        var options = new JsonRepositorySelection(
            JsonRepositoryProvider.LocalJson,
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
        var options = new JsonRepositorySelection(
            JsonRepositoryProvider.LocalJson,
            missingPath,
            null,
            null);

        var result = Factory.Create(options);

        var status = ((ISyncStatusProvider)result).GetStatus();
        status.Should().Be(new SyncStatus(SyncState.Idle, null, null));
    }

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
        var credentialsPath = Path.GetTempFileName();
        try
        {
            var factoryWithoutRemoteFileClient = new CashFlowRepositoryFactory(
                new CashFlowSerializerAdapter(), new JsonStorageFactory(null, NoOpTelemetryTracer.Instance));
            var options = new JsonRepositorySelection(
                JsonRepositoryProvider.GoogleDriveJson,
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
            var options = new JsonRepositorySelection(
                JsonRepositoryProvider.GoogleDriveJson,
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
            var options = new JsonRepositorySelection(
                JsonRepositoryProvider.GoogleDriveJson,
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
    public async Task Create_WithGoogleDriveProvider_ResultImplementsISyncStatusProvider_ReportingPendingAfterAWrite()
    {
        var credentialsPath = Path.GetTempFileName();
        try
        {
            var factory = new CashFlowRepositoryFactory(
                new CashFlowSerializerAdapter(), new JsonStorageFactory(new StubRemoteFileClientFactory(new StubRemoteFileClient(EmptyFile)), NoOpTelemetryTracer.Instance));
            var options = new JsonRepositorySelection(
                JsonRepositoryProvider.GoogleDriveJson,
                null,
                credentialsPath,
                "Pessoais/Gleison/Financeiros");

            var repository = factory.Create(options);
            repository.AddExpense(Expense.Create(
                new DateOnly(2026, 7, 1), "Test expense", 10m, Category.Create("Casa"), TestAccounts.Bank("Chase", roundUpEnabled: true), null));

            await repository.ApplyAndSaveAsync(() => true);

            ((ISyncStatusProvider)repository).GetStatus().State.Should().Be(SyncState.Pending);
        }
        finally
        {
            File.Delete(credentialsPath);
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

    [Fact]
    public async Task Create_WithGoogleDriveProviderAndTracer_RecordsTheSaveAndUploadSpansOnFlush()
    {
        var credentialsPath = Path.GetTempFileName();
        try
        {
            var remoteFileClient = new RecordingRemoteFileClient(EmptyFile);
            var tracer = new RecordingTelemetryTracer();
            var factory = new CashFlowRepositoryFactory(
                new CashFlowSerializerAdapter(), new JsonStorageFactory(new StubRemoteFileClientFactory(remoteFileClient), tracer));
            var options = new JsonRepositorySelection(
                JsonRepositoryProvider.GoogleDriveJson,
                null,
                credentialsPath,
                "Pessoais/Gleison/Financeiros");

            var repository = factory.Create(options);
            repository.AddExpense(Expense.Create(
                new DateOnly(2026, 7, 1), "Tracer test expense", 10m, Category.Create("Casa"), TestAccounts.Bank("Chase", roundUpEnabled: true), null));

            await repository.ApplyAndSaveAsync(() => true);

            await ((ISyncStatusProvider)repository).FlushAsync();

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
        var options = new JsonRepositorySelection(
            JsonRepositoryProvider.GoogleDriveJson,
            null,
            missingCredentialsPath,
            "Pessoais/Gleison/Financeiros");

        Action act = () => Factory.Create(options);

        act.Should().Throw<FileNotFoundException>()
            .WithMessage("*Remote storage credentials file not found*");
    }

    private static string EmptyFile() => new CashFlowSerializerAdapter().Serialize(CashFlowData.Create());
}
