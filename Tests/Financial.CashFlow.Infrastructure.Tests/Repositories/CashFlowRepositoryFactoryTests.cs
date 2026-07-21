using Financial.CashFlow.Infrastructure.Persistence;
using Financial.CashFlow.Infrastructure.Repositories;
using Financial.Shared.Infrastructure.Persistence;
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
        var factoryWithoutRemoteFileClient = new CashFlowRepositoryFactory(new CashFlowSerializerAdapter());
        var options = new CashFlowRepositorySelectionOptions(
            CashFlowRepositoryProvider.GoogleDriveJson,
            null,
            Path.GetTempFileName(),
            "Pessoais/Gleison/Financeiros");

        Action act = () => factoryWithoutRemoteFileClient.Create(options);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*IRemoteFileClientFactory*");
    }

    private sealed class StubRemoteFileClientFactory : IRemoteFileClientFactory
    {
        public IRemoteFileClient Create(string credentialsPath) => new StubRemoteFileClient();
    }

    private sealed class StubRemoteFileClient : IRemoteFileClient
    {
        public string DownloadFileContent(string path) => throw new NotSupportedException();
        public void UploadFileContent(string path, string content) => throw new NotSupportedException();
    }
}
