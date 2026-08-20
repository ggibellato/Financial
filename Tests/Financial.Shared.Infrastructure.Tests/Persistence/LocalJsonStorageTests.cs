using Financial.Shared.Infrastructure.Persistence;
using FluentAssertions;
using System.IO;

namespace Financial.Shared.Infrastructure.Tests.Persistence;

public class LocalJsonStorageTests
{
    [Fact]
    public async Task ReadAsync_WhenFileExists_ReturnsContent()
    {
        var tempFile = CreateTempFile("{\"test\": true}");
        try
        {
            var storage = new LocalJsonStorage(tempFile);

            var result = await storage.ReadAsync();

            result.Should().Be("{\"test\": true}");
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public async Task ReadAsync_WhenFileDoesNotExist_ThrowsFileNotFoundException()
    {
        var nonExistentPath = Path.Combine(Path.GetTempPath(), $"nonexistent-{Guid.NewGuid():N}.json");
        var storage = new LocalJsonStorage(nonExistentPath);

        Func<Task> act = () => storage.ReadAsync();

        await act.Should().ThrowAsync<FileNotFoundException>();
    }

    [Fact]
    public async Task WriteAsync_WritesContentToFile()
    {
        var tempFile = CreateTempFile("initial");
        try
        {
            var storage = new LocalJsonStorage(tempFile);

            await storage.WriteAsync("{\"written\": true}");

            var content = await File.ReadAllTextAsync(tempFile);
            content.Should().Be("{\"written\": true}");
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    /// <summary>
    /// The classic staged-write regression: renaming a shorter document over a longer one must
    /// replace the file, not overwrite its first bytes and leave the old tail behind.
    /// </summary>
    [Fact]
    public async Task WriteAsync_WithShorterContentThanExisting_ReplacesTheFileEntirely()
    {
        var tempFile = CreateTempFile("{\"a\": 1, \"b\": 2, \"c\": 3, \"padding\": \"aaaaaaaaaaaaaaaaaaaa\"}");
        try
        {
            var storage = new LocalJsonStorage(tempFile);

            await storage.WriteAsync("{}");

            var content = await File.ReadAllTextAsync(tempFile);
            content.Should().Be("{}");
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public async Task WriteAsync_LeavesNoStagedFileBehind()
    {
        var tempFile = CreateTempFile("initial");
        try
        {
            var storage = new LocalJsonStorage(tempFile);

            await storage.WriteAsync("{\"written\": true}");

            File.Exists(tempFile + LocalJsonStorage.TemporaryFileSuffix).Should().BeFalse();
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    /// <summary>
    /// The whole point of staging: a write that never completes must leave the previous document
    /// readable, rather than a truncated file that fails to load at startup.
    /// </summary>
    [Fact]
    public async Task WriteAsync_WhenTheStagedWriteFails_LeavesTheExistingDocumentIntact()
    {
        var tempFile = CreateTempFile("{\"original\": true}");
        var blockedStagingPath = tempFile + LocalJsonStorage.TemporaryFileSuffix;
        Directory.CreateDirectory(blockedStagingPath);
        try
        {
            var storage = new LocalJsonStorage(tempFile);

            Func<Task> act = () => storage.WriteAsync("{\"replacement\": true}");

            await act.Should().ThrowAsync<UnauthorizedAccessException>();
            var content = await File.ReadAllTextAsync(tempFile);
            content.Should().Be("{\"original\": true}");
        }
        finally
        {
            Directory.Delete(blockedStagingPath, recursive: true);
            File.Delete(tempFile);
        }
    }

    [Fact]
    public async Task Constructor_WithNullPath_UsesDefaultFileName()
    {
        // Null path resolves to AppContext.BaseDirectory/data.json; file won't exist there in tests
        var storage = new LocalJsonStorage(null);

        Func<Task> act = () => storage.ReadAsync();

        var ex = await act.Should().ThrowAsync<FileNotFoundException>();
        ex.Which.FileName.Should().EndWith(LocalJsonStorage.DefaultDataFileName);
    }

    [Fact]
    public async Task Constructor_WithDirectoryPath_AppendsDefaultFileName()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"storage-test-dir-{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        try
        {
            var storage = new LocalJsonStorage(tempDir);

            Func<Task> act = () => storage.ReadAsync();

            var ex = await act.Should().ThrowAsync<FileNotFoundException>();
            ex.Which.FileName.Should().Be(Path.Combine(tempDir, LocalJsonStorage.DefaultDataFileName));
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    [Fact]
    public async Task Constructor_WithRelativePath_ResolvesAgainstBaseDirectory()
    {
        var relativeFileName = $"relative-storage-test-{Guid.NewGuid():N}.json";
        var storage = new LocalJsonStorage(relativeFileName);

        Func<Task> act = () => storage.ReadAsync();

        var ex = await act.Should().ThrowAsync<FileNotFoundException>();
        ex.Which.FileName.Should().Be(Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, relativeFileName)));
    }

    private static string CreateTempFile(string content)
    {
        var path = Path.Combine(Path.GetTempPath(), $"storage-test-{Guid.NewGuid():N}.json");
        File.WriteAllText(path, content);
        return path;
    }
}
