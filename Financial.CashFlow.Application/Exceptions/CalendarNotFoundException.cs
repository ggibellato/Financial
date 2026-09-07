namespace Financial.CashFlow.Application.Exceptions;

/// <summary>
/// Thrown by <see cref="Interfaces.ICalendarProvider"/>'s event methods when the calendar itself
/// no longer exists (e.g. the dedicated calendar was deleted outside the app) - a signal to
/// recreate the calendar and re-sync, not a generic transient failure.
/// </summary>
public sealed class CalendarNotFoundException : Exception
{
    public CalendarNotFoundException(string message) : base(message)
    {
    }
}
