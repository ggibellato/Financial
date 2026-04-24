using Financial.Application.DTOs;
using Financial.Application.Interfaces;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System;
using System.Net;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Xunit;

namespace Financial.Api.Tests;

public class DividendEndpointsTests
{
    [Fact]
    public async Task GetDividendHistory_ReturnsOk()
    {
        await using var factory = CreateFactory();
        using var client = factory.CreateClient();
        var response = await client.GetAsync("/api/v1/financial/dividends/BCIA11/history?exchange=BVMF");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var history = await response.Content.ReadFromJsonAsync<DividendHistoryItemDTO[]>();
        history.Should().NotBeNull();
        history!.Length.Should().Be(1);
        history[0].Type.Should().Be("Dividend");
    }

    [Fact]
    public async Task GetDividendSummary_ReturnsOk()
    {
        await using var factory = CreateFactory();
        using var client = factory.CreateClient();
        var response = await client.GetAsync("/api/v1/financial/dividends/BCIA11/summary?exchange=BVMF");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var summary = await response.Content.ReadFromJsonAsync<DividendSummaryDTO>();
        summary.Should().NotBeNull();
        summary!.Ticker.Should().Be("BCIA11");
        summary.Exchange.Should().Be("BVMF");
        summary.YearTotals.Should().ContainSingle(total => total.Year == 2023 && total.Total == 4m);
    }

    private static WebApplicationFactory<Program> CreateFactory()
    {
        return new ApiTestFactory().WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<IDividendService>();
                services.AddSingleton<IDividendService>(new DividendServiceStub());
            });
        });
    }

    private sealed class DividendServiceStub : IDividendService
    {
        public IReadOnlyList<DividendHistoryItemDTO> GetDividendHistory(DividendLookupRequestDTO request)
        {
            return new[]
            {
                new DividendHistoryItemDTO
                {
                    Type = "Dividend",
                    Date = new DateTime(2024, 2, 1),
                    Value = 1.23m
                }
            };
        }

        public DividendSummaryDTO GetDividendSummary(DividendLookupRequestDTO request)
        {
            return new DividendSummaryDTO
            {
                Exchange = request.Exchange,
                Ticker = request.Ticker,
                Name = "Sample Asset",
                CurrentPrice = 10.5m,
                PriceAsOf = new DateTimeOffset(2024, 2, 1, 0, 0, 0, TimeSpan.Zero),
                AverageDividendLastFiveYears = 4m,
                PriceMaxBuy = 66.67m,
                DiscountPercent = 20m,
                YearTotals = new[]
                {
                    new DividendYearTotalDTO
                    {
                        Year = 2023,
                        Total = 4m
                    }
                }
            };
        }
    }
}
