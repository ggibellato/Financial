using Financial.CashFlow.Application.Models;
using Financial.CashFlow.Infrastructure.Persistence;
using FluentAssertions;

namespace Financial.CashFlow.Infrastructure.Tests.Persistence;

public class CalendarConnectionStoreTests
{
    private static string CreateTempPath() =>
        Path.Combine(Path.GetTempPath(), $"calendar-credentials-{Guid.NewGuid():N}.json");

    [Fact]
    public void Load_WhenFileDoesNotExist_ReturnsNull()
    {
        var store = new CalendarConnectionStore(CreateTempPath());

        store.Load().Should().BeNull();
    }

    [Fact]
    public void Save_ThenLoad_RoundTripsTheConnection()
    {
        var path = CreateTempPath();
        var store = new CalendarConnectionStore(path);
        var connection = new CalendarConnection(
            "user@gmail.com",
            "calendar-id",
            "access-token",
            "refresh-token",
            new DateTimeOffset(2026, 9, 7, 12, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 9, 1, 10, 15, 0, TimeSpan.Zero),
            new Dictionary<Guid, string> { [Guid.Parse("8f3b1c1a-2e3a-4b1a-9a7f-500000000001")] = "event-id" });

        try
        {
            store.Save(connection);
            var loaded = store.Load();

            // BeEquivalentTo, not Be: record equality on IReadOnlyDictionary falls back to
            // reference equality, and deserialization always produces a new dictionary instance.
            loaded.Should().BeEquivalentTo(connection);
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
        var store = new CalendarConnectionStore(path);
        store.Save(new CalendarConnection("user@gmail.com", "cal", "at", "rt", DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, new Dictionary<Guid, string>()));

        store.Delete();

        File.Exists(path).Should().BeFalse();
        store.Load().Should().BeNull();
    }

    [Fact]
    public void Delete_WhenNothingStored_DoesNotThrow()
    {
        var store = new CalendarConnectionStore(CreateTempPath());

        Action act = () => store.Delete();

        act.Should().NotThrow();
    }
}
