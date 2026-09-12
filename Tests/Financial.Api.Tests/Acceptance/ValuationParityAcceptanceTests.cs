using Financial.Investment.Application.DTOs;
using Financial.Investment.Application.Enums;
using Financial.Investment.Application.Interfaces;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using System.Net.Http.Json;

namespace Financial.Api.Tests.Acceptance;

public class ValuationParityAcceptanceTests : ApiEndpointTests
{
    [Fact]
    public async Task WebAndDesktopReadTheIdenticalServerComputedValuation()
    {
        var webResponse = await Client.PutAsJsonAsync("/api/v1/financial/prices", new SetAssetPriceDTO
        {
            BrokerName = "XPI",
            PortfolioName = "Default",
            AssetName = "BCIA11",
            Date = new DateOnly(2026, 8, 15),
            Price = 123.45m
        });
        var webAsset = await webResponse.Content.ReadFromJsonAsync<AssetDetailsDTO>();
        webAsset.Should().NotBeNull();

        var navigationService = Services.GetRequiredService<INavigationService>();
        var desktopAsset = navigationService.GetAssetDetails("XPI", "Default", "BCIA11", InvestmentScope.Active);
        desktopAsset.Should().NotBeNull();

        desktopAsset!.MarketValue.Should().Be(webAsset!.MarketValue);
        desktopAsset.CostOfUnitsHeld.Should().Be(webAsset.CostOfUnitsHeld);
        desktopAsset.UnrealisedGain.Should().Be(webAsset.UnrealisedGain);
        desktopAsset.PriceOnlyReturn.Should().Be(webAsset.PriceOnlyReturn);
        desktopAsset.TotalReturn.Should().Be(webAsset.TotalReturn);
        desktopAsset.PriceAsOfDate.Should().Be(webAsset.PriceAsOfDate);
        desktopAsset.MarketStatus.Should().Be(webAsset.MarketStatus);
    }

    [Fact]
    public async Task WebAndDesktopBothReportUnavailableNotNoughtForAnUnpricedHolding()
    {
        var webResponse = await Client.GetAsync("/api/v1/financial/assets/XPI/Default/BCIA11");
        var webAsset = await webResponse.Content.ReadFromJsonAsync<AssetDetailsDTO>();
        webAsset.Should().NotBeNull();

        var navigationService = Services.GetRequiredService<INavigationService>();
        var desktopAsset = navigationService.GetAssetDetails("XPI", "Default", "BCIA11", InvestmentScope.Active);
        desktopAsset.Should().NotBeNull();

        webAsset!.MarketValue.Should().BeNull();
        webAsset.UnrealisedGain.Should().BeNull();
        desktopAsset!.MarketValue.Should().BeNull();
        desktopAsset.UnrealisedGain.Should().BeNull();
    }
}
