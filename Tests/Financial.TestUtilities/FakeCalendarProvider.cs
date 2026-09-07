using Financial.CashFlow.Application.Exceptions;
using Financial.CashFlow.Application.Interfaces;
using Financial.CashFlow.Application.Models;

namespace Financial.TestUtilities;

/// <summary>Hand-written ICalendarProvider test double - no live network calls, matching this
/// codebase's "test the wiring, not the live SDK" convention for external providers. Configure
/// the public fields, then read the *CallCount / *Calls lists back.</summary>
public sealed class FakeCalendarProvider : ICalendarProvider
{
    public string AuthorizationUrl { get; set; } = "https://accounts.google.com/o/oauth2/v2/auth?state=fake";
    public CalendarTokenResult ExchangeResult { get; set; } = new("access-token", "refresh-token", DateTimeOffset.UtcNow.AddHours(1));
    public CalendarTokenResult? RefreshResult { get; set; }
    public bool RefreshThrowsRevoked { get; set; }
    public string AccountEmail { get; set; } = "user@gmail.com";
    public string CreatedCalendarId { get; set; } = "calendar-id";
    public bool CreateCalendarThrows { get; set; }
    public bool DeleteCalendarThrows { get; set; }
    public bool RevokeThrows { get; set; }

    public int RevokeCallCount { get; private set; }
    public List<string> RevokedTokens { get; } = new();
    public int RefreshCallCount { get; private set; }
    public List<(string AccessToken, string CalendarId)> DeletedCalendars { get; } = new();
    public string? LastState { get; private set; }
    public int ExchangeCallCount { get; private set; }

    public string BuildAuthorizationUrl(string state)
    {
        LastState = state;
        return AuthorizationUrl;
    }

    public Task<CalendarTokenResult> ExchangeCodeForTokenAsync(string code, CancellationToken cancellationToken = default)
    {
        ExchangeCallCount++;
        return Task.FromResult(ExchangeResult);
    }

    public Task<CalendarTokenResult> RefreshAccessTokenAsync(string refreshToken, CancellationToken cancellationToken = default)
    {
        RefreshCallCount++;
        if (RefreshThrowsRevoked)
        {
            throw new CalendarTokenRevokedException("Refresh token was revoked.");
        }

        return Task.FromResult(RefreshResult ?? ExchangeResult);
    }

    public Task RevokeTokenAsync(string token, CancellationToken cancellationToken = default)
    {
        RevokeCallCount++;
        RevokedTokens.Add(token);
        if (RevokeThrows)
        {
            throw new InvalidOperationException("Simulated token revoke failure.");
        }

        return Task.CompletedTask;
    }

    public Task<string> GetAccountEmailAsync(string accessToken, CancellationToken cancellationToken = default) =>
        Task.FromResult(AccountEmail);

    public Task<string> CreateCalendarAsync(string accessToken, string calendarName, CancellationToken cancellationToken = default)
    {
        if (CreateCalendarThrows)
        {
            throw new InvalidOperationException("Simulated calendar creation failure.");
        }

        return Task.FromResult(CreatedCalendarId);
    }

    public Task DeleteCalendarAsync(string accessToken, string calendarId, CancellationToken cancellationToken = default)
    {
        DeletedCalendars.Add((accessToken, calendarId));
        if (DeleteCalendarThrows)
        {
            throw new InvalidOperationException("Simulated calendar delete failure.");
        }

        return Task.CompletedTask;
    }
}
