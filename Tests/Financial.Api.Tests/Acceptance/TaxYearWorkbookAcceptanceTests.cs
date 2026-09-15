using System.Net.Http.Json;
using Financial.Investment.Application.DTOs;
using Financial.Investment.Domain.Entities;
using FluentAssertions;

namespace Financial.Api.Tests.Acceptance;

public class TaxYearWorkbookAcceptanceTests : ApiEndpointTests
{
    private const string BrokerName = "XPI";
    private const string PortfolioName = "Default";
    private const string AssetName = "BCIA11";

    [Fact]
    [Trait("AC", "P51-F03-tax-year-workbook-01")]
    [Trait("AC", "P51-F03-tax-year-workbook-05")]
    public async Task WorkbookOptionsAndWorkbook_ReflectARecordedCreditForThatJurisdictionAndTaxYear()
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

        var options = await Client.GetFromJsonAsync<List<TaxWorkbookOptionDTO>>("/api/v1/financial/tax-workbook/options");
        options.Should().ContainSingle(o => o.Jurisdiction == Jurisdiction.BR && o.TaxYear == "2026");

        var workbook = await Client.GetFromJsonAsync<TaxWorkbookDTO>("/api/v1/financial/tax-workbook?jurisdiction=BR&taxYear=2026");
        workbook!.Entries.Should().ContainSingle(e => e.EventCategory == EventCategory.Dividend && e.GrossAmount == 100m);
    }

    [Fact]
    [Trait("AC", "P51-F03-tax-year-workbook-02")]
    public async Task Workbook_TotalsMultipleEntriesOfTheSameCategory()
    {
        await Client.PostAsJsonAsync("/api/v1/financial/credits", new CreditCreateDTO
        {
            BrokerName = BrokerName,
            PortfolioName = PortfolioName,
            AssetName = AssetName,
            Date = new DateTime(2026, 3, 1),
            Type = "Dividend",
            Value = 60m
        });
        await Client.PostAsJsonAsync("/api/v1/financial/credits", new CreditCreateDTO
        {
            BrokerName = BrokerName,
            PortfolioName = PortfolioName,
            AssetName = AssetName,
            Date = new DateTime(2026, 9, 1),
            Type = "Dividend",
            Value = 40m
        });

        var workbook = await Client.GetFromJsonAsync<TaxWorkbookDTO>("/api/v1/financial/tax-workbook?jurisdiction=BR&taxYear=2026");

        var total = workbook!.CategoryTotals.Should().ContainSingle(t => t.EventCategory == EventCategory.Dividend).Subject;
        total.TotalGrossAmount.Should().Be(100m);
    }

    [Fact]
    [Trait("AC", "P51-F03-tax-year-workbook-03")]
    public async Task Workbook_AggregateStatus_IsIncompleteWhenNoRuleCoversTheEvent()
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

        var workbook = await Client.GetFromJsonAsync<TaxWorkbookDTO>("/api/v1/financial/tax-workbook?jurisdiction=BR&taxYear=2026");

        workbook!.CalculationStatus.Should().Be(CalculationStatus.Incomplete);
    }

    [Fact]
    [Trait("AC", "P51-F03-tax-year-workbook-03")]
    public async Task Workbook_AggregateStatus_IsFinalWhenARuleCoversEveryEntry()
    {
        await Client.PostAsJsonAsync("/api/v1/financial/tax-rules", new TaxRuleCreateDTO
        {
            Jurisdiction = Jurisdiction.BR,
            EventCategory = EventCategory.Dividend,
            Label = "BR dividend rule",
            EffectiveFrom = new DateOnly(2026, 1, 1),
            EffectiveTo = null
        });

        await Client.PostAsJsonAsync("/api/v1/financial/credits", new CreditCreateDTO
        {
            BrokerName = BrokerName,
            PortfolioName = PortfolioName,
            AssetName = AssetName,
            Date = new DateTime(2026, 6, 1),
            Type = "Dividend",
            Value = 100m
        });

        var workbook = await Client.GetFromJsonAsync<TaxWorkbookDTO>("/api/v1/financial/tax-workbook?jurisdiction=BR&taxYear=2026");

        var entry = workbook!.Entries.Should().ContainSingle().Subject;
        entry.CalculationStatus.Should().Be(CalculationStatus.Final);
        entry.TaxRuleLabel.Should().Be("BR dividend rule");
        workbook.CalculationStatus.Should().Be(CalculationStatus.Final);
    }

    [Fact]
    [Trait("AC", "P51-F03-tax-year-workbook-04")]
    public async Task Workbook_EntryEvidenceReference_ResolvesBackToTheRecordedCredit()
    {
        var createResponse = await Client.PostAsJsonAsync("/api/v1/financial/credits", new CreditCreateDTO
        {
            BrokerName = BrokerName,
            PortfolioName = PortfolioName,
            AssetName = AssetName,
            Date = new DateTime(2026, 6, 1),
            Type = "Dividend",
            Value = 100m
        });
        var assetDetails = await createResponse.Content.ReadFromJsonAsync<AssetDetailsDTO>();
        var creditId = assetDetails!.Credits.Single(c => c.Value == 100m && c.Date == new DateTime(2026, 6, 1)).Id;

        var workbook = await Client.GetFromJsonAsync<TaxWorkbookDTO>("/api/v1/financial/tax-workbook?jurisdiction=BR&taxYear=2026");

        workbook!.Entries.Should().ContainSingle().Which.EvidenceReference.Should().Be(creditId);
    }

    [Fact]
    public async Task Workbook_InvalidJurisdiction_ReturnsBadRequest()
    {
        var response = await Client.GetAsync("/api/v1/financial/tax-workbook?jurisdiction=NotReal&taxYear=2026");

        response.StatusCode.Should().Be(System.Net.HttpStatusCode.BadRequest);
    }
}
