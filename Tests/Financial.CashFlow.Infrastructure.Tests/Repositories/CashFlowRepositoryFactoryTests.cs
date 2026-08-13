using System.Diagnostics;
using Financial.CashFlow.Domain.Entities;
using Financial.CashFlow.Infrastructure.Persistence;
using Financial.CashFlow.Infrastructure.Repositories;
using Financial.Shared.Infrastructure.Persistence;
using Financial.Shared.Infrastructure.Sync;
using FluentAssertions;

namespace Financial.CashFlow.Infrastructure.Tests.Repositories;

public class CashFlowRepositoryFactoryTests
{
    private static readonly CashFlowRepositoryFactory Factory =
        new(new CashFlowSerializerAdapter(), new StubRemoteFileClientFactory());

    [Fact]
    public void Constructor_WithNullSerializer_Throws()
    {
        Action act = () => new CashFlowRepositoryFactory(null!);
        act.Should().Throw<ArgumentNullException>().WithParameterName("serializer");
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
            .WithMessage("*Google Drive credentials file path is required*");
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
            var factoryWithoutRemoteFileClient = new CashFlowRepositoryFactory(new CashFlowSerializerAdapter());
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
    public async Task Create_WithGoogleDriveProvider_SaveChangesAsync_ReturnsWithoutWaitingOnUpload()
    {
        var credentialsPath = Path.GetTempFileName();
        try
        {
            var factory = new CashFlowRepositoryFactory(new CashFlowSerializerAdapter(), new StubRemoteFileClientFactory());
            var options = new CashFlowRepositorySelectionOptions(
                CashFlowRepositoryProvider.GoogleDriveJson,
                null,
                credentialsPath,
                "Pessoais/Gleison/Financeiros");

            var repository = factory.Create(options);
            repository.AddExpense(Expense.Create(
                new DateOnly(2026, 7, 1), "Test expense", 10m, Category.Create("Casa"), Bank.Create("Chase", roundUpEnabled: true), null));

            var stopwatch = Stopwatch.StartNew();
            await repository.SaveChangesAsync();
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
            var factory = new CashFlowRepositoryFactory(new CashFlowSerializerAdapter(), new StubRemoteFileClientFactory());
            var options = new CashFlowRepositorySelectionOptions(
                CashFlowRepositoryProvider.GoogleDriveJson,
                null,
                credentialsPath,
                "Pessoais/Gleison/Financeiros");

            var repository = factory.Create(options);
            repository.AddExpense(Expense.Create(
                new DateOnly(2026, 7, 1), "Test expense", 10m, Category.Create("Casa"), Bank.Create("Chase", roundUpEnabled: true), null));

            await repository.SaveChangesAsync();

            ((ISyncStatusProvider)repository).GetStatus().State.Should().Be(SyncState.Pending);
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
            .WithMessage("*Google Drive credentials file not found*");
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
}
