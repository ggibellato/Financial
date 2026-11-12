using System.Net;
using System.Net.Http.Json;
using Financial.CashFlow.Application.Interfaces;
using Financial.Shared.Abstractions.Currencies;
using FluentAssertions;
using FluentAssertions.Execution;
using Microsoft.AspNetCore.Mvc;
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
    protected const string Api = "/api/v1/financial";

    private readonly ApiTestFactory _factory;
    private HttpClient? _client;

    protected ApiEndpointTests(
        IExchangeRateProvider? exchangeRateProvider = null,
        TimeProvider? timeProvider = null,
        ICalendarProvider? calendarProvider = null,
        bool useRealExchangeRates = false)
    {
        _factory = new ApiTestFactory(exchangeRateProvider, timeProvider, calendarProvider, useRealExchangeRates);
    }

    /// <summary>The API client for this test's own host, created on first use so a test that builds
    /// its own factory instead does not pay for booting a second one.</summary>
    protected HttpClient Client => _client ??= _factory.CreateClient();

    /// <summary>A client on this same test's host with a chosen redirect behavior - e.g. for a
    /// test asserting on a raw 302 rather than following it.</summary>
    protected HttpClient CreateClient(WebApplicationFactoryClientOptions options) => _factory.CreateClient(options);

    /// <summary>The host's service provider, for the few tests that assert on resolved services.</summary>
    protected IServiceProvider Services => _factory.Services;

    protected void ReplaceCashFlowDataBeforeStart(string json) => _factory.ReplaceCashFlowData(json);

    protected string SavedCashFlowData() => _factory.ReadCashFlowData();

    protected static async Task<string?> ProblemDetailAsync(HttpResponseMessage response) =>
        (await response.Content.ReadFromJsonAsync<ProblemDetails>())!.Detail;

    protected static async Task ShouldBeProblemAsync(HttpResponseMessage response, HttpStatusCode status, string detail)
    {
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();

        using (new AssertionScope())
        {
            response.StatusCode.Should().Be(status);
            problem!.Detail.Should().Be(detail);
        }
    }

    public virtual Task InitializeAsync() => Task.CompletedTask;

    public virtual async Task DisposeAsync()
    {
        _client?.Dispose();
        await _factory.DisposeAsync();
    }
}
