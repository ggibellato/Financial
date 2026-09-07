using Financial.CashFlow.Application.Models;

namespace Financial.CashFlow.Application.Interfaces;

/// <summary>Reads/writes the local Google Calendar credentials file, separate from the CashFlow
/// repository's <c>data-cashflow.json</c>.</summary>
public interface IGoogleCalendarConnectionStore
{
    /// <summary>Returns <see langword="null"/> when no connection has been established yet.</summary>
    GoogleCalendarConnection? Load();

    void Save(GoogleCalendarConnection connection);

    /// <summary>No-op when nothing is stored.</summary>
    void Delete();
}
