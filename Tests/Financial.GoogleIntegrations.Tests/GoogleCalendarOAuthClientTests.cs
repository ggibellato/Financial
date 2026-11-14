using System.Net;
using Financial.Integrations.GoogleCalendar;
using FluentAssertions;
using FluentAssertions.Execution;
using Google.Apis.Auth.OAuth2.Responses;
using Microsoft.Extensions.DependencyInjection;

namespace Financial.GoogleIntegrations.Tests;

[Trait("Category", "Unit")]
public class GoogleCalendarOAuthClientTests
{
    private const string TokenEndpointPath = "/token";
    private const string TokenJson = """{"access_token":"access-1","refresh_token":"refresh-1","expires_in":3600,"token_type":"Bearer"}""";

    private static IGoogleCalendarOAuthClient CreateClient()
    {
        var services = new ServiceCollection();
        services.AddGoogleCalendarOAuthClient();
        return services.BuildServiceProvider().GetRequiredService<IGoogleCalendarOAuthClient>();
    }

    private static (GoogleCalendarOAuthClient Client, FakeGoogleApiHandler Handler) CreateClientWithHandler(
        Func<RecordedRequest, HttpResponseMessage> respond)
    {
        var handler = new FakeGoogleApiHandler(respond);
        return (new GoogleCalendarOAuthClient(handler.CreateHttpClient(), handler.SdkHttpClientFactory), handler);
    }

    private static HttpResponseMessage TokenError(string error) =>
        FakeGoogleApiHandler.Json(HttpStatusCode.BadRequest, $$"""{"error":"{{error}}","error_description":"rejected"}""");

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
    public async Task ExchangeCodeForTokenAsync_PostsTheCodeAndReturnsBothTokens()
    {
        var (client, handler) = CreateClientWithHandler(_ => FakeGoogleApiHandler.Json(HttpStatusCode.OK, TokenJson));

        var result = await client.ExchangeCodeForTokenAsync("client-123", "secret-xyz", "https://example.test/callback", "code-abc");

        var request = handler.Requests.Single();
        using (new AssertionScope())
        {
            result.AccessToken.Should().Be("access-1");
            result.RefreshToken.Should().Be("refresh-1");
            request.Method.Should().Be(HttpMethod.Post);
            request.Uri.AbsolutePath.Should().Be(TokenEndpointPath);
            request.Form("grant_type").Should().Be("authorization_code");
            request.Form("code").Should().Be("code-abc");
            request.Form("redirect_uri").Should().Be("https://example.test/callback");
            request.Form("client_id").Should().Be("client-123");
        }
    }

    [Fact]
    public async Task ExchangeCodeForTokenAsync_InvalidGrant_ThrowsTokenRevoked()
    {
        var (client, _) = CreateClientWithHandler(_ => TokenError("invalid_grant"));

        var act = () => client.ExchangeCodeForTokenAsync("client-123", "secret-xyz", "https://example.test/callback", "used-code");

        await act.Should().ThrowAsync<GoogleTokenRevokedException>();
    }

    [Fact]
    public async Task RefreshAccessTokenAsync_PostsTheRefreshTokenAndReturnsTheNewAccessToken()
    {
        var (client, handler) = CreateClientWithHandler(_ => FakeGoogleApiHandler.Json(HttpStatusCode.OK, TokenJson));

        var result = await client.RefreshAccessTokenAsync("client-123", "secret-xyz", "refresh-old");

        var request = handler.Requests.Single();
        result.AccessToken.Should().Be("access-1");
        request.Form("grant_type").Should().Be("refresh_token");
        request.Form("refresh_token").Should().Be("refresh-old");
    }

    [Fact]
    public async Task RefreshAccessTokenAsync_InvalidGrant_ThrowsTokenRevoked()
    {
        var (client, _) = CreateClientWithHandler(_ => TokenError("invalid_grant"));

        var act = () => client.RefreshAccessTokenAsync("client-123", "secret-xyz", "revoked-token");

        await act.Should().ThrowAsync<GoogleTokenRevokedException>();
    }

    [Fact]
    public async Task RefreshAccessTokenAsync_OtherTokenError_IsNotReportedAsRevoked()
    {
        var (client, _) = CreateClientWithHandler(_ => TokenError("invalid_client"));

        var act = () => client.RefreshAccessTokenAsync("client-123", "wrong-secret", "refresh-old");

        (await act.Should().ThrowAsync<TokenResponseException>()).Which.Error.Error.Should().Be("invalid_client");
    }

    [Fact]
    public async Task RevokeTokenAsync_PostsTheTokenToTheRevokeEndpoint()
    {
        var (client, handler) = CreateClientWithHandler(_ => new HttpResponseMessage(HttpStatusCode.OK));

        await client.RevokeTokenAsync("token-abc");

        var request = handler.Requests.Single();
        request.Method.Should().Be(HttpMethod.Post);
        request.Uri.Should().Be(new Uri("https://oauth2.googleapis.com/revoke"));
        request.Body.Should().Be("token=token-abc");
    }

    [Fact]
    public async Task RevokeTokenAsync_WithNonSuccessStatusCode_Throws()
    {
        var (client, _) = CreateClientWithHandler(_ => new HttpResponseMessage(HttpStatusCode.BadRequest));

        var act = () => client.RevokeTokenAsync("token-abc");

        await act.Should().ThrowAsync<HttpRequestException>();
    }

    [Fact]
    public async Task GetAccountEmailAsync_SendsTheAccessTokenAsABearerHeaderAndReturnsTheEmail()
    {
        var (client, handler) = CreateClientWithHandler(_ =>
            FakeGoogleApiHandler.Json(HttpStatusCode.OK, """{"email":"user@example.test"}"""));

        var email = await client.GetAccountEmailAsync("access-token-123");

        email.Should().Be("user@example.test");
        handler.Requests.Single().Authorization.Should().Be("Bearer access-token-123");
        handler.Requests.Single().Uri.Should().Be(new Uri("https://www.googleapis.com/oauth2/v2/userinfo"));
    }

    [Fact]
    public async Task GetAccountEmailAsync_WithNonSuccessStatusCode_Throws()
    {
        var (client, _) = CreateClientWithHandler(_ => new HttpResponseMessage(HttpStatusCode.Unauthorized));

        var act = () => client.GetAccountEmailAsync("access-token-123");

        await act.Should().ThrowAsync<HttpRequestException>();
    }

    [Fact]
    public async Task GetAccountEmailAsync_WithResponseMissingEmail_ThrowsInvalidOperationException()
    {
        var (client, _) = CreateClientWithHandler(_ => FakeGoogleApiHandler.Json(HttpStatusCode.OK, "{}"));

        var act = () => client.GetAccountEmailAsync("access-token-123");

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*did not include an email address*");
    }

    [Fact]
    public async Task CreateCalendarAsync_PostsTheNameWithTheAccessTokenAndReturnsTheNewId()
    {
        var (client, handler) = CreateClientWithHandler(_ =>
            FakeGoogleApiHandler.Json(HttpStatusCode.OK, """{"id":"cal-1","summary":"Financial"}"""));

        var id = await client.CreateCalendarAsync("access-token-123", "Financial");

        var request = handler.Requests.Single();
        id.Should().Be("cal-1");
        request.Method.Should().Be(HttpMethod.Post);
        request.Uri.AbsolutePath.Should().EndWith("/calendars");
        request.Body.Should().Contain("\"summary\":\"Financial\"");
        request.Authorization.Should().Be("Bearer access-token-123");
    }

    [Fact]
    public async Task FindCalendarIdByNameAsync_MatchOnALaterPage_FollowsThePageToken()
    {
        var (client, handler) = CreateClientWithHandler(request => request.Query("pageToken") == "page-2"
            ? FakeGoogleApiHandler.Json(HttpStatusCode.OK, """{"items":[{"id":"cal-2","summary":"Financial"}]}""")
            : FakeGoogleApiHandler.Json(HttpStatusCode.OK, """{"items":[{"id":"cal-1","summary":"Financial old"}],"nextPageToken":"page-2"}"""));

        var id = await client.FindCalendarIdByNameAsync("access-token-123", "Financial");

        id.Should().Be("cal-2", "only an exact name match counts, so the first page's near-miss is skipped");
        handler.Requests.Should().HaveCount(2);
    }

    [Fact]
    public async Task FindCalendarIdByNameAsync_NoMatchOnAnyPage_ReturnsNull()
    {
        var (client, _) = CreateClientWithHandler(_ =>
            FakeGoogleApiHandler.Json(HttpStatusCode.OK, """{"items":[{"id":"cal-1","summary":"Personal"}]}"""));

        var id = await client.FindCalendarIdByNameAsync("access-token-123", "Financial");

        id.Should().BeNull();
    }

    [Fact]
    public async Task FindEventIdByTitlePrefixAsync_ReturnsOnlyAnEventWhoseTitleStartsWithThePrefix()
    {
        var (client, handler) = CreateClientWithHandler(_ => FakeGoogleApiHandler.Json(HttpStatusCode.OK,
            """{"items":[{"id":"ev-1","summary":"Paid: Amex statement"},{"id":"ev-2","summary":"Amex statement due"}]}"""));

        var id = await client.FindEventIdByTitlePrefixAsync("access-token-123", "cal-1", "Amex statement");

        id.Should().Be("ev-2");
        handler.Requests.Single().Query("q").Should().Be("Amex statement");
    }

    [Fact]
    public async Task CreateEventAsync_PostsTheEventToTheCalendarAndReturnsItsId()
    {
        var (client, handler) = CreateClientWithHandler(_ =>
            FakeGoogleApiHandler.Json(HttpStatusCode.OK, """{"id":"ev-1"}"""));

        var id = await client.CreateEventAsync("access-token-123", "cal-1", "Amex statement due", "Pay it", new DateOnly(2026, 11, 20));

        var request = handler.Requests.Single();
        id.Should().Be("ev-1");
        request.Uri.AbsolutePath.Should().EndWith("/calendars/cal-1/events");
        request.Body.Should().Contain("\"summary\":\"Amex statement due\"");
    }

    [Fact]
    public async Task CreateEventAsync_CalendarNotFound_ThrowsCalendarNotFound()
    {
        var (client, _) = CreateClientWithHandler(_ => FakeGoogleApiHandler.Error(HttpStatusCode.NotFound));

        var act = () => client.CreateEventAsync("access-token-123", "deleted-cal", "title", "description", new DateOnly(2026, 11, 20));

        (await act.Should().ThrowAsync<GoogleCalendarNotFoundException>()).WithMessage("*deleted-cal*");
    }

    [Fact]
    public async Task DeleteEventAsync_OtherApiError_IsNotReportedAsCalendarNotFound()
    {
        var (client, _) = CreateClientWithHandler(_ => FakeGoogleApiHandler.Error(HttpStatusCode.Forbidden));

        var act = () => client.DeleteEventAsync("access-token-123", "cal-1", "ev-1");

        await act.Should().ThrowAsync<Google.GoogleApiException>();
    }
}
