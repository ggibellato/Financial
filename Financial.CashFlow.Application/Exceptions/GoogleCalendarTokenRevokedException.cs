namespace Financial.CashFlow.Application.Exceptions;

/// <summary>
/// Thrown by <see cref="Interfaces.IGoogleCalendarClient.RefreshAccessTokenAsync"/> when Google
/// rejects the stored refresh token (revoked externally, e.g. the user removed app access in
/// their Google Account settings) - a permanent failure, distinct from a transient API error.
/// </summary>
public sealed class GoogleCalendarTokenRevokedException : Exception
{
    public GoogleCalendarTokenRevokedException(string message) : base(message)
    {
    }
}
