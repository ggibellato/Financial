using Financial.CashFlow.Application.Exceptions;
using Financial.CashFlow.Infrastructure.Configuration;
using Financial.CashFlow.Infrastructure.Services;
using Financial.Integrations.GoogleCalendar;
using FluentAssertions;
using Microsoft.Extensions.Options;

namespace Financial.CashFlow.Infrastructure.Tests.Services;

public class GoogleCalendarProviderAdapterTests
{
    private readonly FakeGoogleCalendarOAuthClient _oAuthClient = new();
    private readonly GoogleCalendarSettingsOptions _settings = new()
    {
        ClientId = "client-id",
        ClientSecret = "client-secret",
        RedirectUri = "https://example.test/callback"
    };

    private GoogleCalendarProviderAdapter CreateAdapter() => new(_oAuthClient, Options.Create(_settings));

    [Fact]
    public void BuildAuthorizationUrl_ForwardsConfiguredClientIdRedirectUriAndCalendarScope()
    {
        var adapter = CreateAdapter();

        adapter.BuildAuthorizationUrl("state-value");

        _oAuthClient.LastClientId.Should().Be("client-id");
        _oAuthClient.LastRedirectUri.Should().Be("https://example.test/callback");
        _oAuthClient.LastScope.Should().Be("https://www.googleapis.com/auth/calendar");
        _oAuthClient.LastState.Should().Be("state-value");
    }

    [Fact]
    public async Task ExchangeCodeForTokenAsync_ForwardsConfiguredClientCredentials()
    {
        var adapter = CreateAdapter();

        await adapter.ExchangeCodeForTokenAsync("auth-code");

        _oAuthClient.LastClientId.Should().Be("client-id");
        _oAuthClient.LastClientSecret.Should().Be("client-secret");
        _oAuthClient.LastRedirectUri.Should().Be("https://example.test/callback");
        _oAuthClient.LastCode.Should().Be("auth-code");
    }

    [Fact]
    public async Task RefreshAccessTokenAsync_WhenGoogleRejectsTheRefreshToken_ThrowsApplicationLevelException()
    {
        _oAuthClient.RefreshThrowsRevoked = true;
        var adapter = CreateAdapter();

        Func<Task> act = () => adapter.RefreshAccessTokenAsync("refresh-token");

        await act.Should().ThrowAsync<CalendarTokenRevokedException>();
    }

    [Fact]
    public void BuildAuthorizationUrl_WhenClientIdNotConfigured_Throws()
    {
        _settings.ClientId = null;
        var adapter = CreateAdapter();

        Action act = () => adapter.BuildAuthorizationUrl("state");

        act.Should().Throw<InvalidOperationException>().WithMessage("*ClientId*");
    }

    [Fact]
    public void BuildAuthorizationUrl_WhenClientIdIsAnEmptyString_Throws()
    {
        // appsettings.json ships an empty-string placeholder (not an absent key), which reads back
        // as "" rather than null - this must be treated as "not configured" too.
        _settings.ClientId = "";
        var adapter = CreateAdapter();

        Action act = () => adapter.BuildAuthorizationUrl("state");

        act.Should().Throw<InvalidOperationException>().WithMessage("*ClientId*");
    }

    private sealed class FakeGoogleCalendarOAuthClient : IGoogleCalendarOAuthClient
    {
        public bool RefreshThrowsRevoked { get; set; }
        public string? LastClientId { get; private set; }
        public string? LastClientSecret { get; private set; }
        public string? LastRedirectUri { get; private set; }
        public string? LastScope { get; private set; }
        public string? LastState { get; private set; }
        public string? LastCode { get; private set; }

        public string BuildAuthorizationUrl(string clientId, string redirectUri, string scope, string state)
        {
            LastClientId = clientId;
            LastRedirectUri = redirectUri;
            LastScope = scope;
            LastState = state;
            return "https://accounts.google.com/o/oauth2/v2/auth?state=" + state;
        }

        public Task<GoogleOAuthTokenResult> ExchangeCodeForTokenAsync(
            string clientId, string clientSecret, string redirectUri, string code, CancellationToken cancellationToken = default)
        {
            LastClientId = clientId;
            LastClientSecret = clientSecret;
            LastRedirectUri = redirectUri;
            LastCode = code;
            return Task.FromResult(new GoogleOAuthTokenResult("access-token", "refresh-token", DateTimeOffset.UtcNow.AddHours(1)));
        }

        public Task<GoogleOAuthTokenResult> RefreshAccessTokenAsync(
            string clientId, string clientSecret, string refreshToken, CancellationToken cancellationToken = default)
        {
            LastClientId = clientId;
            LastClientSecret = clientSecret;
            if (RefreshThrowsRevoked)
            {
                throw new GoogleTokenRevokedException("revoked", new InvalidOperationException());
            }

            return Task.FromResult(new GoogleOAuthTokenResult("refreshed-access-token", null, DateTimeOffset.UtcNow.AddHours(1)));
        }

        public Task RevokeTokenAsync(string token, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task<string> GetAccountEmailAsync(string accessToken, CancellationToken cancellationToken = default) =>
            Task.FromResult("user@gmail.com");

        public Task<string> CreateCalendarAsync(string accessToken, string calendarName, CancellationToken cancellationToken = default) =>
            Task.FromResult("calendar-id");

        public Task DeleteCalendarAsync(string accessToken, string calendarId, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public bool EventCallsThrowNotFound { get; set; }
        public string CreatedEventId { get; set; } = "event-id";
        public string? LastCalendarId { get; private set; }
        public string? LastEventId { get; private set; }
        public string? LastTitle { get; private set; }
        public string? LastDescription { get; private set; }
        public DateOnly? LastDate { get; private set; }

        public Task<string> CreateEventAsync(
            string accessToken, string calendarId, string title, string description, DateOnly date, CancellationToken cancellationToken = default)
        {
            LastCalendarId = calendarId;
            LastTitle = title;
            LastDescription = description;
            LastDate = date;
            if (EventCallsThrowNotFound)
            {
                throw new GoogleCalendarNotFoundException("not found", new InvalidOperationException());
            }

            return Task.FromResult(CreatedEventId);
        }

        public Task UpdateEventAsync(
            string accessToken, string calendarId, string eventId, string title, string description, DateOnly date, CancellationToken cancellationToken = default)
        {
            LastCalendarId = calendarId;
            LastEventId = eventId;
            LastTitle = title;
            LastDescription = description;
            LastDate = date;
            if (EventCallsThrowNotFound)
            {
                throw new GoogleCalendarNotFoundException("not found", new InvalidOperationException());
            }

            return Task.CompletedTask;
        }

        public Task DeleteEventAsync(string accessToken, string calendarId, string eventId, CancellationToken cancellationToken = default)
        {
            LastCalendarId = calendarId;
            LastEventId = eventId;
            if (EventCallsThrowNotFound)
            {
                throw new GoogleCalendarNotFoundException("not found", new InvalidOperationException());
            }

            return Task.CompletedTask;
        }
    }
}
