using Financial.CashFlow.Application.Interfaces;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Financial.Api.Tests;

/// <summary>
/// Base for endpoint tests that exercise the API over its own host. xUnit builds one instance per
/// test, so every test still gets an isolated factory, host and temp data file - the same isolation
/// the per-test <c>await using var factory = new ApiTestFactory()</c> used to give, without each
/// test repeating it.
/// </summary>
public abstract class ApiEndpointTests : IAsyncLifetime
{
    private readonly ApiTestFactory _factory;
    private HttpClient? _client;

    protected ApiEndpointTests(
        IExchangeRateProvider? exchangeRateProvider = null,
        TimeProvider? timeProvider = null,
        ICalendarProvider? calendarProvider = null)
    {
        _factory = new ApiTestFactory(exchangeRateProvider, timeProvider, calendarProvider);
    }

    /// <summary>The API client for this test's own host, created on first use so a test that builds
    /// its own factory instead does not pay for booting a second one.</summary>
    protected HttpClient Client => _client ??= _factory.CreateClient();

    /// <summary>A client on this same test's host with a chosen redirect behavior - e.g. for a
    /// test asserting on a raw 302 rather than following it.</summary>
    protected HttpClient CreateClient(WebApplicationFactoryClientOptions options) => _factory.CreateClient(options);

    /// <summary>The host's service provider, for the few tests that assert on resolved services.</summary>
    protected IServiceProvider Services => _factory.Services;

    public virtual Task InitializeAsync() => Task.CompletedTask;

    public virtual async Task DisposeAsync()
    {
        _client?.Dispose();
        await _factory.DisposeAsync();
    }
}
