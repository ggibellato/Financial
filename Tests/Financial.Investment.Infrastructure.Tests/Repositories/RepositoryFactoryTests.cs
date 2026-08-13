using System.Diagnostics;
using Financial.Investment.Domain.Entities;
using Financial.Investment.Infrastructure.Integrations.GoogleFinancialSupport;
using Financial.Investment.Infrastructure.Persistence;
using Financial.Investment.Infrastructure.Repositories;
using Financial.Shared.Infrastructure.Persistence;
using Financial.Shared.Infrastructure.Sync;
using FluentAssertions;
using System.IO;

namespace Financial.Investment.Infrastructure.Tests.Repositories;

public class RepositoryFactoryTests
{
    private static readonly RepositoryFactory Factory =
        new(new InvestmentsSerializerAdapter(), new GoogleFileClientFactory());

    [Fact]
    public void Constructor_WithNullSerializer_Throws()
    {
        Action act = () => new RepositoryFactory(null!);
        act.Should().Throw<ArgumentNullException>().WithParameterName("serializer");
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
        var options = new RepositorySelectionOptions(
            RepositoryProvider.GoogleDriveJson,
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
        var options = new RepositorySelectionOptions(
            RepositoryProvider.GoogleDriveJson,
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
        var options = new RepositorySelectionOptions(
            RepositoryProvider.LocalJson,
            TestDataPaths.DataJsonFile,
            null,
            null);

        var result = Factory.Create(options);

        result.Should().BeOfType<JSONRepository>();
    }

    [Fact]
    public void Create_WithGoogleDriveProvider_WithoutCredentials_ThrowsFileNotFoundException()
    {
        var options = new RepositorySelectionOptions(
            RepositoryProvider.GoogleDriveJson,
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
        var options = new RepositorySelectionOptions(
            (RepositoryProvider)999,
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
        var factoryWithoutRemoteFileClient = new RepositoryFactory(new InvestmentsSerializerAdapter());
        var options = new RepositorySelectionOptions(
            RepositoryProvider.GoogleDriveJson,
            null,
            TestDataPaths.DataJsonFile,
            "Pessoais/Gleison/Financeiros");

        Action act = () => factoryWithoutRemoteFileClient.Create(options);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*IRemoteFileClientFactory*");
    }

    [Fact]
    public async Task Create_WithGoogleDriveProvider_SaveChangesAsync_ReturnsWithoutWaitingOnUpload()
    {
        var factory = new RepositoryFactory(new InvestmentsSerializerAdapter(), new StubRemoteFileClientFactory());
        var options = new RepositorySelectionOptions(
            RepositoryProvider.GoogleDriveJson,
            null,
            TestDataPaths.DataJsonFile,
            "Pessoais/Gleison/Financeiros");

        var repository = factory.Create(options);

        var stopwatch = Stopwatch.StartNew();
        await repository.SaveChangesAsync();
        stopwatch.Stop();

        // The debounce window is a real 10 seconds in production; returning near-instantly proves
        // the write was only queued, not actually uploaded through StubRemoteFileClient (which
        // throws NotSupportedException on upload if ever actually invoked).
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(1000);
    }

    [Fact]
    public async Task Create_WithGoogleDriveProvider_ResultImplementsISyncStatusProvider_ReportingPendingAfterAWrite()
    {
        var factory = new RepositoryFactory(new InvestmentsSerializerAdapter(), new StubRemoteFileClientFactory());
        var options = new RepositorySelectionOptions(
            RepositoryProvider.GoogleDriveJson,
            null,
            TestDataPaths.DataJsonFile,
            "Pessoais/Gleison/Financeiros");

        var repository = factory.Create(options);

        await repository.SaveChangesAsync();

        ((ISyncStatusProvider)repository).GetStatus().State.Should().Be(SyncState.Pending);
    }

    [Fact]
    public void Create_WithLocalJsonProvider_ResultReportsIdleStatus()
    {
        var options = new RepositorySelectionOptions(
            RepositoryProvider.LocalJson,
            TestDataPaths.DataJsonFile,
            null,
            null);

        var result = Factory.Create(options);

        var status = ((ISyncStatusProvider)result).GetStatus();
        status.Should().Be(new SyncStatus(SyncState.Idle, null, null));
    }

    [Fact]
    public async Task Create_WithGoogleDriveProvider_TwoInstancesFromSeparateCreateCalls_NeverShareStatus()
    {
        var factory = new RepositoryFactory(new InvestmentsSerializerAdapter(), new StubRemoteFileClientFactory());
        var options = new RepositorySelectionOptions(
            RepositoryProvider.GoogleDriveJson,
            null,
            TestDataPaths.DataJsonFile,
            "Pessoais/Gleison/Financeiros");

        var repositoryA = factory.Create(options);
        var repositoryB = factory.Create(options);

        await repositoryA.SaveChangesAsync();

        // Writing to A must never mark B as dirty/Pending - each Create() call produces a fully
        // independent DebouncedJsonStorage with no shared dirty flag, generation counter, or status.
        ((ISyncStatusProvider)repositoryA).GetStatus().State.Should().Be(SyncState.Pending);
        ((ISyncStatusProvider)repositoryB).GetStatus().State.Should().Be(SyncState.Idle);
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
}
