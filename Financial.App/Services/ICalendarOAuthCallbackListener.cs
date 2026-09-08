namespace Financial.Presentation.App.Services;

public sealed record CalendarOAuthCallbackResult(string? Code, string? State, string? Error);

/// <summary>
/// Receives Google's OAuth redirect directly inside the WPF process - the "loopback" pattern
/// Google documents for native/desktop apps - so connecting the calendar never depends on
/// Financial.Api being started separately. Binds to the authorization URL's own redirect_uri
/// for the lifetime of a single consent flow.
/// </summary>
public interface ICalendarOAuthCallbackListener
{
    Task<CalendarOAuthCallbackResult> ListenAsync(string authorizationUrl, CancellationToken cancellationToken = default);
}
