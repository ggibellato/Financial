using Financial.Application.Interfaces;
using Financial.Infrastructure.Repositories;
using FluentAssertions;
using System;
using System.IO;

namespace Financial.Infrastructure.Tests;

public class RepositoryFactoryTests
{
    [Fact]
    public void Create_WithLocalJsonProvider_ReturnsJsonRepository()
    {
        var options = new RepositorySelectionOptions(
            RepositoryProvider.LocalJson,
            TestDataPaths.DataJsonFile,
            null,
            null);

        var factory = new RepositoryFactory();

        var result = factory.Create(options);

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

        var factory = new RepositoryFactory();

        Action act = () => factory.Create(options);

        act.Should().Throw<FileNotFoundException>()
            .WithMessage("*Google Drive credentials file path is required*");
    }
}

