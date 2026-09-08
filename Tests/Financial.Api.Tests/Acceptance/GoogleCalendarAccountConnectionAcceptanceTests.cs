using System.Net;
using System.Net.Http.Json;
using Financial.CashFlow.Application.DTOs;
using Financial.CashFlow.Application.Models;
using Financial.TestUtilities;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Financial.Api.Tests.Acceptance;

public class GoogleCalendarAccountConnectionAcceptanceTests : ApiEndpointTests
{
    private const string BaseRoute = "/api/v1/financial/integrations/calendar";
    private static readonly DateTimeOffset Now = new(2026, 9, 7, 12, 0, 0, TimeSpan.Zero);

    private readonly FakeCalendarProvider _provider;

    public GoogleCalendarAccountConnectionAcceptanceTests() : this(new FakeCalendarProvider())
    {
    }

    private GoogleCalendarAccountConnectionAcceptanceTests(FakeCalendarProvider provider)
        : base(timeProvider: new FakeTimeProvider(Now), calendarProvider: provider)
    {
        _provider = provider;
    }

    [Fact]
    [Trait("AC", "P45-F01-google-calendar-account-connection-01")]
    public async Task CompletingConsent_ResultsInStatusReportingConnectedWithAccountEmailAndCalendar()
    {
        var state = await BeginConnectAndCaptureStateAsync();
        _provider.AccountEmail = "user@gmail.com";
        _provider.CreatedCalendarId = "cal-1";

        var callback = await Client.GetAsync($"{BaseRoute}/callback?code=auth-code&state={Uri.EscapeDataString(state)}");
        callback.EnsureSuccessStatusCode();

        var status = await Client.GetFromJsonAsync<CalendarConnectionStatusDTO>($"{BaseRoute}/status");

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

        var status = await Client.GetFromJsonAsync<CalendarConnectionStatusDTO>($"{BaseRoute}/status");

        status!.Connected.Should().BeFalse();
        _provider.ExchangeCallCount.Should().Be(0);
    }

    [Fact]
    [Trait("AC", "P45-F01-google-calendar-account-connection-03")]
    public async Task Disconnecting_RevokesTokenAndClearsCredentials_LeavesTheCalendarIntact_AndStatusReportsDisconnected()
    {
        var state = await BeginConnectAndCaptureStateAsync();
        await Client.GetAsync($"{BaseRoute}/callback?code=auth-code&state={Uri.EscapeDataString(state)}");

        var disconnectResponse = await Client.PostAsync($"{BaseRoute}/disconnect", content: null);
        var result = await disconnectResponse.Content.ReadFromJsonAsync<CalendarDisconnectResultDTO>();

        result!.RemoteCleanupSucceeded.Should().BeTrue();
        _provider.DeletedCalendars.Should().BeEmpty();
        _provider.RevokeCallCount.Should().Be(1);

        var status = await Client.GetFromJsonAsync<CalendarConnectionStatusDTO>($"{BaseRoute}/status");
        status!.Connected.Should().BeFalse();
    }

    [Fact]
    [Trait("AC", "P45-F01-google-calendar-account-connection-04")]
    public async Task ConnectingANewAccount_DeletesThePreviousDedicatedCalendarFirst()
    {
        var firstState = await BeginConnectAndCaptureStateAsync();
        _provider.ExchangeResult = new CalendarTokenResult("first-access", "first-refresh", Now.AddHours(1));
        _provider.CreatedCalendarId = "first-cal";
        await Client.GetAsync($"{BaseRoute}/callback?code=code1&state={Uri.EscapeDataString(firstState)}");

        var secondState = await BeginConnectAndCaptureStateAsync();
        _provider.ExchangeResult = new CalendarTokenResult("second-access", "second-refresh", Now.AddHours(1));
        _provider.CreatedCalendarId = "second-cal";
        await Client.GetAsync($"{BaseRoute}/callback?code=code2&state={Uri.EscapeDataString(secondState)}");

        _provider.DeletedCalendars.Should().ContainSingle(c => c.AccessToken == "first-access" && c.CalendarId == "first-cal");
        var status = await Client.GetFromJsonAsync<CalendarConnectionStatusDTO>($"{BaseRoute}/status");
        status!.Connected.Should().BeTrue();
    }

    [Fact]
    [Trait("AC", "P45-F01-google-calendar-account-connection-05")]
    public async Task ExpiredAccessToken_IsTransparentlyRefreshedOnTheNextApiCall()
    {
        var state = await BeginConnectAndCaptureStateAsync();
        _provider.ExchangeResult = new CalendarTokenResult("access-1", "refresh-1", Now.AddMinutes(-1));
        await Client.GetAsync($"{BaseRoute}/callback?code=code1&state={Uri.EscapeDataString(state)}");
        _provider.RefreshResult = new CalendarTokenResult("refreshed-access", null, Now.AddHours(1));

        var status = await Client.GetFromJsonAsync<CalendarConnectionStatusDTO>($"{BaseRoute}/status");

        status!.Connected.Should().BeTrue();
        _provider.RefreshCallCount.Should().Be(1);
    }

    [Fact]
    [Trait("AC", "P45-F01-google-calendar-account-connection-06")]
    public async Task RevokedRefreshToken_ReportsTokenRevoked_WithoutCrashingOrRepeatedlyRetrying()
    {
        var state = await BeginConnectAndCaptureStateAsync();
        _provider.ExchangeResult = new CalendarTokenResult("access-1", "refresh-1", Now.AddMinutes(-1));
        await Client.GetAsync($"{BaseRoute}/callback?code=code1&state={Uri.EscapeDataString(state)}");
        _provider.RefreshThrowsRevoked = true;

        var firstStatus = await Client.GetFromJsonAsync<CalendarConnectionStatusDTO>($"{BaseRoute}/status");
        var secondStatus = await Client.GetFromJsonAsync<CalendarConnectionStatusDTO>($"{BaseRoute}/status");

        firstStatus!.Connected.Should().BeFalse();
        firstStatus.DisconnectReason.Should().Be("token_revoked");
        secondStatus!.Connected.Should().BeFalse();
        _provider.RefreshCallCount.Should().Be(1);
    }

    private async Task<string> BeginConnectAndCaptureStateAsync()
    {
        using var noRedirectClient = CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var response = await noRedirectClient.GetAsync($"{BaseRoute}/connect");
        response.StatusCode.Should().Be(HttpStatusCode.Redirect);
        return _provider.LastState!;
    }
}
