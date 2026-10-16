using Financial.Investment.Application.DTOs;
using Financial.Investment.Application.Enums;
using Financial.Investment.Application.Interfaces;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using System.Net.Http.Json;

namespace Financial.Api.Tests.Acceptance;

[Trait("Category", "Integration")]
public class ServerComputedValuationAcceptanceTests : ApiEndpointTests
{
    [Fact]
    public async Task SettingAPrice_ReturnsTheValuationTheServiceComputes()
    {
        var apiResponse = await Client.PutAsJsonAsync("/api/v1/financial/prices", new SetAssetPriceDTO
        {
            BrokerName = "XPI",
            PortfolioName = "Default",
            AssetName = "BCIA11",
            Date = new DateOnly(2026, 8, 15),
            Price = 123.45m
        });
        var apiAsset = await apiResponse.Content.ReadFromJsonAsync<AssetDetailsDTO>();
        apiAsset.Should().NotBeNull();

        var navigationService = Services.GetRequiredService<INavigationService>();
        var serviceAsset = navigationService.GetAssetDetails("XPI", "Default", "BCIA11", InvestmentScope.Active);
        serviceAsset.Should().NotBeNull();

        serviceAsset!.MarketValue.Should().Be(apiAsset!.MarketValue);
        serviceAsset.CostOfUnitsHeld.Should().Be(apiAsset.CostOfUnitsHeld);
        serviceAsset.UnrealisedGain.Should().Be(apiAsset.UnrealisedGain);
        serviceAsset.PriceOnlyReturn.Should().Be(apiAsset.PriceOnlyReturn);
        serviceAsset.TotalReturn.Should().Be(apiAsset.TotalReturn);
        serviceAsset.PriceAsOfDate.Should().Be(apiAsset.PriceAsOfDate);
        serviceAsset.MarketStatus.Should().Be(apiAsset.MarketStatus);
    }

    [Fact]
    public async Task AnUnpricedHolding_IsReportedUnavailableNotNought()
    {
        var apiResponse = await Client.GetAsync("/api/v1/financial/assets/XPI/Default/BCIA11");
        var apiAsset = await apiResponse.Content.ReadFromJsonAsync<AssetDetailsDTO>();
        apiAsset.Should().NotBeNull();

        var navigationService = Services.GetRequiredService<INavigationService>();
        var serviceAsset = navigationService.GetAssetDetails("XPI", "Default", "BCIA11", InvestmentScope.Active);
        serviceAsset.Should().NotBeNull();

        apiAsset!.MarketValue.Should().BeNull();
        apiAsset.UnrealisedGain.Should().BeNull();
        serviceAsset!.MarketValue.Should().BeNull();
        serviceAsset.UnrealisedGain.Should().BeNull();
    }
}
