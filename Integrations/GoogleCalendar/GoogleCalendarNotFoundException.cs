namespace Financial.Integrations.GoogleCalendar;

/// <summary>
/// Thrown when a calendar-scoped Google Calendar API call (event create/update/delete) returns
/// 404 for the given calendar id - the dedicated calendar was deleted outside the app.
/// </summary>
public sealed class GoogleCalendarNotFoundException : Exception
{
    public GoogleCalendarNotFoundException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
