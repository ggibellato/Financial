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
}
