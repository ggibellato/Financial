using Financial.Investment.Application.DTOs;
using Financial.Investment.Application.Interfaces;
using Financial.Api.Controllers;
using Financial.TestUtilities;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Mvc.Testing;
using System.Net;
using System.Text.Json;

namespace Financial.Api.Tests;

[Trait("Category", "Integration")]
public class DividendEndpointsTests
{
    [Fact]
    public async Task GetDividendSummary_PassesTheTickerAndExchangeToTheService()
    {
        await using var factory = CreateFactory();
        using var client = factory.CreateClient();
        var response = await client.GetAsync("/api/v1/financial/dividends/BCIA11/summary?exchange=NYSE");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        doc.RootElement.GetProperty("ticker").GetString().Should().Be("BCIA11");
        doc.RootElement.GetProperty("exchange").GetString().Should().Be("NYSE");
    }

    [Fact]
    public async Task GetDividendHistory_KnownTicker_ReturnsOk()
    {
        await using var factory = CreateFactory();
        using var client = factory.CreateClient();
        var response = await client.GetAsync("/api/v1/financial/dividends/BCIA11/history?exchange=BVMF");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetDividendHistory_Returns400ForWhitespaceTicker()
    {
        await using var factory = CreateFactory();
        using var client = factory.CreateClient();
        var response = await client.GetAsync("/api/v1/financial/dividends/%20/history?exchange=BVMF");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetDividendSummary_Returns400ForWhitespaceTicker()
    {
        await using var factory = CreateFactory();
        using var client = factory.CreateClient();
        var response = await client.GetAsync("/api/v1/financial/dividends/%20/summary?exchange=BVMF");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetDividendSummary_WhenServiceThrows_ReturnsNotFoundWithFriendlyDetail()
    {
        var logs = new RecordingLoggerProvider();
        await using var factory = CreateFactory(throwOnLookup: true, logs);
        using var client = factory.CreateClient();
        var response = await client.GetAsync("/api/v1/financial/dividends/ASDF/summary?exchange=BVMF");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        doc.RootElement.GetProperty("detail").GetString().Should().Contain("ASDF");
        ShouldHaveWarnedWithTheErrorTypeOnly(logs);
    }

    [Fact]
    public async Task GetDividendHistory_WhenServiceThrows_ReturnsNotFoundWithFriendlyDetail()
    {
        var logs = new RecordingLoggerProvider();
        await using var factory = CreateFactory(throwOnLookup: true, logs);
        using var client = factory.CreateClient();
        var response = await client.GetAsync("/api/v1/financial/dividends/ASDF/history?exchange=BVMF");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        doc.RootElement.GetProperty("detail").GetString().Should().Contain("ASDF");
        ShouldHaveWarnedWithTheErrorTypeOnly(logs);
    }

    private static void ShouldHaveWarnedWithTheErrorTypeOnly(RecordingLoggerProvider logs)
    {
        var warning = logs.Entries.Should().ContainSingle(entry =>
            entry.Category == typeof(DividendsController).FullName && entry.Level == LogLevel.Warning).Subject;
        warning.Message.Should().Contain(nameof(InvalidOperationException)).And.NotContain("Ticker not found");
    }

    private static WebApplicationFactory<Program> CreateFactory(bool throwOnLookup = false, RecordingLoggerProvider? logs = null)
    {
        var factory = new ApiTestFactory(dividendServiceOverride: new DividendServiceStub(throwOnLookup));
        return logs is null ? factory : factory.WithWebHostBuilder(builder => builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<ILoggerFactory>();
            services.AddSingleton(LoggerFactory.Create(logging => logging.AddProvider(logs)));
        }));
    }

    private sealed class DividendServiceStub(bool throwOnLookup) : IDividendService
    {
        public IReadOnlyList<DividendHistoryItemDTO> GetDividendHistory(DividendLookupRequestDTO request) =>
            throwOnLookup ? throw new InvalidOperationException("Ticker not found.") : [];

        public DividendSummaryDTO GetDividendSummary(DividendLookupRequestDTO request) =>
            throwOnLookup
                ? throw new InvalidOperationException("Ticker not found.")
                : new DividendSummaryDTO { Exchange = request.Exchange, Ticker = request.Ticker, Name = "Sample Asset", History = [], YearTotals = [] };
    }
}
