using Financial.Shared.Abstractions.Observability;
using Financial.Shared.Abstractions.Persistence;
using Financial.Shared.Infrastructure.Persistence;
using Financial.Shared.Abstractions.Sync;
using FluentAssertions;

namespace Financial.Shared.Infrastructure.Tests.Persistence;

public class JsonStorageFactoryTests
{
    [Fact]
    public void Constructor_WithNullTracer_ThrowsArgumentNullException()
    {
        Action act = () => new JsonStorageFactory(null, null!);

        act.Should().Throw<ArgumentNullException>().WithParameterName("tracer");
    }

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
        var factory = new JsonStorageFactory(new StubRemoteFileClientFactory(), NoOpTelemetryTracer.Instance);
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

    private sealed class StubRemoteFileClientFactory : IRemoteFileClientFactory
    {
        public IRemoteFileClient Create(string credentialsPath) => new StubRemoteFileClient();
    }

    private sealed class StubRemoteFileClient : IRemoteFileClient
    {
        public string DownloadFileContent(string path) => "{}";

        public void UploadFileContent(string path, string content) => throw new NotSupportedException();
    }
}
