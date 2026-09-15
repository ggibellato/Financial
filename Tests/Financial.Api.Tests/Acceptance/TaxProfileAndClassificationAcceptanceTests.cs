using System.Net.Http.Json;
using Financial.Investment.Application.DTOs;
using FluentAssertions;

namespace Financial.Api.Tests.Acceptance;

public class TaxProfileAndClassificationAcceptanceTests : ApiEndpointTests
{
    private const string BrokerName = "XPI";
    private const string PortfolioName = "Default";
    private const string AssetName = "BCIA11";

    [Fact]
    [Trait("AC", "P51-F02-tax-profile-and-classification-08")]
    public async Task AssetDetails_TaxJurisdictions_ReflectsTheDistinctJurisdictionsOfItsClassifications()
    {
        await Client.PostAsJsonAsync("/api/v1/financial/credits", new CreditCreateDTO
        {
            BrokerName = BrokerName,
            PortfolioName = PortfolioName,
            AssetName = AssetName,
            Date = new DateTime(2026, 6, 1),
            Type = "Dividend",
            Value = 100m
        });

        var details = await Client.GetFromJsonAsync<AssetDetailsDTO>(
            $"/api/v1/financial/assets/{BrokerName}/{PortfolioName}/{AssetName}");

        details!.TaxJurisdictions.Should().ContainSingle().Which.Should().Be("BR");
    }

    [Fact]
    [Trait("AC", "P51-F02-tax-profile-and-classification-08")]
    public async Task AssetDetails_WithNoClassifiedEventsYet_TaxJurisdictionsIsEmpty()
    {
        await Client.PostAsJsonAsync("/api/v1/financial/assets", new AssetAdminCreateDTO
        {
            BrokerName = BrokerName,
            PortfolioName = PortfolioName,
            Name = "NOCLASSIFICATION",
            Ticker = "NOCLASSIFICATION",
            Exchange = "BVMF"
        });

        var details = await Client.GetFromJsonAsync<AssetDetailsDTO>(
            $"/api/v1/financial/assets/{BrokerName}/{PortfolioName}/NOCLASSIFICATION");

        details!.TaxJurisdictions.Should().BeEmpty();
    }
}
