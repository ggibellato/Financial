using Financial.Shared.Abstractions.Currencies.FxRates;
using Financial.Shared.Abstractions.Observability;
using Financial.Shared.Abstractions.Persistence;
using Financial.Shared.Infrastructure.Persistence;
using Financial.Shared.Infrastructure.Persistence.FxRates;
using Financial.Shared.Infrastructure.Repositories.FxRates;
using FluentAssertions;

namespace Financial.Shared.Infrastructure.Tests.Repositories.FxRates;

public class FxRateStoreFactoryTests
{
    private static readonly FxRateStoreFactory Factory =
        new(new FxRateSerializerAdapter(), new JsonStorageFactory(new StubRemoteFileClientFactory(), NoOpTelemetryTracer.Instance));

    [Fact]
    public void Constructor_WithNullSerializer_Throws()
    {
        Action act = () => new FxRateStoreFactory(null!, new JsonStorageFactory(null, NoOpTelemetryTracer.Instance));
        act.Should().Throw<ArgumentNullException>().WithParameterName("serializer");
    }

    [Fact]
    public void Constructor_WithNullStorageFactory_Throws()
    {
        Action act = () => new FxRateStoreFactory(new FxRateSerializerAdapter(), null!);
        act.Should().Throw<ArgumentNullException>().WithParameterName("storageFactory");
    }

    [Fact]
    public void Create_WithNullOptions_Throws()
    {
        Action act = () => Factory.Create(null!);
        act.Should().Throw<ArgumentNullException>().WithParameterName("options");
    }

    [Fact]
    public void Create_WithLocalJsonProvider_MissingFile_ReturnsEmptyStore()
    {
        var missingPath = Path.Combine(Path.GetTempPath(), $"fxrates-factory-{Guid.NewGuid()}.json");
        var options = new FxRateRepositorySelectionOptions(FxRateRepositoryProvider.LocalJson, missingPath, null, null);

        var result = Factory.Create(options);

        result.Should().BeOfType<FxRateJsonStore>();
        result.TryGetRate(new DateOnly(2026, 9, 18)).Should().BeNull();
    }

    [Fact]
    public void Create_WithGoogleDriveProvider_WithoutCredentials_ThrowsFileNotFoundException()
    {
        var options = new FxRateRepositorySelectionOptions(
            FxRateRepositoryProvider.GoogleDriveJson, null, null, "Pessoais/Gleison/Financeiros");

        Action act = () => Factory.Create(options);

        act.Should().Throw<FileNotFoundException>()
            .WithMessage("*Remote storage credentials file path is required*");
    }

    [Fact]
    public void Create_WithUnsupportedProvider_ThrowsArgumentOutOfRangeException()
    {
        var options = new FxRateRepositorySelectionOptions((FxRateRepositoryProvider)999, null, null, null);

        Action act = () => Factory.Create(options);

        act.Should().Throw<ArgumentOutOfRangeException>().WithParameterName("Provider");
    }

    [Fact]
    public void Create_WithGoogleDriveProvider_ValidCredentials_ReturnsFxRateJsonStore()
    {
        var credentialsPath = Path.GetTempFileName();
        try
        {
            var options = new FxRateRepositorySelectionOptions(
                FxRateRepositoryProvider.GoogleDriveJson, null, credentialsPath, "Pessoais/Gleison/Financeiros");

            var result = Factory.Create(options);

            result.Should().BeOfType<FxRateJsonStore>();
        }
        finally
        {
            File.Delete(credentialsPath);
        }
    }

    private sealed class StubRemoteFileClientFactory : IRemoteFileClientFactory
    {
        public IRemoteFileClient Create(string credentialsPath) => new StubRemoteFileClient();
    }

    private sealed class StubRemoteFileClient : IRemoteFileClient
    {
        public string DownloadFileContent(string path) => new FxRateSerializerAdapter().Serialize(new Dictionary<DateOnly, FxRateRecord>());
        public void UploadFileContent(string path, string content) => throw new NotSupportedException();
    }
}
