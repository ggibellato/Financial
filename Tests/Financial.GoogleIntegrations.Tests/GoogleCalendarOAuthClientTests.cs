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
    public void BuildEvent_SpansTenToElevenInTheGivenTimeZone_OnTheGivenDate()
    {
        var brazil = TimeZoneInfo.CreateCustomTimeZone("brazil", TimeSpan.FromHours(-3), "brazil", "brazil");

        var calendarEvent = GoogleCalendarOAuthClient.BuildEvent("title", "description", new DateOnly(2026, 9, 10), brazil);

        calendarEvent.Start.DateTimeDateTimeOffset.Should().Be(new DateTimeOffset(2026, 9, 10, 10, 0, 0, TimeSpan.FromHours(-3)));
        calendarEvent.End.DateTimeDateTimeOffset.Should().Be(new DateTimeOffset(2026, 9, 10, 11, 0, 0, TimeSpan.FromHours(-3)));
        calendarEvent.Start.DateTimeDateTimeOffset!.Value.UtcDateTime.Hour.Should().Be(13);
        calendarEvent.Summary.Should().Be("title");
        calendarEvent.Description.Should().Be("description");
    }

    [Fact]
    [Trait("AC", "P45-F02-credit-card-due-date-event-sync-04")]
    public void BuildEvent_HasExactlyTwoPopupReminders_OneDayAndOneHourBeforeItsStart()
    {
        var calendarEvent = GoogleCalendarOAuthClient.BuildEvent("title", "description", new DateOnly(2026, 9, 10), TimeZoneInfo.Utc);

        calendarEvent.Reminders.UseDefault.Should().BeFalse();
        calendarEvent.Reminders.Overrides.Should().HaveCount(2);
        calendarEvent.Reminders.Overrides.Should().OnlyContain(r => r.Method == "popup");
        calendarEvent.Reminders.Overrides.Select(r => r.Minutes).Should().BeEquivalentTo(new int?[] { 1440, 60 });
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
