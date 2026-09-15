using Financial.Investment.Application.Services;
using Financial.Investment.Domain.Entities;
using Financial.Shared.Abstractions.Currencies;
using Financial.TestUtilities;
using FluentAssertions;
using FluentAssertions.Execution;

namespace Financial.Investment.Application.Tests.Services;

public class TaxWorkbookServiceTests
{
    private readonly StubInvestmentRepository _repository = new() { Investments = Investments.Create() };
    private readonly RecordingTelemetryTracer _tracer = new();
    private readonly RecordingLogger<TaxWorkbookService> _logger = new();

    [Fact]
    public void GetWorkbook_ExcludesEntriesForADifferentJurisdiction()
    {
        var asset = SeedAsset();
        asset.AddCredit(Credit.Create(new DateTime(2026, 6, 1), Credit.CreditType.Dividend, 100m, 0m, Currency.BRL), _repository.Investments);
        asset.AddCredit(Credit.Create(new DateTime(2026, 6, 1), Credit.CreditType.Dividend, 50m, 0m, Currency.GBP), _repository.Investments);

        var result = CreateService().GetWorkbook("BR", "2026");

        result.Entries.Should().ContainSingle().Which.GrossAmount.Should().Be(100m);
    }

    [Fact]
    public void GetWorkbook_ExcludesEntriesForADifferentTaxYear()
    {
        var asset = SeedAsset();
        asset.AddCredit(Credit.Create(new DateTime(2026, 6, 1), Credit.CreditType.Dividend, 100m, 0m, Currency.BRL), _repository.Investments);
        asset.AddCredit(Credit.Create(new DateTime(2025, 6, 1), Credit.CreditType.Dividend, 50m, 0m, Currency.BRL), _repository.Investments);

        var result = CreateService().GetWorkbook("BR", "2026");

        result.Entries.Should().ContainSingle().Which.GrossAmount.Should().Be(100m);
    }

    [Fact]
    public void GetWorkbook_ExcludesASupersededDisposalClassification()
    {
        var asset = SeedAsset();
        asset.AddTransaction(Transaction.Create(new DateTime(2021, 1, 1), Transaction.TransactionType.Buy, 10m, 50m, 0m));
        var sell = Transaction.Create(new DateTime(2026, 6, 1), Transaction.TransactionType.Sell, 5m, 100m, 0m);
        asset.RecordTransaction(sell, investments: _repository.Investments);
        var taxYear = asset.DisposalRecords.Single().TaxYear;

        asset.RetractTransaction(sell.Id, investments: _repository.Investments);

        var result = CreateService().GetWorkbook("BR", taxYear);

        result.Entries.Should().BeEmpty();
    }

    [Fact]
    public void GetWorkbook_GroupsAndTotalsByEventCategory()
    {
        var asset = SeedAsset();
        asset.AddCredit(Credit.Create(new DateTime(2026, 1, 1), Credit.CreditType.Dividend, 100m, 10m, Currency.BRL), _repository.Investments);
        asset.AddCredit(Credit.Create(new DateTime(2026, 2, 1), Credit.CreditType.Dividend, 50m, 5m, Currency.BRL), _repository.Investments);
        asset.AddTransaction(Transaction.Create(new DateTime(2021, 1, 1), Transaction.TransactionType.Buy, 10m, 60m, 0m));
        asset.RecordTransaction(Transaction.Create(new DateTime(2026, 3, 1), Transaction.TransactionType.Sell, 10m, 100m, 0m), investments: _repository.Investments);

        var result = CreateService().GetWorkbook("BR", "2026");

        using (new AssertionScope())
        {
            result.CategoryTotals.Should().HaveCount(2);
            var dividendTotal = result.CategoryTotals.Should().ContainSingle(t => t.EventCategory == EventCategory.Dividend).Subject;
            dividendTotal.TotalGrossAmount.Should().Be(150m);
            dividendTotal.TotalWithheldAmount.Should().Be(15m);
            dividendTotal.TotalNetAmount.Should().Be(135m);
            dividendTotal.TotalProceeds.Should().BeNull();

            var capitalGainTotal = result.CategoryTotals.Should().ContainSingle(t => t.EventCategory == EventCategory.CapitalGain).Subject;
            capitalGainTotal.TotalProceeds.Should().Be(1000m);
            capitalGainTotal.TotalCostBasis.Should().Be(600m);
            capitalGainTotal.TotalGainLoss.Should().Be(400m);
        }
    }

    [Fact]
    public void GetWorkbook_AggregateStatus_IncompleteWhenNoRuleAppliesToAnyEntry()
    {
        var asset = SeedAsset();
        asset.AddCredit(Credit.Create(new DateTime(2026, 1, 1), Credit.CreditType.Dividend, 100m, 0m, Currency.BRL), _repository.Investments);
        asset.AddCredit(Credit.Create(new DateTime(2026, 2, 1), Credit.CreditType.Coupon, 50m, 0m, Currency.BRL), _repository.Investments);

        var result = CreateService().GetWorkbook("BR", "2026");

        result.CalculationStatus.Should().Be(CalculationStatus.Incomplete);
    }

    [Fact]
    public void GetWorkbook_AggregateStatus_IsIncompleteWhenOnlySomeEntriesHaveAnApplicableRule()
    {
        var asset = SeedAsset();
        _repository.Investments!.CreateTaxRule(
            Jurisdiction.BR, EventCategory.Dividend, "BR dividend rule", "desc", new DateOnly(2026, 1, 1), null);
        asset.AddCredit(Credit.Create(new DateTime(2026, 1, 1), Credit.CreditType.Dividend, 100m, 0m, Currency.BRL), _repository.Investments);
        asset.AddCredit(Credit.Create(new DateTime(2026, 2, 1), Credit.CreditType.Coupon, 50m, 0m, Currency.BRL), _repository.Investments);

        var result = CreateService().GetWorkbook("BR", "2026");

        result.CalculationStatus.Should().Be(CalculationStatus.Incomplete);
    }

    [Fact]
    public void GetWorkbook_AggregateStatus_FinalWhenARuleAppliesToEveryEntry()
    {
        var asset = SeedAsset();
        _repository.Investments!.CreateTaxRule(
            Jurisdiction.BR, EventCategory.Dividend, "BR dividend rule", "desc", new DateOnly(2026, 1, 1), null);
        asset.AddCredit(Credit.Create(new DateTime(2026, 1, 1), Credit.CreditType.Dividend, 100m, 0m, Currency.BRL), _repository.Investments);
        asset.AddCredit(Credit.Create(new DateTime(2026, 2, 1), Credit.CreditType.Dividend, 50m, 0m, Currency.BRL), _repository.Investments);

        var result = CreateService().GetWorkbook("BR", "2026");

        result.CalculationStatus.Should().Be(CalculationStatus.Final);
    }

    [Fact]
    public void GetWorkbook_NoMatchingClassifications_ReturnsEmptyEntriesAndNullStatus()
    {
        SeedAsset();

        var result = CreateService().GetWorkbook("BR", "2099");

        using (new AssertionScope())
        {
            result.Entries.Should().BeEmpty();
            result.CategoryTotals.Should().BeEmpty();
            result.CalculationStatus.Should().BeNull();
        }
    }

    [Fact]
    public void GetWorkbook_DisposalSourcedEntry_ResolvesDateAndEvidenceReferenceFromTheDisposalRecord()
    {
        var asset = SeedAsset();
        asset.AddTransaction(Transaction.Create(new DateTime(2021, 1, 1), Transaction.TransactionType.Buy, 10m, 100m, 0m));
        asset.RecordTransaction(Transaction.Create(new DateTime(2026, 6, 1), Transaction.TransactionType.Sell, 5m, 110m, 0m), investments: _repository.Investments);
        var disposalRecord = asset.DisposalRecords.Single();

        var result = CreateService().GetWorkbook("BR", disposalRecord.TaxYear);

        var entry = result.Entries.Should().ContainSingle().Subject;
        entry.Date.Should().Be(disposalRecord.Date);
        entry.EvidenceReference.Should().Be(disposalRecord.Id);
    }

    [Fact]
    public void GetWorkbook_CreditSourcedEntry_ResolvesDateAndEvidenceReferenceFromTheCredit()
    {
        var asset = SeedAsset();
        var credit = Credit.Create(new DateTime(2026, 6, 1), Credit.CreditType.Dividend, 100m, 0m, Currency.BRL);
        asset.AddCredit(credit, _repository.Investments);

        var result = CreateService().GetWorkbook("BR", "2026");

        var entry = result.Entries.Should().ContainSingle().Subject;
        entry.Date.Should().Be(credit.Date);
        entry.EvidenceReference.Should().Be(credit.Id);
    }

    [Fact]
    public void GetWorkbook_FinalEntry_ResolvesTheApplicableTaxRuleLabel()
    {
        var asset = SeedAsset();
        _repository.Investments!.CreateTaxRule(
            Jurisdiction.BR, EventCategory.Dividend, "BR dividend withholding", "desc", new DateOnly(2026, 1, 1), null);
        asset.AddCredit(Credit.Create(new DateTime(2026, 6, 1), Credit.CreditType.Dividend, 100m, 0m, Currency.BRL), _repository.Investments);

        var result = CreateService().GetWorkbook("BR", "2026");

        result.Entries.Should().ContainSingle().Which.TaxRuleLabel.Should().Be("BR dividend withholding");
    }

    [Fact]
    public void GetWorkbook_IncompleteEntry_HasNoTaxRuleLabel()
    {
        var asset = SeedAsset();
        asset.AddCredit(Credit.Create(new DateTime(2026, 6, 1), Credit.CreditType.Dividend, 100m, 0m, Currency.BRL), _repository.Investments);

        var result = CreateService().GetWorkbook("BR", "2026");

        result.Entries.Should().ContainSingle().Which.TaxRuleLabel.Should().BeNull();
    }

    [Fact]
    public void GetWorkbook_InvalidJurisdiction_ThrowsArgumentException()
    {
        var act = () => CreateService().GetWorkbook("NotAJurisdiction", "2026");

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void GetWorkbookOptions_ReturnsDistinctPairsWithAtLeastOneActiveClassification()
    {
        var asset = SeedAsset();
        asset.AddCredit(Credit.Create(new DateTime(2026, 1, 1), Credit.CreditType.Dividend, 10m, 0m, Currency.BRL), _repository.Investments);
        asset.AddCredit(Credit.Create(new DateTime(2026, 2, 1), Credit.CreditType.Coupon, 10m, 0m, Currency.BRL), _repository.Investments);
        asset.AddCredit(Credit.Create(new DateTime(2026, 3, 1), Credit.CreditType.Dividend, 10m, 0m, Currency.GBP), _repository.Investments);
        var sell = Transaction.Create(new DateTime(2024, 6, 1), Transaction.TransactionType.Sell, 1m, 10m, 0m);
        asset.AddTransaction(Transaction.Create(new DateTime(2020, 1, 1), Transaction.TransactionType.Buy, 1m, 5m, 0m));
        asset.RecordTransaction(sell, investments: _repository.Investments);
        asset.RetractTransaction(sell.Id, investments: _repository.Investments);

        var result = CreateService().GetWorkbookOptions();

        using (new AssertionScope())
        {
            result.Should().HaveCount(2);
            result.Should().ContainSingle(o => o.Jurisdiction == Jurisdiction.BR && o.TaxYear == "2026");
            result.Should().ContainSingle(o => o.Jurisdiction == Jurisdiction.UK);
        }
    }

    private Asset SeedAsset()
    {
        var asset = Asset.Create("Asset A", "ISIN123", "BVMF", "AAA");
        var broker = Broker.Create("XPI", "BRL");
        broker.AddPortfolio("Default").AddAsset(asset);
        _repository.Investments!.AddActiveBroker(broker);
        return asset;
    }

    private TaxWorkbookService CreateService() => new(_repository, _tracer, _logger);
}
