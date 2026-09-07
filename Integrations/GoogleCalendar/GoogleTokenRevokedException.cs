namespace Financial.Integrations.GoogleCalendar;

/// <summary>
/// Thrown when Google rejects a token exchange or refresh with an "invalid_grant" error - the
/// refresh token has been revoked (e.g. the user removed app access in their Google Account
/// settings), not a transient failure that a retry could recover from.
/// </summary>
public sealed class GoogleTokenRevokedException : Exception
{
    public GoogleTokenRevokedException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
