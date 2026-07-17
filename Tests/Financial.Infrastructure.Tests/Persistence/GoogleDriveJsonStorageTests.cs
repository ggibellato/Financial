using Financial.Infrastructure.Persistence;
using FluentAssertions;

namespace Financial.Infrastructure.Tests.Persistence;

public class GoogleDriveJsonStorageTests
{
    [Fact]
    public void Constructor_WithNullClient_ThrowsArgumentNullException()
    {
        Action act = () => new GoogleDriveJsonStorage(null!, "some/path");

        act.Should().Throw<ArgumentNullException>().WithParameterName("client");
    }

    [Fact]
    public void Constructor_WithBlankDriveFilePath_ThrowsArgumentException()
    {
        Action act = () => new GoogleDriveJsonStorage(_ => "content", (_, _) => { }, "");

        act.Should().Throw<ArgumentException>().WithParameterName("driveFilePath");
    }

    [Fact]
    public async Task ReadAsync_DelegatesToDownloadWithDriveFilePath()
    {
        string? capturedPath = null;
        var storage = new GoogleDriveJsonStorage(
            path => { capturedPath = path; return "{\"data\":true}"; },
            (_, _) => throw new InvalidOperationException("upload should not be called"),
            "Pessoais/Gleison/Financeiros");

        var result = await storage.ReadAsync();

        result.Should().Be("{\"data\":true}");
        capturedPath.Should().Be("Pessoais/Gleison/Financeiros");
    }

    [Fact]
    public async Task WriteAsync_DelegatesToUploadWithDriveFilePathAndContent()
    {
        string? capturedPath = null;
        string? capturedContent = null;
        var storage = new GoogleDriveJsonStorage(
            _ => throw new InvalidOperationException("download should not be called"),
            (path, content) => { capturedPath = path; capturedContent = content; },
            "Pessoais/Gleison/Financeiros");

        await storage.WriteAsync("{\"written\":true}");

        capturedPath.Should().Be("Pessoais/Gleison/Financeiros");
        capturedContent.Should().Be("{\"written\":true}");
    }
}
