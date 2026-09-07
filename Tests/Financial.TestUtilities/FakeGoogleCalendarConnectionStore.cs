using Financial.CashFlow.Application.Interfaces;
using Financial.CashFlow.Application.Models;

namespace Financial.TestUtilities;

/// <summary>In-memory IGoogleCalendarConnectionStore test double, standing in for the real
/// local-JSON-file-backed store.</summary>
public sealed class FakeGoogleCalendarConnectionStore : IGoogleCalendarConnectionStore
{
    private GoogleCalendarConnection? _connection;

    public int SaveCallCount { get; private set; }
    public int DeleteCallCount { get; private set; }

    public GoogleCalendarConnection? Load() => _connection;

    public void Save(GoogleCalendarConnection connection)
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
