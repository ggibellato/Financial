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

    public CalendarConnection? Load() => _connection;

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
