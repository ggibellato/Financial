using System.Net.Http.Json;
using Financial.Investment.Application.DTOs;
using Financial.Investment.Application.Interfaces;
using Financial.Investment.Domain.Entities;
using Financial.Shared.Abstractions.Currencies;
using Financial.TestUtilities;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;

namespace Financial.Api.Tests.Acceptance;

/// <summary>
/// F02 adds no DTO/endpoint surface of its own (that's F04/F05), so these assert directly against
/// the repository after posting through the real, already-existing transaction/credit endpoints -
/// the same Integration pattern as any other AC-tracing test, just without a response body to read
/// the captured fields from.
/// </summary>
public class TransactionCreditCurrencyAcceptanceTests : ApiEndpointTests
{
    private const string BrokerName = "XPI";
    private const string PortfolioName = "Default";
    private const string AssetName = "BCIA11";

    public TransactionCreditCurrencyAcceptanceTests()
        : base(exchangeRateProvider: new StubExchangeRateProvider(0.146m))
    {
    }

    [Fact]
    [Trait("AC", "P49-F02-transaction-and-credit-currency-01")]
    public async Task CreateTransaction_RecordsCurrencyAutoFilledFromTheBroker()
    {
        var response = await Client.PostAsJsonAsync("/api/v1/financial/transactions", new TransactionCreateDTO
        {
            BrokerName = BrokerName,
            PortfolioName = PortfolioName,
            AssetName = AssetName,
            Date = new DateTime(2026, 7, 1),
            Type = "Buy",
            Quantity = 1234m,
            UnitPrice = 9.99m,
            Fees = 0m
        });
        response.EnsureSuccessStatusCode();

        var asset = Services.GetRequiredService<IInvestmentRepository>().GetAsset(BrokerName, PortfolioName, AssetName);
        var created = asset!.Transactions.Should().Contain(t => t.Quantity == 1234m).Subject;
        created.Currency.Should().Be(Currency.BRL, "XPI's broker currency is BRL");
    }

    [Fact]
    [Trait("AC", "P49-F02-transaction-and-credit-currency-02")]
    public async Task CreateTransaction_WhoseCurrencyDiffersFromReportingCurrency_CarriesAPopulatedFxRateSnapshot()
    {
        var response = await Client.PostAsJsonAsync("/api/v1/financial/transactions", new TransactionCreateDTO
        {
            BrokerName = BrokerName,
            PortfolioName = PortfolioName,
            AssetName = AssetName,
            Date = new DateTime(2026, 7, 1),
            Type = "Buy",
            Quantity = 4321m,
            UnitPrice = 9.99m,
            Fees = 0m
        });
        response.EnsureSuccessStatusCode();

        var asset = Services.GetRequiredService<IInvestmentRepository>().GetAsset(BrokerName, PortfolioName, AssetName);
        var created = asset!.Transactions.Should().Contain(t => t.Quantity == 4321m).Subject;
        created.FxRateSnapshot.Should().NotBeNull();
        created.FxRateSnapshot!.ToCurrency.Should().Be(Currency.GBP, "the interim reporting currency is fixed at GBP until F03");
        created.FxRateSnapshot!.Rate.Should().Be(0.146m);
    }

    [Fact]
    [Trait("AC", "P49-F02-transaction-and-credit-currency-03")]
    public async Task CreateTransaction_WhoseCurrencyMatchesReportingCurrency_CarriesNoSnapshot()
    {
        const string GbpBroker = "Trading212";
        const string GbpAsset = "VWRL";
        var repository = Services.GetRequiredService<IInvestmentRepository>();
        await repository.ApplyAndSaveAsync(() =>
        {
            var broker = Broker.Create(GbpBroker, "GBP");
            var portfolio = broker.AddPortfolio(PortfolioName);
            portfolio.AddAsset(Asset.Create(GbpAsset, "GBPISIN", "LSE", GbpAsset));
            repository.GetInvestments().AddActiveBroker(broker);
            return true;
        });

        var response = await Client.PostAsJsonAsync("/api/v1/financial/transactions", new TransactionCreateDTO
        {
            BrokerName = GbpBroker,
            PortfolioName = PortfolioName,
            AssetName = GbpAsset,
            Date = new DateTime(2026, 7, 1),
            Type = "Buy",
            Quantity = 8888m,
            UnitPrice = 9.99m,
            Fees = 0m
        });
        response.EnsureSuccessStatusCode();

        var asset = repository.GetAsset(GbpBroker, PortfolioName, GbpAsset);
        var created = asset!.Transactions.Should().Contain(t => t.Quantity == 8888m).Subject;
        created.Currency.Should().Be(Currency.GBP, "Trading212's broker currency matches the fixed GBP reporting currency");
        created.FxRateSnapshot.Should().BeNull();
    }

    [Fact]
    public async Task UpdateTransaction_PreservesTheOriginallyCapturedCurrencyAndFxRateSnapshot()
    {
        var createResponse = await Client.PostAsJsonAsync("/api/v1/financial/transactions", new TransactionCreateDTO
        {
            BrokerName = BrokerName,
            PortfolioName = PortfolioName,
            AssetName = AssetName,
            Date = new DateTime(2026, 7, 1),
            Type = "Buy",
            Quantity = 5555m,
            UnitPrice = 9.99m,
            Fees = 0m
        });
        createResponse.EnsureSuccessStatusCode();
        var repository = Services.GetRequiredService<IInvestmentRepository>();
        var original = repository.GetAsset(BrokerName, PortfolioName, AssetName)!.Transactions.Should().Contain(t => t.Quantity == 5555m).Subject;

        var updateResponse = await Client.PutAsJsonAsync("/api/v1/financial/transactions", new TransactionUpdateDTO
        {
            Id = original.Id,
            BrokerName = BrokerName,
            PortfolioName = PortfolioName,
            AssetName = AssetName,
            Date = new DateTime(2026, 7, 1),
            Type = "Buy",
            Quantity = 6666m,
            UnitPrice = 9.99m,
            Fees = 0m
        });
        updateResponse.EnsureSuccessStatusCode();

        var updated = repository.GetAsset(BrokerName, PortfolioName, AssetName)!.Transactions.Should().Contain(t => t.Id == original.Id).Subject;
        updated.Quantity.Should().Be(6666m);
        updated.Currency.Should().Be(original.Currency);
        updated.FxRateSnapshot.Should().Be(original.FxRateSnapshot);
    }

    [Fact]
    [Trait("AC", "P49-F02-transaction-and-credit-currency-01")]
    public async Task CreateCredit_RecordsCurrencyAndFxRateSnapshot()
    {
        var response = await Client.PostAsJsonAsync("/api/v1/financial/credits", new CreditCreateDTO
        {
            BrokerName = BrokerName,
            PortfolioName = PortfolioName,
            AssetName = AssetName,
            Date = new DateTime(2026, 7, 1),
            Type = "Dividend",
            Value = 777m
        });
        response.EnsureSuccessStatusCode();

        var asset = Services.GetRequiredService<IInvestmentRepository>().GetAsset(BrokerName, PortfolioName, AssetName);
        var created = asset!.Credits.Should().Contain(c => c.Value == 777m).Subject;
        created.Currency.Should().Be(Currency.BRL);
        created.FxRateSnapshot.Should().NotBeNull();
    }
}
