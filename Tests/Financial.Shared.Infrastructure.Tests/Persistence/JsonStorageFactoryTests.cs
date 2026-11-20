using Financial.Shared.Abstractions.Observability;
using Financial.Shared.Abstractions.Persistence;
using Financial.Shared.Infrastructure.Persistence;
using Financial.Shared.Abstractions.Sync;
using FluentAssertions;

namespace Financial.Shared.Infrastructure.Tests.Persistence;

[Trait("Category", "Integration")]
public class JsonStorageFactoryTests
{
    [Fact]
    public async Task CreateLocal_ReturnsWorkingLocalJsonStorage()
    {
        var factory = new JsonStorageFactory(null, NoOpTelemetryTracer.Instance);
        var tempFile = Path.Combine(Path.GetTempPath(), $"json-storage-factory-{Guid.NewGuid():N}.json");
        try
        {
            var storage = factory.CreateLocal(tempFile, LocalJsonStorage.DefaultDataFileName);

            await storage.WriteAsync("{\"written\":true}");
            var content = await storage.ReadAsync();

            content.Should().Be("{\"written\":true}");
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public void CreateRemote_WithoutRemoteFileClientFactory_ThrowsInvalidOperationException()
    {
        var factory = new JsonStorageFactory(null, NoOpTelemetryTracer.Instance);
        var credentialsPath = Path.GetTempFileName();
        try
        {
            Action act = () => factory.CreateRemote(
                credentialsPath, "Pessoais/Gleison/Financeiros", "Test:CredentialsPath", "TestProvider");

            act.Should().Throw<InvalidOperationException>().WithMessage("*IRemoteFileClientFactory*");
        }
        finally
        {
            File.Delete(credentialsPath);
        }
    }

    [Fact]
    public void CreateRemote_WithRemoteFileClientFactory_ReturnsDebounceWrappedStorage()
    {
        var factory = new JsonStorageFactory(new StubRemoteFileClientFactory(new StubRemoteFileClient(() => "{}")), NoOpTelemetryTracer.Instance);
        var credentialsPath = Path.GetTempFileName();
        try
        {
            var storage = factory.CreateRemote(
                credentialsPath, "Pessoais/Gleison/Financeiros", "Test:CredentialsPath", "TestProvider");

            // DebouncedJsonStorage is the only IJsonStorage in this project that also implements
            // ISyncStatusProvider - this is how CreateRemote's debounce-wrapping (vs CreateLocal's
            // direct, unwrapped storage) is observable through the IJsonStorage-typed return value.
            var syncStatusProvider = storage.Should().BeAssignableTo<ISyncStatusProvider>().Which;
            syncStatusProvider.GetStatus().State.Should().Be(SyncState.Idle);
        }
        finally
        {
            File.Delete(credentialsPath);
        }
    }

    [Fact]
    public async Task CreateRemote_WithATracer_RecordsTheSaveAndUploadSpansOnFlush()
    {
        var tracer = new RecordingTelemetryTracer();
        var factory = new JsonStorageFactory(new StubRemoteFileClientFactory(new RecordingRemoteFileClient(() => "{}")), tracer);
        var credentialsPath = Path.GetTempFileName();
        try
        {
            var storage = factory.CreateRemote(
                credentialsPath, "Pessoais/Gleison/Financeiros", "Test:CredentialsPath", "TestProvider");

            await storage.WriteAsync("{\"written\":true}");
            await ((ISyncStatusProvider)storage).FlushAsync();

            tracer.ShouldHaveSucceededSpan("JsonStorage.Save");
            tracer.ShouldHaveSucceededSpan("RemoteStorage.Upload");
        }
        finally
        {
            File.Delete(credentialsPath);
        }
    }
}
