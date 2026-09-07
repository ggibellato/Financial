using Financial.Integrations.GoogleCalendar;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;

namespace Financial.GoogleIntegrations.Tests;

public class GoogleCalendarOAuthClientTests
{
    private static IGoogleCalendarOAuthClient CreateClient()
    {
        var services = new ServiceCollection();
        services.AddGoogleCalendarOAuthClient();
        return services.BuildServiceProvider().GetRequiredService<IGoogleCalendarOAuthClient>();
    }

    [Fact]
    public void BuildAuthorizationUrl_IncludesClientIdRedirectUriScopeAndState()
    {
        var client = CreateClient();

        var url = client.BuildAuthorizationUrl(
            clientId: "client-123",
            redirectUri: "https://example.test/callback",
            scope: "https://www.googleapis.com/auth/calendar",
            state: "state-abc");

        url.Should().StartWith("https://accounts.google.com/o/oauth2/v2/auth?");
        url.Should().Contain("client_id=client-123");
        url.Should().Contain($"redirect_uri={Uri.EscapeDataString("https://example.test/callback")}");
        url.Should().Contain($"scope={Uri.EscapeDataString("https://www.googleapis.com/auth/calendar")}");
        url.Should().Contain("state=state-abc");
        url.Should().Contain("response_type=code");
    }

    [Fact]
    public void BuildAuthorizationUrl_RequestsOfflineAccessAndForcesConsent()
    {
        var client = CreateClient();

        var url = client.BuildAuthorizationUrl("client-123", "https://example.test/callback", "scope", "state");

        url.Should().Contain("access_type=offline");
        url.Should().Contain("prompt=consent");
    }

    [Fact]
    public void BuildEvent_IsAllDay_OnTheGivenDate()
    {
        var calendarEvent = GoogleCalendarOAuthClient.BuildEvent("title", "description", new DateOnly(2026, 9, 10));

        calendarEvent.Start.Date.Should().Be("2026-09-10");
        calendarEvent.End.Date.Should().Be("2026-09-11");
        calendarEvent.Summary.Should().Be("title");
        calendarEvent.Description.Should().Be("description");
    }

    [Fact]
    [Trait("AC", "P45-F02-credit-card-due-date-event-sync-04")]
    public void BuildEvent_HasExactlyOnePopupReminderSet1440MinutesBeforeItsStart()
    {
        var calendarEvent = GoogleCalendarOAuthClient.BuildEvent("title", "description", new DateOnly(2026, 9, 10));

        calendarEvent.Reminders.UseDefault.Should().BeFalse();
        calendarEvent.Reminders.Overrides.Should().ContainSingle();
        calendarEvent.Reminders.Overrides[0].Method.Should().Be("popup");
        calendarEvent.Reminders.Overrides[0].Minutes.Should().Be(1440);
    }
}
