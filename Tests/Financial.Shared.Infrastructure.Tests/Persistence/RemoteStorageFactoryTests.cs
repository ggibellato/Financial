using Financial.Shared.Infrastructure.Persistence;
using FluentAssertions;

namespace Financial.Shared.Infrastructure.Tests.Persistence;

[Trait("Category", "Integration")]
public class RemoteStorageFactoryTests
{
    private const string CredentialsConfigKey = "Test:GoogleDriveCredentialsPath";

    [Theory]
    [InlineData(null)]
    [InlineData(" ")]
    public void Create_WithoutACredentialsPath_ThrowsFileNotFoundNamingTheConfigKey(string? credentialsPath)
    {
        Action act = () => Create(credentialsPath);

        act.Should().Throw<FileNotFoundException>()
            .WithMessage($"*credentials file path is required*{CredentialsConfigKey}*");
    }

    [Fact]
    public void Create_CredentialsFileMissing_ThrowsFileNotFoundWithTheResolvedPath()
    {
        var fileName = $"missing-credentials-{Guid.NewGuid()}.json";

        Action act = () => Create(fileName);

        act.Should().Throw<FileNotFoundException>()
            .Which.FileName.Should().Be(Path.Combine(AppContext.BaseDirectory, fileName));
    }

    [Fact]
    public void Create_RelativeCredentialsPath_OpensTheClientWithThePathUnderTheBaseDirectory()
    {
        var fileName = $"credentials-{Guid.NewGuid()}.json";
        var absolutePath = Path.Combine(AppContext.BaseDirectory, fileName);
        File.WriteAllText(absolutePath, "{}");
        var clientFactory = new StubRemoteFileClientFactory(new StubRemoteFileClient(() => "{}"));
        try
        {
            Create(fileName, clientFactory);

            clientFactory.LastCredentialsPath.Should().Be(absolutePath);
        }
        finally
        {
            File.Delete(absolutePath);
        }
    }

    private static void Create(string? credentialsPath, StubRemoteFileClientFactory? clientFactory = null) =>
        RemoteStorageFactory.Create(
            credentialsPath,
            "Pessoais/Financeiros",
            clientFactory ?? new StubRemoteFileClientFactory(new StubRemoteFileClient(() => "{}")),
            CredentialsConfigKey,
            "TestProvider");
}
