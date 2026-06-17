using Financial.Infrastructure.Persistence;
using Financial.Infrastructure.Repositories;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
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

    [Fact]
    public void CreateFromConfiguration_WithLocalJsonProvider_ReturnsJsonRepository()
    {
        var configuration = BuildConfiguration(new Dictionary<string, string?>
        {
            ["Repository:Provider"] = "LocalJson",
            ["DataJsonFile"] = TestDataPaths.DataJsonFile
        });

        var result = Factory.CreateFromConfiguration(configuration);

        result.Should().BeOfType<JSONRepository>();
    }

    [Fact]
    public void CreateFromConfiguration_WithNoProvider_DefaultsToLocalJson()
    {
        var configuration = BuildConfiguration(new Dictionary<string, string?>
        {
            ["DataJsonFile"] = TestDataPaths.DataJsonFile
        });

        var result = Factory.CreateFromConfiguration(configuration);

        result.Should().BeOfType<JSONRepository>();
    }

    [Fact]
    public void CreateFromConfiguration_WithUnsupportedProvider_ThrowsInvalidOperationException()
    {
        var configuration = BuildConfiguration(new Dictionary<string, string?>
        {
            ["Repository:Provider"] = "Unknown"
        });

        Action act = () => Factory.CreateFromConfiguration(configuration);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Repository provider 'Unknown' is not supported*");
    }

    private static IConfiguration BuildConfiguration(Dictionary<string, string?> values) =>
        new ConfigurationBuilder().AddInMemoryCollection(values).Build();
}
