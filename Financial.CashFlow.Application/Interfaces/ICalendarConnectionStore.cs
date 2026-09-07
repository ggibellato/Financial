using Financial.CashFlow.Application.Models;

namespace Financial.CashFlow.Application.Interfaces;

/// <summary>Reads/writes the local calendar-connection credentials file, separate from the
/// CashFlow repository's <c>data-cashflow.json</c>. Provider-agnostic: it persists whichever
/// <see cref="ICalendarProvider"/> connection is active.</summary>
public interface ICalendarConnectionStore
{
    /// <summary>Returns <see langword="null"/> when no connection has been established yet.</summary>
    CalendarConnection? Load();

    void Save(CalendarConnection connection);

    /// <summary>No-op when nothing is stored.</summary>
    void Delete();
}
