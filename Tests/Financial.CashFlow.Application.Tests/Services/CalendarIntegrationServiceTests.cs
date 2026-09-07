using Financial.CashFlow.Application.Models;
using Financial.CashFlow.Application.Services;
using Financial.TestUtilities;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;

namespace Financial.CashFlow.Application.Tests.Services;

public class CalendarIntegrationServiceTests
{
    private static readonly Microsoft.Extensions.Logging.ILogger<CalendarIntegrationService> Logger =
        NullLogger<CalendarIntegrationService>.Instance;
    private static readonly DateTimeOffset Now = new(2026, 9, 7, 10, 0, 0, TimeSpan.Zero);

    private readonly FakeCalendarProvider _provider;
    private readonly FakeCalendarConnectionStore _store;
    private readonly RecordingTelemetryTracer _tracer;
    private readonly FakeTimeProvider _timeProvider;
    private readonly CalendarIntegrationService _sut;

    public CalendarIntegrationServiceTests()
    {
        _provider = new FakeCalendarProvider();
        _store = new FakeCalendarConnectionStore();
        _tracer = new RecordingTelemetryTracer();
        _timeProvider = new FakeTimeProvider(Now);
        _sut = new CalendarIntegrationService(_provider, _store, _tracer, Logger, _timeProvider);
    }

    [Fact]
    public void Constructor_WithNullProvider_Throws()
    {
        Action act = () => new CalendarIntegrationService(null!, _store, _tracer, Logger, _timeProvider);
        act.Should().Throw<ArgumentNullException>().WithParameterName("provider");
    }

    [Fact]
    public void Constructor_WithNullStore_Throws()
    {
        Action act = () => new CalendarIntegrationService(_provider, null!, _tracer, Logger, _timeProvider);
        act.Should().Throw<ArgumentNullException>().WithParameterName("store");
    }

    [Fact]
    public void Constructor_WithNullTracer_Throws()
    {
        Action act = () => new CalendarIntegrationService(_provider, _store, null!, Logger, _timeProvider);
        act.Should().Throw<ArgumentNullException>().WithParameterName("tracer");
    }

    [Fact]
    public void Constructor_WithNullLogger_Throws()
    {
        Action act = () => new CalendarIntegrationService(_provider, _store, _tracer, null!, _timeProvider);
        act.Should().Throw<ArgumentNullException>().WithParameterName("logger");
    }

    [Fact]
    public void Constructor_WithNullTimeProvider_Throws()
    {
        Action act = () => new CalendarIntegrationService(_provider, _store, _tracer, Logger, null!);
        act.Should().Throw<ArgumentNullException>().WithParameterName("timeProvider");
    }

    [Fact]
    public void BuildAuthorizationUrl_ReturnsTheProvidersUrl_AndIssuesAPendingState()
    {
        var url = _sut.BuildAuthorizationUrl();

        url.Should().Be(_provider.AuthorizationUrl);
        _provider.LastState.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task CompleteConnectionAsync_WithValidCodeAndState_CreatesCalendarAndPersistsConnection()
    {
        _sut.BuildAuthorizationUrl();
        _provider.ExchangeResult = new CalendarTokenResult("new-access", "new-refresh", Now.AddHours(1));
        _provider.AccountEmail = "user@gmail.com";
        _provider.CreatedCalendarId = "cal-1";

        var result = await _sut.CompleteConnectionAsync(code: "auth-code", state: _provider.LastState, error: null);

        result.Success.Should().BeTrue();
        var connection = _store.Load();
        connection.Should().NotBeNull();
        connection!.AccountEmail.Should().Be("user@gmail.com");
        connection.CalendarId.Should().Be("cal-1");
        connection.AccessToken.Should().Be("new-access");
        connection.RefreshToken.Should().Be("new-refresh");
        connection.ConnectedAtUtc.Should().Be(Now);
    }

    [Fact]
    public async Task CompleteConnectionAsync_WithErrorParameter_DoesNotExchangeCodeOrPersist()
    {
        _sut.BuildAuthorizationUrl();

        var result = await _sut.CompleteConnectionAsync(code: null, state: _provider.LastState, error: "access_denied");

        result.Success.Should().BeFalse();
        _provider.ExchangeCallCount.Should().Be(0);
        _store.Load().Should().BeNull();
    }

    [Fact]
    public async Task CompleteConnectionAsync_WithMismatchedState_DoesNotExchangeCode()
    {
        _sut.BuildAuthorizationUrl();

        var result = await _sut.CompleteConnectionAsync(code: "auth-code", state: "wrong-state", error: null);

        result.Success.Should().BeFalse();
        _provider.ExchangeCallCount.Should().Be(0);
        _store.Load().Should().BeNull();
    }

    [Fact]
    public async Task CompleteConnectionAsync_WhenCalendarCreationFails_RevokesTheJustIssuedTokenAndPersistsNothing()
    {
        _sut.BuildAuthorizationUrl();
        _provider.ExchangeResult = new CalendarTokenResult("new-access", "new-refresh", Now.AddHours(1));
        _provider.CreateCalendarThrows = true;

        var result = await _sut.CompleteConnectionAsync(code: "auth-code", state: _provider.LastState, error: null);

        result.Success.Should().BeFalse();
        _provider.RevokeCallCount.Should().Be(1);
        _provider.RevokedTokens.Should().ContainSingle().Which.Should().Be("new-access");
        _store.Load().Should().BeNull();
    }

    [Fact]
    public async Task CompleteConnectionAsync_WhenAlreadyConnected_DisconnectsThePreviousConnectionFirst()
    {
        var previous = new CalendarConnection("old@gmail.com", "old-cal", "old-access", "old-refresh", Now.AddHours(1), Now.AddDays(-1), new Dictionary<Guid, string>());
        _store.Save(previous);

        _sut.BuildAuthorizationUrl();
        _provider.ExchangeResult = new CalendarTokenResult("new-access", "new-refresh", Now.AddHours(1));
        _provider.AccountEmail = "new@gmail.com";
        _provider.CreatedCalendarId = "new-cal";

        var result = await _sut.CompleteConnectionAsync(code: "auth-code", state: _provider.LastState, error: null);

        result.Success.Should().BeTrue();
        _provider.DeletedCalendars.Should().ContainSingle(c => c.AccessToken == "old-access" && c.CalendarId == "old-cal");
        _provider.RevokedTokens.Should().Contain("old-access");
        var connection = _store.Load();
        connection!.AccountEmail.Should().Be("new@gmail.com");
        connection.CalendarId.Should().Be("new-cal");
    }

    [Fact]
    public async Task GetStatusAsync_WhenNotConnected_ReturnsConnectedFalse()
    {
        var status = await _sut.GetStatusAsync();

        status.Connected.Should().BeFalse();
        status.AccountEmail.Should().BeNull();
    }

    [Fact]
    public async Task GetStatusAsync_WhenAccessTokenExpired_RefreshesTransparently()
    {
        var connection = new CalendarConnection("user@gmail.com", "cal-1", "old-access", "refresh-token", Now.AddMinutes(-1), Now.AddDays(-1), new Dictionary<Guid, string>());
        _store.Save(connection);
        _provider.RefreshResult = new CalendarTokenResult("refreshed-access", null, Now.AddHours(1));

        var status = await _sut.GetStatusAsync();

        status.Connected.Should().BeTrue();
        status.AccountEmail.Should().Be("user@gmail.com");
        status.CalendarId.Should().Be("cal-1");
        _provider.RefreshCallCount.Should().Be(1);
        _store.Load()!.AccessToken.Should().Be("refreshed-access");
    }

    [Fact]
    public async Task GetStatusAsync_WhenRefreshTokenIsRevoked_ReportsTokenRevoked_AndDoesNotRetryOnNextCall()
    {
        var connection = new CalendarConnection("user@gmail.com", "cal-1", "old-access", "refresh-token", Now.AddMinutes(-1), Now.AddDays(-1), new Dictionary<Guid, string>());
        _store.Save(connection);
        _provider.RefreshThrowsRevoked = true;

        var firstStatus = await _sut.GetStatusAsync();
        var secondStatus = await _sut.GetStatusAsync();

        firstStatus.Connected.Should().BeFalse();
        firstStatus.DisconnectReason.Should().Be("token_revoked");
        secondStatus.Connected.Should().BeFalse();
        secondStatus.DisconnectReason.Should().Be("token_revoked");
        _provider.RefreshCallCount.Should().Be(1);
    }

    [Fact]
    public async Task DisconnectAsync_WhenConnected_DeletesCalendarRevokesTokenAndClearsLocalState()
    {
        var connection = new CalendarConnection("user@gmail.com", "cal-1", "access-token", "refresh-token", Now.AddHours(1), Now.AddDays(-1), new Dictionary<Guid, string>());
        _store.Save(connection);

        var result = await _sut.DisconnectAsync();

        result.RemoteCleanupSucceeded.Should().BeTrue();
        _provider.DeletedCalendars.Should().ContainSingle(c => c.AccessToken == "access-token" && c.CalendarId == "cal-1");
        _provider.RevokeCallCount.Should().Be(1);
        _store.Load().Should().BeNull();
    }

    [Fact]
    public async Task DisconnectAsync_WhenRemoteCallsFail_StillClearsLocalState()
    {
        var connection = new CalendarConnection("user@gmail.com", "cal-1", "access-token", "refresh-token", Now.AddHours(1), Now.AddDays(-1), new Dictionary<Guid, string>());
        _store.Save(connection);
        _provider.DeleteCalendarThrows = true;
        _provider.RevokeThrows = true;

        var result = await _sut.DisconnectAsync();

        result.RemoteCleanupSucceeded.Should().BeFalse();
        _store.Load().Should().BeNull();
        _store.DeleteCallCount.Should().Be(1);
    }

    [Fact]
    public async Task DisconnectAsync_WhenNothingIsConnected_IsIdempotent()
    {
        var result = await _sut.DisconnectAsync();

        result.RemoteCleanupSucceeded.Should().BeTrue();
        _provider.RevokeCallCount.Should().Be(0);
    }

    [Fact]
    public async Task GetValidAccessTokenAsync_WhenNotConnected_ReturnsNull()
    {
        var token = await _sut.GetValidAccessTokenAsync();

        token.Should().BeNull();
    }

    [Fact]
    public async Task GetValidAccessTokenAsync_WhenTokenStillValid_ReturnsItWithoutRefreshing()
    {
        var connection = new CalendarConnection("user@gmail.com", "cal-1", "access-token", "refresh-token", Now.AddHours(1), Now.AddDays(-1), new Dictionary<Guid, string>());
        _store.Save(connection);

        var token = await _sut.GetValidAccessTokenAsync();

        token.Should().Be("access-token");
        _provider.RefreshCallCount.Should().Be(0);
    }

    [Fact]
    public async Task GetValidAccessTokenAsync_WhenTokenExpired_RefreshesAndReturnsTheNewToken()
    {
        var connection = new CalendarConnection("user@gmail.com", "cal-1", "old-access", "refresh-token", Now.AddMinutes(-1), Now.AddDays(-1), new Dictionary<Guid, string>());
        _store.Save(connection);
        _provider.RefreshResult = new CalendarTokenResult("refreshed-access", null, Now.AddHours(1));

        var token = await _sut.GetValidAccessTokenAsync();

        token.Should().Be("refreshed-access");
        _provider.RefreshCallCount.Should().Be(1);
    }

    [Fact]
    public async Task GetValidAccessTokenAsync_WhenConnectionIsKnownRevoked_ReturnsNullWithoutRetryingRefresh()
    {
        var connection = new CalendarConnection("user@gmail.com", "cal-1", "old-access", "refresh-token", Now.AddHours(1), Now.AddDays(-1), new Dictionary<Guid, string>())
        {
            RevokedReason = "token_revoked"
        };
        _store.Save(connection);

        var token = await _sut.GetValidAccessTokenAsync();

        token.Should().BeNull();
        _provider.RefreshCallCount.Should().Be(0);
    }
}
