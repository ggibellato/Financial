using Financial.Shared.Abstractions.Configuration;
using Financial.Shared.Abstractions.Persistence;
using FluentAssertions;

namespace Financial.Shared.Abstractions.Tests.Persistence;

[Trait("Category", "Unit")]
public class JsonStorageFactoryExtensionsTests
{
    private static readonly JsonRepositoryConfigurationKeys Keys = new("Investment");

    private readonly RecordingJsonStorageFactory _storageFactory = new();

    [Fact]
    public void Create_LocalJson_CreatesLocalStorageWithTheContextsDefaultFileName()
    {
        var selection = new JsonRepositorySelection(JsonRepositoryProvider.LocalJson, "data/custom.json", "unused", "unused");

        var storage = _storageFactory.Create(selection, "data-investment.json", Keys);

        storage.Should().BeSameAs(_storageFactory.Storage);
        _storageFactory.Calls.Should().Equal("local:data/custom.json|data-investment.json");
    }

    [Fact]
    public void Create_GoogleDriveJson_CreatesRemoteStorageNamingTheContextsCredentialsKey()
    {
        var selection = new JsonRepositorySelection(
            JsonRepositoryProvider.GoogleDriveJson, "unused", "credentials.json", "Financial/data-investment.json");

        var storage = _storageFactory.Create(selection, "data-investment.json", Keys);

        storage.Should().BeSameAs(_storageFactory.Storage);
        _storageFactory.Calls.Should().Equal(
            "remote:credentials.json|Financial/data-investment.json|Investment:GoogleDrive:CredentialsPath|GoogleDriveJson");
    }

    [Fact]
    public void Create_UnsupportedProvider_ThrowsArgumentOutOfRangeException()
    {
        var selection = new JsonRepositorySelection((JsonRepositoryProvider)999, null, null, null);

        var act = () => _storageFactory.Create(selection, "data-investment.json", Keys);

        act.Should().Throw<ArgumentOutOfRangeException>().WithParameterName("Provider");
        _storageFactory.Calls.Should().BeEmpty();
    }

    private sealed class RecordingJsonStorageFactory : IJsonStorageFactory
    {
        public List<string> Calls { get; } = [];

        public IJsonStorage Storage { get; } = new FakeSyncStatusStorage();

        public IJsonStorage CreateLocal(string? localDataPath, string defaultDataFileName)
        {
            Calls.Add($"local:{localDataPath}|{defaultDataFileName}");
            return Storage;
        }

        public IJsonStorage CreateRemote(
            string? credentialsPath,
            string? remoteFilePath,
            string credentialsConfigKey,
            string providerName)
        {
            Calls.Add($"remote:{credentialsPath}|{remoteFilePath}|{credentialsConfigKey}|{providerName}");
            return Storage;
        }
    }
}
