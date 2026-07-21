using Financial.Infrastructure.Integrations.GoogleFinancialSupport;
using Financial.Investment.Infrastructure.Persistence;
using Financial.Investment.Infrastructure.Repositories;
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

}
