using Financial.Shared.Abstractions;
using Financial.Shared.Infrastructure.Persistence;
using Financial.TestUtilities;
using FluentAssertions;

namespace Financial.Shared.Infrastructure.Tests.Persistence;

public class GoogleDriveJsonStorageTests
{
    /// <summary>The tracer is the same in every span test; the download/upload callbacks are what each test varies.</summary>
    private readonly RecordingTelemetryTracer _tracer;

    public GoogleDriveJsonStorageTests()
    {
        _tracer = new RecordingTelemetryTracer();
    }

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

    [Fact]
    public async Task ReadAsync_RecordsGoogleDriveDownloadSpan()
    {
        var storage = new GoogleDriveJsonStorage(
            _ => "{\"data\":true}",
            (_, _) => { },
            "Pessoais/Gleison/Financeiros",
            _tracer);

        await storage.ReadAsync();

        var span = _tracer.Spans.Should().ContainSingle().Which;
        span.Name.Should().Be("GoogleDrive.Download");
        span.Attributes[TelemetryAttributeKeys.OperationResult].Should().Be(TelemetryOperationResults.Success);
    }

    [Fact]
    public async Task WriteAsync_RecordsGoogleDriveUploadSpan()
    {
        var storage = new GoogleDriveJsonStorage(
            _ => "{\"data\":true}",
            (_, _) => { },
            "Pessoais/Gleison/Financeiros",
            _tracer);

        await storage.WriteAsync("{\"written\":true}");

        var span = _tracer.Spans.Should().ContainSingle().Which;
        span.Name.Should().Be("GoogleDrive.Upload");
        span.Attributes[TelemetryAttributeKeys.OperationResult].Should().Be(TelemetryOperationResults.Success);
    }

    [Fact]
    public async Task ReadAsync_WhenDownloadThrows_RecordsFailedSpanWithException()
    {
        var storage = new GoogleDriveJsonStorage(
            _ => throw new InvalidOperationException("boom"),
            (_, _) => { },
            "Pessoais/Gleison/Financeiros",
            _tracer);

        var act = async () => await storage.ReadAsync();

        await act.Should().ThrowAsync<InvalidOperationException>();
        var span = _tracer.Spans.Should().ContainSingle().Which;
        span.Attributes[TelemetryAttributeKeys.OperationResult].Should().Be(TelemetryOperationResults.Failed);
        span.RecordedException.Should().NotBeNull();
    }
}
