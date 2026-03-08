using FinancialModel.Application;
using FinancialModel.Infrastructure;
using FluentAssertions;
using System;

namespace Financial.Infrastructure.Tests;

public class RepositoryFactoryTests
{
    [Fact]
    public void Create_WithLocalJsonProvider_ReturnsLocalRepository()
    {
        var options = new RepositorySelectionOptions(
            RepositoryProvider.LocalJson,
            TestDataPaths.DataJsonPath,
            null,
            null);

        var factory = new RepositoryFactory();

        var result = factory.Create(options);

        result.Should().BeOfType<LocalJSONRepository>();
    }

    [Fact]
    public void Create_WithGoogleDriveProvider_WithoutCredentials_ThrowsFileNotFoundException()
    {
        var options = new RepositorySelectionOptions(
            RepositoryProvider.GoogleDriveJson,
            null,
            null,
            "Pessoais/Gleison/Financeiros/data.json");

        var factory = new RepositoryFactory();

        Action act = () => factory.Create(options);

        act.Should().Throw<FileNotFoundException>()
            .WithMessage("*Google Drive credentials file path is required*");
    }
}
