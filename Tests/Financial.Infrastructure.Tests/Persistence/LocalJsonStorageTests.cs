using Financial.Infrastructure.Persistence;
using FluentAssertions;
using System.IO;

namespace Financial.Infrastructure.Tests.Persistence;

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

    [Fact]
    public async Task Constructor_WithNullPath_UsesDefaultFileName()
    {
        // Null path resolves to AppContext.BaseDirectory/data.json; file won't exist there in tests
        var storage = new LocalJsonStorage(null);

        Func<Task> act = () => storage.ReadAsync();

        var ex = await act.Should().ThrowAsync<FileNotFoundException>();
        ex.Which.FileName.Should().EndWith(LocalJsonStorage.DefaultDataFileName);
    }

    private static string CreateTempFile(string content)
    {
        var path = Path.Combine(Path.GetTempPath(), $"storage-test-{Guid.NewGuid():N}.json");
        File.WriteAllText(path, content);
        return path;
    }
}
