using System.Net;
using System.Net.Http.Json;
using Financial.Investment.Application.DTOs;
using Financial.Investment.Domain.Entities;
using FluentAssertions;
using FluentAssertions.Execution;

namespace Financial.Api.Tests.Acceptance;

public class UpcomingIncomeAcceptanceTests : ApiEndpointTests
{
    private const string UpcomingIncomeRoute = "/api/v1/financial/upcoming-income";
    private const string SeededBroker = "XPI";
    private const string SeededPortfolio = "Default";
    private const string SeededAsset = "BCIA11";
    private const string MonthlyAsset = "MONTHLYPAYER";
    private const string QuarterlyAsset = "QUARTERLYPAYER";
    private const string FourMonthlyAsset = "FOURMONTHLYPAYER";
    private const string SingleCreditAsset = "SINGLECREDIT";

    private static readonly DateTime PurchaseDate = new(2024, 12, 1);
    private static readonly DateTime FirstCreditDate = new(2025, 1, 15);

    [Fact]
    [Trait("AC", "P52-F04-upcoming-income-01")]
    public async Task EachDetectedFrequency_ProjectsExactlyOneIntervalAfterTheLastCredit()
    {
        await SeedFrequencyPayersAsync();

        var entries = await GetUpcomingIncomeAsync();

        using var _ = new AssertionScope();
        var monthly = EntryFor(entries, MonthlyAsset);
        monthly.LastCreditDate.Should().Be(new DateTime(2025, 3, 15));
        monthly.ProjectedNextDate.Should().Be(monthly.LastCreditDate.AddMonths(1));

        var quarterly = EntryFor(entries, QuarterlyAsset);
        quarterly.LastCreditDate.Should().Be(new DateTime(2025, 7, 15));
        quarterly.ProjectedNextDate.Should().Be(quarterly.LastCreditDate.AddMonths(3));

        var fourMonthly = EntryFor(entries, FourMonthlyAsset);
        fourMonthly.LastCreditDate.Should().Be(new DateTime(2025, 9, 15));
        fourMonthly.ProjectedNextDate.Should().Be(fourMonthly.LastCreditDate.AddMonths(4));
    }

    [Fact]
    [Trait("AC", "P52-F04-upcoming-income-02")]
    public async Task AHoldingWithNoDetectableFrequency_IsOmittedFromTheList()
    {
        await SeedFrequencyPayersAsync();
        await SeedPayerAsync(SingleCreditAsset, FirstCreditDate);

        var entries = await GetUpcomingIncomeAsync();

        using var _ = new AssertionScope();
        entries.Should().NotBeEmpty();
        entries.Should().NotContain(entry => entry.AssetName == SingleCreditAsset);
    }

    [Fact]
    [Trait("AC", "P52-F04-upcoming-income-03")]
    public async Task ProjectedDates_AreIdenticalAcrossCalls_SoNoWindowChoiceCanMoveThem()
    {
        await SeedFrequencyPayersAsync();

        var first = await GetUpcomingIncomeAsync();
        var second = await GetUpcomingIncomeAsync();

        using var _ = new AssertionScope();
        first.Should().NotBeEmpty();
        second.Select(entry => (entry.AssetName, entry.ProjectedNextDate))
            .Should().Equal(first.Select(entry => (entry.AssetName, entry.ProjectedNextDate)));
    }

    [Fact]
    [Trait("AC", "P52-F04-upcoming-income-04")]
    public async Task NoProjectableHolding_ReturnsAnEmptyListRatherThanAnError()
    {
        await ArchiveTheOnlyProjectableHoldingAsync();

        var response = await Client.GetAsync(UpcomingIncomeRoute);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        (await response.Content.ReadFromJsonAsync<List<UpcomingIncomeDTO>>()).Should().BeEmpty();
    }

    private static UpcomingIncomeDTO EntryFor(IReadOnlyList<UpcomingIncomeDTO> entries, string assetName) =>
        entries.Single(entry => entry.AssetName == assetName);

    private async Task<IReadOnlyList<UpcomingIncomeDTO>> GetUpcomingIncomeAsync() =>
        (await Client.GetFromJsonAsync<List<UpcomingIncomeDTO>>(UpcomingIncomeRoute))!;

    private async Task SeedFrequencyPayersAsync()
    {
        await SeedPayerAsync(MonthlyAsset, FirstCreditDate, new DateTime(2025, 2, 15), new DateTime(2025, 3, 15));
        await SeedPayerAsync(QuarterlyAsset, FirstCreditDate, new DateTime(2025, 4, 15), new DateTime(2025, 7, 15));
        await SeedPayerAsync(FourMonthlyAsset, FirstCreditDate, new DateTime(2025, 5, 15), new DateTime(2025, 9, 15));
    }

    private async Task SeedPayerAsync(string assetName, params DateTime[] creditDates)
    {
        await CreateAssetAsync(assetName);
        await BuyAsync(assetName);
        foreach (var creditDate in creditDates)
        {
            await AddCreditAsync(assetName, creditDate);
        }
    }

    private async Task CreateAssetAsync(string assetName)
    {
        var response = await Client.PostAsJsonAsync("/api/v1/financial/assets", new AssetAdminCreateDTO
        {
            BrokerName = SeededBroker,
            PortfolioName = SeededPortfolio,
            Name = assetName,
            Exchange = "BVMF",
            Ticker = assetName,
            Country = CountryCode.BR,
            Class = GlobalAssetClass.Equity
        });
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    private async Task BuyAsync(string assetName)
    {
        var response = await Client.PostAsJsonAsync("/api/v1/financial/transactions", new TransactionCreateDTO
        {
            BrokerName = SeededBroker,
            PortfolioName = SeededPortfolio,
            AssetName = assetName,
            Date = PurchaseDate,
            Type = nameof(Transaction.TransactionType.Buy),
            Quantity = 10m,
            UnitPrice = 5m,
            Fees = 0m,
            Withheld = 0m
        });
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    private async Task AddCreditAsync(string assetName, DateTime creditDate)
    {
        var response = await Client.PostAsJsonAsync("/api/v1/financial/credits", new CreditCreateDTO
        {
            BrokerName = SeededBroker,
            PortfolioName = SeededPortfolio,
            AssetName = assetName,
            Date = creditDate,
            Type = nameof(Credit.CreditType.Dividend),
            Value = 12m,
            Withheld = 0m
        });
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    private async Task ArchiveTheOnlyProjectableHoldingAsync()
    {
        var asset = await Client.GetFromJsonAsync<AssetDetailsDTO>(
            $"/api/v1/financial/assets/{SeededBroker}/{SeededPortfolio}/{SeededAsset}");

        var sell = await Client.PostAsJsonAsync("/api/v1/financial/transactions", new TransactionCreateDTO
        {
            BrokerName = SeededBroker,
            PortfolioName = SeededPortfolio,
            AssetName = SeededAsset,
            Date = new DateTime(2025, 12, 1),
            Type = nameof(Transaction.TransactionType.Sell),
            Quantity = asset!.Quantity,
            UnitPrice = 10m,
            Fees = 0m,
            Withheld = 0m
        });
        sell.StatusCode.Should().Be(HttpStatusCode.OK);

        var archive = await Client.PostAsJsonAsync("/api/v1/financial/assets/archive", new ArchiveAssetRequestDTO
        {
            BrokerName = SeededBroker,
            SourcePortfolioName = SeededPortfolio,
            AssetName = SeededAsset,
            DestinationPortfolioName = "Closed 2025"
        });
        archive.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
