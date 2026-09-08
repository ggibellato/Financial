using Financial.CashFlow.Application.Interfaces;
using Financial.CashFlow.Application.Models;

namespace Financial.TestUtilities;

/// <summary>In-memory ICalendarConnectionStore test double, standing in for the real
/// local-JSON-file-backed store.</summary>
public sealed class FakeCalendarConnectionStore : ICalendarConnectionStore
{
    private CalendarConnection? _connection;

    public int SaveCallCount { get; private set; }
    public int DeleteCallCount { get; private set; }

    /// <summary>When set, Load() throws this instead of returning - simulates the real
    /// file-backed store racing a concurrent Save() (see GoogleCalendarProviderAdapterTests /
    /// CreditCardCalendarSyncServiceTests for the scenario this stands in for).</summary>
    public Exception? ThrowOnLoad { get; set; }

    public CalendarConnection? Load() => ThrowOnLoad is null ? _connection : throw ThrowOnLoad;

    public void Save(CalendarConnection connection)
    {
        SaveCallCount++;
        _connection = connection;
    }

    public void Delete()
    {
        DeleteCallCount++;
        _connection = null;
    }
}
