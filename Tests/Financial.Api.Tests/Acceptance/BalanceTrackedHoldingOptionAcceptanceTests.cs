using System.Net;
using System.Net.Http.Json;
using Financial.Investment.Application.DTOs;
using Financial.Investment.Domain.Entities;
using FluentAssertions;
using FluentAssertions.Execution;

namespace Financial.Api.Tests.Acceptance;

[Trait("Category", "Integration")]
public class BalanceTrackedHoldingOptionAcceptanceTests : ApiEndpointTests
{
    private const string AssetsRoute = "/api/v1/financial/assets";
    private const string Broker = "XPI";
    private const string Portfolio = "Default";
    private const string HoldingName = "PREV69";
    private static readonly DateTime RecordDate = new(2026, 1, 5);

    [Fact]
    [Trait("AC", "P69-F01-balance-tracked-holding-option-01")]
    public async Task CreateAsset_TrackedByBalance_IsStoredAsProviderValue()
    {
        await CreateHoldingAsync(ValuationMethod.ProviderValue);

        var stored = await GetStoredAsync(HoldingName);

        using (new AssertionScope())
        {
            stored.ValuationMethod.Should().Be(ValuationMethod.ProviderValue);
            stored.IsTrackedByBalance.Should().BeTrue();
        }
    }

    [Fact]
    [Trait("AC", "P69-F01-balance-tracked-holding-option-02")]
    public async Task CreateAsset_WithoutAChoice_IsQuantityAndPrice_AndExistingAssetsAreUnchanged()
    {
        var before = await GetAssetsAsync();

        await CreateHoldingAsync(valuationMethod: null);

        var after = await GetAssetsAsync();
        var created = after.Single(a => a.Name == HoldingName);
        using (new AssertionScope())
        {
            created.ValuationMethod.Should().Be(ValuationMethod.Unspecified);
            created.IsTrackedByBalance.Should().BeFalse();
            after.Where(a => a.Name != HoldingName).Select(a => (a.Name, a.ValuationMethod, a.IsTrackedByBalance))
                .Should().BeEquivalentTo(before.Select(a => (a.Name, a.ValuationMethod, a.IsTrackedByBalance)));
        }
    }

    [Theory]
    [Trait("AC", "P69-F01-balance-tracked-holding-option-03")]
    [InlineData(ValuationMethod.ProviderValue)]
    [InlineData(ValuationMethod.Manual)]
    public async Task ProviderValueAndManualAssets_AreReportedAsBalance(ValuationMethod valuationMethod)
    {
        await CreateHoldingAsync(valuationMethod);

        (await GetStoredAsync(HoldingName)).IsTrackedByBalance.Should().BeTrue();
    }

    [Theory]
    [Trait("AC", "P69-F01-balance-tracked-holding-option-04")]
    [InlineData("transaction")]
    [InlineData("credit")]
    [InlineData("price")]
    public async Task AssetWithATransactionCreditOrPrice_CannotChangeTrackingMode(string record)
    {
        await CreateHoldingAsync(valuationMethod: null);
        await AddRecordAsync(record);

        var response = await Client.PutAsJsonAsync($"{AssetsRoute}/{Broker}/{Portfolio}/{HoldingName}", UpdateRequest(HoldingName, ValuationMethod.ProviderValue));

        var stored = await GetStoredAsync(HoldingName);
        using (new AssertionScope())
        {
            stored.HasRecords.Should().BeTrue();
            response.StatusCode.Should().Be(HttpStatusCode.Conflict);
            stored.ValuationMethod.Should().Be(ValuationMethod.Unspecified);
        }
    }

    [Fact]
    [Trait("AC", "P69-F01-balance-tracked-holding-option-06")]
    public async Task UpdateAsset_ModeChangeAfterATransaction_Returns409AndSavesNothing()
    {
        await CreateHoldingAsync(valuationMethod: null);
        await AddRecordAsync("transaction");

        var response = await Client.PutAsJsonAsync($"{AssetsRoute}/{Broker}/{Portfolio}/{HoldingName}", UpdateRequest("RENAMED69", ValuationMethod.ProviderValue));

        var assets = await GetAssetsAsync();
        using (new AssertionScope())
        {
            response.StatusCode.Should().Be(HttpStatusCode.Conflict);
            (await response.Content.ReadAsStringAsync()).Should().Contain("so how it is tracked can't be changed.");
            assets.Should().ContainSingle(a => a.Name == HoldingName).Which.ValuationMethod.Should().Be(ValuationMethod.Unspecified);
            assets.Should().NotContain(a => a.Name == "RENAMED69");
        }
    }

    private async Task CreateHoldingAsync(ValuationMethod? valuationMethod)
    {
        var response = await Client.PostAsJsonAsync(AssetsRoute, new AssetAdminCreateDTO
        {
            BrokerName = Broker,
            PortfolioName = Portfolio,
            Name = HoldingName,
            Country = CountryCode.BR,
            Class = GlobalAssetClass.Pension,
            ValuationMethod = valuationMethod
        });
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    private async Task AddRecordAsync(string record)
    {
        var response = record switch
        {
            "transaction" => await Client.PostAsJsonAsync("/api/v1/financial/transactions", new TransactionCreateDTO
            {
                BrokerName = Broker,
                PortfolioName = Portfolio,
                AssetName = HoldingName,
                Date = RecordDate,
                Type = nameof(Transaction.TransactionType.Buy),
                Quantity = 10m,
                UnitPrice = 100m,
                Fees = 0m,
                Withheld = 0m
            }),
            "credit" => await Client.PostAsJsonAsync("/api/v1/financial/credits", new CreditCreateDTO
            {
                BrokerName = Broker,
                PortfolioName = Portfolio,
                AssetName = HoldingName,
                Date = RecordDate,
                Type = nameof(Credit.CreditType.Dividend),
                Value = 10m,
                Withheld = 0m
            }),
            _ => await Client.PutAsJsonAsync("/api/v1/financial/prices", new SetAssetPriceDTO
            {
                BrokerName = Broker,
                PortfolioName = Portfolio,
                AssetName = HoldingName,
                Date = DateOnly.FromDateTime(RecordDate),
                Price = 1000m
            })
        };
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    private static AssetAdminUpdateDTO UpdateRequest(string name, ValuationMethod valuationMethod) => new()
    {
        Name = name,
        Country = CountryCode.BR,
        Class = GlobalAssetClass.Pension,
        ValuationMethod = valuationMethod
    };

    private async Task<List<AssetAdminDTO>> GetAssetsAsync() =>
        (await Client.GetFromJsonAsync<List<AssetAdminDTO>>(AssetsRoute))!;

    private async Task<AssetAdminDTO> GetStoredAsync(string name) =>
        (await GetAssetsAsync()).Single(a => a.Name == name);
}
