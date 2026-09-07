namespace Financial.CashFlow.Application.Exceptions;

/// <summary>
/// Thrown by <see cref="Interfaces.ICalendarProvider.RefreshAccessTokenAsync"/> when a provider
/// rejects the stored refresh token (revoked externally, e.g. the user removed app access in
/// their account settings) - a permanent failure, distinct from a transient API error. Named
/// generically because token revocation is a standard OAuth outcome, not a Google-specific one.
/// </summary>
public sealed class CalendarTokenRevokedException : Exception
{
    public CalendarTokenRevokedException(string message) : base(message)
    {
    }
}
