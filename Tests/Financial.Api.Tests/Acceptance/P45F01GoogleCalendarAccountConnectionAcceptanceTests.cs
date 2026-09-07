using System.Net;
using System.Net.Http.Json;
using Financial.CashFlow.Application.DTOs;
using Financial.CashFlow.Application.Models;
using Financial.TestUtilities;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Financial.Api.Tests.Acceptance;

public class P45F01GoogleCalendarAccountConnectionAcceptanceTests : ApiEndpointTests
{
    private const string BaseRoute = "/api/v1/financial/integrations/google-calendar";
    private static readonly DateTimeOffset Now = new(2026, 9, 7, 12, 0, 0, TimeSpan.Zero);

    private readonly FakeGoogleCalendarClient _client;

    public P45F01GoogleCalendarAccountConnectionAcceptanceTests() : this(new FakeGoogleCalendarClient())
    {
    }

    private P45F01GoogleCalendarAccountConnectionAcceptanceTests(FakeGoogleCalendarClient client)
        : base(timeProvider: new FakeTimeProvider(Now), googleCalendarClient: client)
    {
        _client = client;
    }

    [Fact]
    [Trait("AC", "P45-F01-google-calendar-account-connection-01")]
    public async Task CompletingConsent_ResultsInStatusReportingConnectedWithAccountEmailAndCalendar()
    {
        var state = await BeginConnectAndCaptureStateAsync();
        _client.AccountEmail = "user@gmail.com";
        _client.CreatedCalendarId = "cal-1";

        var callback = await Client.GetAsync($"{BaseRoute}/callback?code=auth-code&state={Uri.EscapeDataString(state)}");
        callback.EnsureSuccessStatusCode();

        var status = await Client.GetFromJsonAsync<GoogleCalendarConnectionStatusDTO>($"{BaseRoute}/status");

        status!.Connected.Should().BeTrue();
        status.AccountEmail.Should().Be("user@gmail.com");
        status.CalendarName.Should().Be("Financial - Credit Card Due Dates");
    }

    [Fact]
    [Trait("AC", "P45-F01-google-calendar-account-connection-02")]
    public async Task DecliningConsent_LeavesStatusDisconnected_AndPersistsNoCredentials()
    {
        await BeginConnectAndCaptureStateAsync();

        var callback = await Client.GetAsync($"{BaseRoute}/callback?error=access_denied");
        callback.EnsureSuccessStatusCode();

        var status = await Client.GetFromJsonAsync<GoogleCalendarConnectionStatusDTO>($"{BaseRoute}/status");

        status!.Connected.Should().BeFalse();
        _client.ExchangeCallCount.Should().Be(0);
    }

    [Fact]
    [Trait("AC", "P45-F01-google-calendar-account-connection-03")]
    public async Task Disconnecting_DeletesCalendarRevokesTokenAndClearsCredentials_AndStatusReportsDisconnected()
    {
        var state = await BeginConnectAndCaptureStateAsync();
        await Client.GetAsync($"{BaseRoute}/callback?code=auth-code&state={Uri.EscapeDataString(state)}");

        var disconnectResponse = await Client.PostAsync($"{BaseRoute}/disconnect", content: null);
        var result = await disconnectResponse.Content.ReadFromJsonAsync<GoogleCalendarDisconnectResultDTO>();

        result!.RemoteCleanupSucceeded.Should().BeTrue();
        _client.DeletedCalendars.Should().ContainSingle();
        _client.RevokeCallCount.Should().Be(1);

        var status = await Client.GetFromJsonAsync<GoogleCalendarConnectionStatusDTO>($"{BaseRoute}/status");
        status!.Connected.Should().BeFalse();
    }

    [Fact]
    [Trait("AC", "P45-F01-google-calendar-account-connection-04")]
    public async Task ConnectingANewAccount_DeletesThePreviousDedicatedCalendarFirst()
    {
        var firstState = await BeginConnectAndCaptureStateAsync();
        _client.ExchangeResult = new GoogleCalendarTokenResult("first-access", "first-refresh", Now.AddHours(1));
        _client.CreatedCalendarId = "first-cal";
        await Client.GetAsync($"{BaseRoute}/callback?code=code1&state={Uri.EscapeDataString(firstState)}");

        var secondState = await BeginConnectAndCaptureStateAsync();
        _client.ExchangeResult = new GoogleCalendarTokenResult("second-access", "second-refresh", Now.AddHours(1));
        _client.CreatedCalendarId = "second-cal";
        await Client.GetAsync($"{BaseRoute}/callback?code=code2&state={Uri.EscapeDataString(secondState)}");

        _client.DeletedCalendars.Should().ContainSingle(c => c.AccessToken == "first-access" && c.CalendarId == "first-cal");
        var status = await Client.GetFromJsonAsync<GoogleCalendarConnectionStatusDTO>($"{BaseRoute}/status");
        status!.Connected.Should().BeTrue();
    }

    [Fact]
    [Trait("AC", "P45-F01-google-calendar-account-connection-05")]
    public async Task ExpiredAccessToken_IsTransparentlyRefreshedOnTheNextApiCall()
    {
        var state = await BeginConnectAndCaptureStateAsync();
        _client.ExchangeResult = new GoogleCalendarTokenResult("access-1", "refresh-1", Now.AddMinutes(-1));
        await Client.GetAsync($"{BaseRoute}/callback?code=code1&state={Uri.EscapeDataString(state)}");
        _client.RefreshResult = new GoogleCalendarTokenResult("refreshed-access", null, Now.AddHours(1));

        var status = await Client.GetFromJsonAsync<GoogleCalendarConnectionStatusDTO>($"{BaseRoute}/status");

        status!.Connected.Should().BeTrue();
        _client.RefreshCallCount.Should().Be(1);
    }

    [Fact]
    [Trait("AC", "P45-F01-google-calendar-account-connection-06")]
    public async Task RevokedRefreshToken_ReportsTokenRevoked_WithoutCrashingOrRepeatedlyRetrying()
    {
        var state = await BeginConnectAndCaptureStateAsync();
        _client.ExchangeResult = new GoogleCalendarTokenResult("access-1", "refresh-1", Now.AddMinutes(-1));
        await Client.GetAsync($"{BaseRoute}/callback?code=code1&state={Uri.EscapeDataString(state)}");
        _client.RefreshThrowsRevoked = true;

        var firstStatus = await Client.GetFromJsonAsync<GoogleCalendarConnectionStatusDTO>($"{BaseRoute}/status");
        var secondStatus = await Client.GetFromJsonAsync<GoogleCalendarConnectionStatusDTO>($"{BaseRoute}/status");

        firstStatus!.Connected.Should().BeFalse();
        firstStatus.DisconnectReason.Should().Be("token_revoked");
        secondStatus!.Connected.Should().BeFalse();
        _client.RefreshCallCount.Should().Be(1);
    }

    private async Task<string> BeginConnectAndCaptureStateAsync()
    {
        using var noRedirectClient = CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var response = await noRedirectClient.GetAsync($"{BaseRoute}/connect");
        response.StatusCode.Should().Be(HttpStatusCode.Redirect);
        return _client.LastState!;
    }
}
