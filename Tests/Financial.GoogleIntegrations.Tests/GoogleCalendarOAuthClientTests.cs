using System.Net;
using System.Net.Http.Headers;
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

    private static GoogleCalendarOAuthClient CreateClientWithHandler(Func<HttpRequestMessage, Task<HttpResponseMessage>> respond) =>
        new(new HttpClient(new FakeHttpMessageHandler(respond)));

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

    [Fact]
    public async Task RevokeTokenAsync_PostsTheTokenToTheRevokeEndpoint()
    {
        HttpRequestMessage? capturedRequest = null;
        string? capturedBody = null;
        var client = CreateClientWithHandler(async request =>
        {
            capturedRequest = request;
            capturedBody = request.Content is null ? null : await request.Content.ReadAsStringAsync();
            return new HttpResponseMessage(HttpStatusCode.OK);
        });

        await client.RevokeTokenAsync("token-abc");

        capturedRequest!.Method.Should().Be(HttpMethod.Post);
        capturedRequest.RequestUri.Should().Be(new Uri("https://oauth2.googleapis.com/revoke"));
        capturedBody.Should().Be("token=token-abc");
    }

    [Fact]
    public async Task RevokeTokenAsync_WithNonSuccessStatusCode_Throws()
    {
        var client = CreateClientWithHandler(_ => Task.FromResult(new HttpResponseMessage(HttpStatusCode.BadRequest)));

        var act = () => client.RevokeTokenAsync("token-abc");

        await act.Should().ThrowAsync<HttpRequestException>();
    }

    [Fact]
    public async Task GetAccountEmailAsync_WithSuccessfulResponse_ReturnsTheEmail()
    {
        var client = CreateClientWithHandler(_ => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("""{"email":"user@example.test"}""")
        }));

        var email = await client.GetAccountEmailAsync("access-token-123");

        email.Should().Be("user@example.test");
    }

    [Fact]
    public async Task GetAccountEmailAsync_SendsTheAccessTokenAsABearerHeader()
    {
        HttpRequestMessage? capturedRequest = null;
        var client = CreateClientWithHandler(request =>
        {
            capturedRequest = request;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("""{"email":"user@example.test"}""")
            });
        });

        await client.GetAccountEmailAsync("access-token-123");

        capturedRequest!.Headers.Authorization.Should().Be(new AuthenticationHeaderValue("Bearer", "access-token-123"));
        capturedRequest.RequestUri.Should().Be(new Uri("https://www.googleapis.com/oauth2/v2/userinfo"));
    }

    [Fact]
    public async Task GetAccountEmailAsync_WithNonSuccessStatusCode_Throws()
    {
        var client = CreateClientWithHandler(_ => Task.FromResult(new HttpResponseMessage(HttpStatusCode.Unauthorized)));

        var act = () => client.GetAccountEmailAsync("access-token-123");

        await act.Should().ThrowAsync<HttpRequestException>();
    }

    [Fact]
    public async Task GetAccountEmailAsync_WithResponseMissingEmail_ThrowsInvalidOperationException()
    {
        var client = CreateClientWithHandler(_ => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("{}")
        }));

        var act = () => client.GetAccountEmailAsync("access-token-123");

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*did not include an email address*");
    }

    private sealed class FakeHttpMessageHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, Task<HttpResponseMessage>> _responder;

        public FakeHttpMessageHandler(Func<HttpRequestMessage, Task<HttpResponseMessage>> responder)
        {
            _responder = responder;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            _responder(request);
    }
}
