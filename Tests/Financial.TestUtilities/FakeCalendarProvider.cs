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

    /// <summary>When set, <see cref="FindCalendarByNameAsync"/> returns this id instead of
    /// <see langword="null"/>, simulating an existing calendar with that name already in the
    /// account.</summary>
    public string? ExistingCalendarId { get; set; }
    public int CreateCalendarCallCount { get; private set; }
    public List<string> FindCalendarByNameCalls { get; } = new();

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
        CreateCalendarCallCount++;
        if (CreateCalendarThrows)
        {
            throw new InvalidOperationException("Simulated calendar creation failure.");
        }

        return Task.FromResult(CreatedCalendarId);
    }

    public Task<string?> FindCalendarByNameAsync(string accessToken, string calendarName, CancellationToken cancellationToken = default)
    {
        FindCalendarByNameCalls.Add(calendarName);
        return Task.FromResult(ExistingCalendarId);
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

    public string CreatedEventId { get; set; } = "event-id";
    public bool CreateEventThrows { get; set; }
    public bool UpdateEventThrows { get; set; }
    public bool DeleteEventThrows { get; set; }

    /// <summary>When set, any event call (create/update/delete) targeting this calendar id
    /// throws <see cref="CalendarNotFoundException"/> - calls against any other calendar id
    /// (e.g. one created after self-healing) succeed normally, matching how a real "the old
    /// calendar was deleted but the new one works" scenario behaves.</summary>
    public string? NotFoundCalendarId { get; set; }

    public List<(string CalendarId, string Title, string Description, DateOnly Date)> CreatedEvents { get; } = new();
    public List<(string CalendarId, string EventId, string Title, string Description, DateOnly Date)> UpdatedEvents { get; } = new();
    public List<(string CalendarId, string EventId)> DeletedEvents { get; } = new();

    public Task<string> CreateEventAsync(
        string accessToken, string calendarId, string title, string description, DateOnly date, CancellationToken cancellationToken = default)
    {
        CreatedEvents.Add((calendarId, title, description, date));
        if (calendarId == NotFoundCalendarId)
        {
            throw new CalendarNotFoundException("Calendar not found.");
        }

        if (CreateEventThrows)
        {
            throw new InvalidOperationException("Simulated event create failure.");
        }

        return Task.FromResult(CreatedEventId);
    }

    public Task UpdateEventAsync(
        string accessToken, string calendarId, string eventId, string title, string description, DateOnly date, CancellationToken cancellationToken = default)
    {
        UpdatedEvents.Add((calendarId, eventId, title, description, date));
        if (calendarId == NotFoundCalendarId)
        {
            throw new CalendarNotFoundException("Calendar not found.");
        }

        if (UpdateEventThrows)
        {
            throw new InvalidOperationException("Simulated event update failure.");
        }

        return Task.CompletedTask;
    }

    public Task DeleteEventAsync(string accessToken, string calendarId, string eventId, CancellationToken cancellationToken = default)
    {
        DeletedEvents.Add((calendarId, eventId));
        if (calendarId == NotFoundCalendarId)
        {
            throw new CalendarNotFoundException("Calendar not found.");
        }

        if (DeleteEventThrows)
        {
            throw new InvalidOperationException("Simulated event delete failure.");
        }

        return Task.CompletedTask;
    }
}
