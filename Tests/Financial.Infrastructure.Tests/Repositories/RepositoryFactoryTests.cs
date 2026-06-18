using Financial.Infrastructure.Persistence;
using Financial.Infrastructure.Repositories;
using FluentAssertions;
using System.IO;

namespace Financial.Infrastructure.Tests.Repositories;

public class RepositoryFactoryTests
{
    private static readonly RepositoryFactory Factory = new(new InvestmentsSerializerAdapter());

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

}
