using Financial.CashFlow.Application.Models;
using Financial.CashFlow.Infrastructure.Persistence;
using FluentAssertions;

namespace Financial.CashFlow.Infrastructure.Tests.Persistence;

public class GoogleCalendarConnectionStoreTests
{
    private static string CreateTempPath() =>
        Path.Combine(Path.GetTempPath(), $"google-calendar-credentials-{Guid.NewGuid():N}.json");

    [Fact]
    public void Load_WhenFileDoesNotExist_ReturnsNull()
    {
        var store = new GoogleCalendarConnectionStore(CreateTempPath());

        store.Load().Should().BeNull();
    }

    [Fact]
    public void Save_ThenLoad_RoundTripsTheConnection()
    {
        var path = CreateTempPath();
        var store = new GoogleCalendarConnectionStore(path);
        var connection = new GoogleCalendarConnection(
            "user@gmail.com",
            "calendar-id",
            "access-token",
            "refresh-token",
            new DateTimeOffset(2026, 9, 7, 12, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 9, 1, 10, 15, 0, TimeSpan.Zero));

        try
        {
            store.Save(connection);
            var loaded = store.Load();

            loaded.Should().Be(connection);
        }
        finally
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
    }

    [Fact]
    public void Delete_RemovesTheFile()
    {
        var path = CreateTempPath();
        var store = new GoogleCalendarConnectionStore(path);
        store.Save(new GoogleCalendarConnection("user@gmail.com", "cal", "at", "rt", DateTimeOffset.UtcNow, DateTimeOffset.UtcNow));

        store.Delete();

        File.Exists(path).Should().BeFalse();
        store.Load().Should().BeNull();
    }

    [Fact]
    public void Delete_WhenNothingStored_DoesNotThrow()
    {
        var store = new GoogleCalendarConnectionStore(CreateTempPath());

        Action act = () => store.Delete();

        act.Should().NotThrow();
    }
}
