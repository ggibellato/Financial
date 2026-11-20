using Financial.Shared.Abstractions.Configuration;
using Financial.Shared.Abstractions.Currencies.FxRates;
using Financial.Shared.Abstractions.Observability;
using Financial.Shared.Infrastructure.Persistence;
using Financial.Shared.Infrastructure.Persistence.FxRates;
using Financial.Shared.Infrastructure.Repositories.FxRates;
using Financial.TestUtilities;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;

namespace Financial.Shared.Infrastructure.Tests.Repositories.FxRates;

[Trait("Category", "Integration")]
public class FxRateStoreFactoryTests
{
    private static readonly FxRateStoreFactory Factory = new(
        new FxRateSerializerAdapter(),
        new JsonStorageFactory(
            new StubRemoteFileClientFactory(
                new StubRemoteFileClient(() => new FxRateSerializerAdapter().Serialize(new Dictionary<DateOnly, FxRateRecord>()))),
            NoOpTelemetryTracer.Instance),
        TestClock.At(),
        NullLogger<FxRateJsonStore>.Instance);

    [Fact]
    public void Create_WithLocalJsonProvider_MissingFile_ReturnsEmptyStore()
    {
        var missingPath = Path.Combine(Path.GetTempPath(), $"fxrates-factory-{Guid.NewGuid()}.json");
        var options = new JsonRepositorySelection(JsonRepositoryProvider.LocalJson, missingPath, null, null);

        var result = Factory.Create(options);

        result.Should().BeOfType<FxRateJsonStore>();
        result.TryGetRate(new DateOnly(2026, 9, 18)).Should().BeNull();
    }

    [Fact]
    public async Task Create_WithLocalJsonProvider_ReloadsARateItStoredInTheFile()
    {
        var path = Path.Combine(Path.GetTempPath(), $"fxrates-factory-{Guid.NewGuid()}.json");
        var options = new JsonRepositorySelection(JsonRepositoryProvider.LocalJson, path, null, null);
        var record = new FxRateRecord(5.45m, 0.77m, "frankfurter", TestClock.Default);
        try
        {
            var store = Factory.Create(options);
            await store.SetRateAsync(new DateOnly(2026, 9, 18), record);
            await store.FlushAsync();

            Factory.Create(options).TryGetRate(new DateOnly(2026, 9, 18)).Should().Be(record);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void Create_WithGoogleDriveProvider_ValidCredentials_ReturnsFxRateJsonStore()
    {
        var credentialsPath = Path.GetTempFileName();
        try
        {
            var options = new JsonRepositorySelection(
                JsonRepositoryProvider.GoogleDriveJson, null, credentialsPath, "Pessoais/Gleison/Financeiros");

            var result = Factory.Create(options);

            result.Should().BeOfType<FxRateJsonStore>();
        }
        finally
        {
            File.Delete(credentialsPath);
        }
    }
}
