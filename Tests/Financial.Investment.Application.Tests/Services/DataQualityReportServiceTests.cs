using Financial.Investment.Application.Enums;
using Financial.Investment.Application.Services;
using Financial.Investment.Domain.Entities;
using Financial.Shared.Abstractions.Currencies;
using Financial.Shared.Abstractions.Observability;
using Financial.TestUtilities;
using FluentAssertions;
using FluentAssertions.Execution;

namespace Financial.Investment.Application.Tests.Services;

public class DataQualityReportServiceTests
{
    private static readonly DateTimeOffset Today = new(2026, 8, 14, 9, 0, 0, TimeSpan.Zero);
    private static readonly DateOnly TodayDate = DateOnly.FromDateTime(Today.UtcDateTime);

    private readonly StubInvestmentRepository _repository = new();
    private readonly RecordingTelemetryTracer _tracer = new();
    private readonly RecordingLogger<DataQualityReportService> _logger = new();

    [Fact]
    public void Constructor_WithNullHoldingValuationService_Throws()
    {
        Action act = () => new DataQualityReportService(_repository, _tracer, _logger, null!);

        act.Should().Throw<ArgumentNullException>().WithParameterName("holdingValuationService");
    }

    [Fact]
    public void GenerateReport_SaleExceedsPurchases_NamesHoldingAndShortfall()
    {
        var asset = Asset.Create("OVERSOLD", "ISIN1", "BVMF", "OVS");
        asset.AddTransaction(Transaction.Create(new DateTime(2021, 1, 1), Transaction.TransactionType.Buy, 5m, 10m, 0m));
        asset.AddTransaction(Transaction.Create(new DateTime(2021, 6, 1), Transaction.TransactionType.Sell, 8m, 12m, 0m));

        SeedActive(asset);

        var report = CreateService().GenerateReport();

        using var _ = new AssertionScope();
        report.SalesExceedPurchases.Should().ContainSingle();
        var finding = report.SalesExceedPurchases[0];
        finding.AssetName.Should().Be("OVERSOLD");
        finding.QuantityHeld.Should().Be(5m);
        finding.Shortfall.Should().Be(3m);
        finding.OffendingSaleDate.Should().Be(new DateTime(2021, 6, 1));
    }

    [Fact]
    public void GenerateReport_SalesCoveredByPurchases_NotReported()
    {
        var asset = Asset.Create("COVERED", "ISIN1", "BVMF", "COV");
        asset.AddTransaction(Transaction.Create(new DateTime(2021, 1, 1), Transaction.TransactionType.Buy, 10m, 10m, 0m));
        asset.AddTransaction(Transaction.Create(new DateTime(2021, 6, 1), Transaction.TransactionType.Sell, 5m, 12m, 0m));

        SeedActive(asset);

        var report = CreateService().GenerateReport();

        report.SalesExceedPurchases.Should().BeEmpty();
    }

    [Fact]
    public void GenerateReport_OpenHoldingWithNoRecordedPrice_IsNamed()
    {
        var asset = Asset.Create("UNPRICED", "ISIN1", "BVMF", "UNP");
        asset.AddTransaction(Transaction.Create(DateTime.Today, Transaction.TransactionType.Buy, 1m, 10m, 0m));

        SeedActive(asset);

        var report = CreateService().GenerateReport();

        report.UnpricedOpenHoldings.Should().ContainSingle(f => f.AssetName == "UNPRICED");
    }

    [Fact]
    public void GenerateReport_OpenHoldingWithRecordedPrice_NotReported()
    {
        var asset = Asset.Create("PRICED", "ISIN1", "BVMF", "PRC");
        asset.AddTransaction(Transaction.Create(DateTime.Today, Transaction.TransactionType.Buy, 1m, 10m, 0m));
        asset.SetPrice(DateOnly.FromDateTime(DateTime.Today), 15m, isManual: false);

        SeedActive(asset);

        var report = CreateService().GenerateReport();

        report.UnpricedOpenHoldings.Should().BeEmpty();
    }

    [Fact]
    public void GenerateReport_HistoricHoldingIsNeverReportedAsUnpricedOpen()
    {
        var asset = Asset.Create("CLOSED", "ISIN1", "BVMF", "CLS");
        asset.AddTransaction(Transaction.Create(new DateTime(2021, 1, 1), Transaction.TransactionType.Buy, 5m, 10m, 0m));
        asset.AddTransaction(Transaction.Create(new DateTime(2021, 6, 1), Transaction.TransactionType.Sell, 5m, 12m, 0m));

        SeedHistoric(asset);

        var report = CreateService().GenerateReport();

        report.UnpricedOpenHoldings.Should().BeEmpty();
    }

    [Fact]
    public void GenerateReport_UnclassifiedHolding_NamedAndSplitByScope()
    {
        var active = Asset.Create("ACTIVEUNCLASSIFIED", "ISIN1", "BVMF", "ACU");
        var historic = Asset.Create("HISTORICUNCLASSIFIED", "ISIN2", "BVMF", "HIU");

        SeedActive(active);
        SeedHistoricInto(historic);

        var report = CreateService().GenerateReport();

        using var _ = new AssertionScope();
        report.UnclassifiedHoldings.Should().Contain(f => f.AssetName == "ACTIVEUNCLASSIFIED" && f.Scope == InvestmentScope.Active);
        report.UnclassifiedHoldings.Should().Contain(f => f.AssetName == "HISTORICUNCLASSIFIED" && f.Scope == InvestmentScope.Historic);
    }

    [Fact]
    public void GenerateReport_ClassSetDirectlyWithNoLocalTypeCode_TreatedAsClassified()
    {
        // Bitcoin/BOVA11/IVVB11 precedent (FR-057): a class set directly, with no local type code,
        // is a legitimate classification outcome, not a gap.
        var asset = Asset.Create("Bitcoin", "", "", "BTC", CountryCode.Unknown, "", GlobalAssetClass.Cryptocurrency);

        SeedActive(asset);

        var report = CreateService().GenerateReport();

        report.UnclassifiedHoldings.Should().NotContain(f => f.AssetName == "Bitcoin");
    }

    [Fact]
    public void GenerateReport_UnclassifiedAndUnpriced_LinksTheTwoProblems()
    {
        var linked = Asset.Create("BONDNOTCLASSIFIED", "ISIN1", "BVMF", "BNC");
        linked.AddTransaction(Transaction.Create(DateTime.Today, Transaction.TransactionType.Buy, 1m, 1000m, 0m));

        var unclassifiedButPriced = Asset.Create("PRICEDUNCLASSIFIED", "ISIN2", "BVMF", "PUC");
        unclassifiedButPriced.AddTransaction(Transaction.Create(DateTime.Today, Transaction.TransactionType.Buy, 1m, 100m, 0m));
        unclassifiedButPriced.SetPrice(DateOnly.FromDateTime(DateTime.Today), 100m, isManual: false);

        SeedActive(linked, unclassifiedButPriced);

        var report = CreateService().GenerateReport();

        using var _ = new AssertionScope();
        report.UnclassifiedAndUnpricedOpenHoldings.Should().ContainSingle(f => f.AssetName == "BONDNOTCLASSIFIED");
        report.UnclassifiedAndUnpricedOpenHoldings.Should().NotContain(f => f.AssetName == "PRICEDUNCLASSIFIED");
    }

    [Fact]
    public void GenerateReport_HistoricHoldingStillCarryingQuantity_IsNamedWithCost()
    {
        var asset = Asset.Create("STILLOPENHISTORIC", "ISIN1", "BVMF", "SOH");
        asset.AddTransaction(Transaction.Create(new DateTime(2021, 1, 1), Transaction.TransactionType.Buy, 28m, 75m, 0m));

        SeedHistoric(asset);

        var report = CreateService().GenerateReport();

        using var _ = new AssertionScope();
        report.HistoricHoldingsStillOpen.Should().ContainSingle();
        var finding = report.HistoricHoldingsStillOpen[0];
        finding.AssetName.Should().Be("STILLOPENHISTORIC");
        finding.Quantity.Should().Be(28m);
        finding.CostOfUnitsHeld.Should().Be(2100m);
    }

    [Fact]
    public void GenerateReport_HistoricHoldingFullyClosed_NotReported()
    {
        var asset = Asset.Create("FULLYCLOSED", "ISIN1", "BVMF", "FLC");
        asset.AddTransaction(Transaction.Create(new DateTime(2021, 1, 1), Transaction.TransactionType.Buy, 5m, 10m, 0m));
        asset.AddTransaction(Transaction.Create(new DateTime(2021, 6, 1), Transaction.TransactionType.Sell, 5m, 12m, 0m));

        SeedHistoric(asset);

        var report = CreateService().GenerateReport();

        report.HistoricHoldingsStillOpen.Should().BeEmpty();
    }

    [Fact]
    public void GenerateReport_NeverWritesToTheRepository()
    {
        var asset = Asset.Create("ANY", "ISIN1", "BVMF", "ANY");
        SeedActive(asset);

        CreateService().GenerateReport();

        _repository.WriteCallCount.Should().Be(0);
    }

    [Fact]
    public void GenerateReport_RunTwice_ProducesTheSameResult()
    {
        var oversold = Asset.Create("OVERSOLD", "ISIN1", "BVMF", "OVS");
        oversold.AddTransaction(Transaction.Create(new DateTime(2021, 1, 1), Transaction.TransactionType.Buy, 5m, 10m, 0m));
        oversold.AddTransaction(Transaction.Create(new DateTime(2021, 6, 1), Transaction.TransactionType.Sell, 8m, 12m, 0m));

        var unpriced = Asset.Create("UNPRICED", "ISIN2", "BVMF", "UNP");
        unpriced.AddTransaction(Transaction.Create(DateTime.Today, Transaction.TransactionType.Buy, 1m, 10m, 0m));

        SeedActive(oversold, unpriced);

        var service = CreateService();
        var first = service.GenerateReport();
        var second = service.GenerateReport();

        first.Should().BeEquivalentTo(second);
    }

    [Fact]
    public void GenerateReport_OpenHoldingWithZeroInvestedAmountAndNonzeroMarketValue_IsNamed()
    {
        var asset = Asset.Create("PROVIDERVALUED", "ISIN1", "BVMF", "PRV");
        asset.SetValuationMethod(ValuationMethod.ProviderValue);
        asset.SetPrice(TodayDate, 1200m, isManual: false);

        SeedActive(asset);

        var report = CreateService(new FakeTimeProvider(Today)).GenerateReport();

        report.OpenHoldingsMissingCostBasis.Should().ContainSingle(f => f.AssetName == "PROVIDERVALUED");
    }

    [Fact]
    public void GenerateReport_OpenHoldingWithCostBasisAndPrice_NotReportedAsMissingCostBasis()
    {
        var asset = Asset.Create("PRICED", "ISIN1", "BVMF", "PRC");
        asset.AddTransaction(Transaction.Create(new DateTime(2026, 1, 5), Transaction.TransactionType.Buy, 10m, 20m, 0m));
        asset.SetPrice(TodayDate, 25m, isManual: false);

        SeedActive(asset);

        var report = CreateService(new FakeTimeProvider(Today)).GenerateReport();

        report.OpenHoldingsMissingCostBasis.Should().BeEmpty();
    }

    [Fact]
    public void GenerateReport_UnpricedOpenHolding_NeverAlsoReportedAsMissingCostBasis()
    {
        var asset = Asset.Create("SOLDOUTBUTSTILLACTIVE", "ISIN1", "BVMF", "SBA");
        asset.AddTransaction(Transaction.Create(new DateTime(2026, 1, 5), Transaction.TransactionType.Buy, 4m, 20m, 0m));
        asset.AddTransaction(Transaction.Create(new DateTime(2026, 2, 5), Transaction.TransactionType.Sell, 4m, 25m, 0m));

        SeedActive(asset);

        var report = CreateService(new FakeTimeProvider(Today)).GenerateReport();

        using var _ = new AssertionScope();
        report.UnpricedOpenHoldings.Should().ContainSingle(f => f.AssetName == "SOLDOUTBUTSTILLACTIVE");
        report.OpenHoldingsMissingCostBasis.Should().BeEmpty();
    }

    [Fact]
    public void GenerateReport_HistoricHoldingMissingCostBasis_NeverReported()
    {
        var asset = Asset.Create("PROVIDERVALUEDHISTORIC", "ISIN1", "BVMF", "PVH");
        asset.SetValuationMethod(ValuationMethod.ProviderValue);
        asset.SetPrice(TodayDate, 1200m, isManual: false);

        SeedHistoric(asset);

        var report = CreateService(new FakeTimeProvider(Today)).GenerateReport();

        report.OpenHoldingsMissingCostBasis.Should().BeEmpty();
    }

    [Fact]
    public void GenerateReport_StaleValuationCount_CountsOnlyActiveHoldingsWithStaleMarketStatus()
    {
        var stale = Asset.Create("STALE", "ISIN1", "BVMF", "STL");
        stale.AddTransaction(Transaction.Create(new DateTime(2026, 1, 5), Transaction.TransactionType.Buy, 2m, 20m, 0m));
        stale.SetPrice(TodayDate.AddDays(-10), 22m, isManual: false);

        var current = Asset.Create("CURRENT", "ISIN2", "BVMF", "CUR");
        current.AddTransaction(Transaction.Create(new DateTime(2026, 1, 5), Transaction.TransactionType.Buy, 2m, 20m, 0m));
        current.SetPrice(TodayDate, 22m, isManual: false);

        SeedActive(stale, current);

        var report = CreateService(new FakeTimeProvider(Today)).GenerateReport();

        report.StaleValuationCount.Should().Be(1);
    }

    [Fact]
    public void GenerateReport_StaleValuationCount_ExcludesHistoricHoldings()
    {
        var historic = Asset.Create("STALEHISTORIC", "ISIN1", "BVMF", "STH");
        historic.AddTransaction(Transaction.Create(new DateTime(2026, 1, 5), Transaction.TransactionType.Buy, 2m, 20m, 0m));
        historic.SetPrice(TodayDate.AddDays(-30), 22m, isManual: false);

        SeedHistoric(historic);

        var report = CreateService(new FakeTimeProvider(Today)).GenerateReport();

        report.StaleValuationCount.Should().Be(0);
    }

    [Fact]
    public void GenerateReport_StaleValuationCount_ZeroWhenNoHoldingIsStale()
    {
        var asset = Asset.Create("CURRENT", "ISIN1", "BVMF", "CUR");
        asset.AddTransaction(Transaction.Create(new DateTime(2026, 1, 5), Transaction.TransactionType.Buy, 2m, 20m, 0m));
        asset.SetPrice(TodayDate, 22m, isManual: false);

        SeedActive(asset);

        var report = CreateService(new FakeTimeProvider(Today)).GenerateReport();

        report.StaleValuationCount.Should().Be(0);
    }

    [Fact]
    public void GenerateReport_UnresolvedTaxClassification_IncompleteStatus_IsNamed()
    {
        var asset = Asset.Create("DIVIDENDPAYER", "ISIN1", "BVMF", "DVP");
        SeedActive(asset);
        asset.AddCredit(Credit.Create(new DateTime(2026, 6, 1), Credit.CreditType.Dividend, 100m, 0m, Currency.BRL), _repository.Investments!);

        var report = CreateService().GenerateReport();

        var finding = report.UnresolvedTaxClassifications.Should().ContainSingle().Subject;
        using var _ = new AssertionScope();
        finding.AssetName.Should().Be("DIVIDENDPAYER");
        finding.BrokerName.Should().Be("XPI");
        finding.PortfolioName.Should().Be("Default");
        finding.TaxYear.Should().Be("2026");
        finding.EventCategory.Should().Be(EventCategory.Dividend);
    }

    [Fact]
    public void GenerateReport_TaxClassification_FinalStatus_NotReported()
    {
        var asset = Asset.Create("DIVIDENDPAYER", "ISIN1", "BVMF", "DVP");
        SeedActive(asset);
        _repository.Investments!.CreateTaxRule(
            Jurisdiction.BR, EventCategory.Dividend, "BR dividend rule", "desc", new DateOnly(2026, 1, 1), null);
        asset.AddCredit(Credit.Create(new DateTime(2026, 6, 1), Credit.CreditType.Dividend, 100m, 0m, Currency.BRL), _repository.Investments);

        var report = CreateService().GenerateReport();

        report.UnresolvedTaxClassifications.Should().BeEmpty();
    }

    [Fact]
    public void GenerateReport_SupersededTaxClassification_NeverReported()
    {
        var asset = Asset.Create("DISPOSED", "ISIN1", "BVMF", "DSP");
        SeedActive(asset);
        asset.AddTransaction(Transaction.Create(new DateTime(2021, 1, 1), Transaction.TransactionType.Buy, 10m, 50m, 0m));
        var sell = Transaction.Create(new DateTime(2026, 6, 1), Transaction.TransactionType.Sell, 5m, 100m, 0m);
        asset.RecordTransaction(sell, investments: _repository.Investments);

        var beforeRetraction = CreateService().GenerateReport();
        asset.RetractTransaction(sell.Id, investments: _repository.Investments);
        var afterRetraction = CreateService().GenerateReport();

        using var _ = new AssertionScope();
        beforeRetraction.UnresolvedTaxClassifications.Should().ContainSingle(f => f.EventCategory == EventCategory.CapitalGain);
        afterRetraction.UnresolvedTaxClassifications.Should().BeEmpty();
    }

    [Fact]
    public void GenerateReport_UnresolvedTaxClassification_HistoricHolding_StillReported()
    {
        var asset = Asset.Create("CLOSEDDIVIDENDPAYER", "ISIN1", "BVMF", "CDP");
        SeedHistoric(asset);
        asset.AddCredit(Credit.Create(new DateTime(2026, 6, 1), Credit.CreditType.Dividend, 100m, 0m, Currency.BRL), _repository.Investments!);

        var report = CreateService().GenerateReport();

        report.UnresolvedTaxClassifications.Should().ContainSingle(f => f.AssetName == "CLOSEDDIVIDENDPAYER");
    }

    [Fact]
    public void GenerateReport_AssetWithTwoUnresolvedClassifications_EmitsTwoFindings()
    {
        var asset = Asset.Create("DIVIDENDPAYER", "ISIN1", "BVMF", "DVP");
        SeedActive(asset);
        asset.AddCredit(Credit.Create(new DateTime(2025, 6, 1), Credit.CreditType.Dividend, 100m, 0m, Currency.BRL), _repository.Investments!);
        asset.AddCredit(Credit.Create(new DateTime(2026, 6, 1), Credit.CreditType.Dividend, 120m, 0m, Currency.BRL), _repository.Investments);

        var report = CreateService().GenerateReport();

        using var _ = new AssertionScope();
        report.UnresolvedTaxClassifications.Should().HaveCount(2);
        report.UnresolvedTaxClassifications.Select(f => f.TaxYear).Should().ContainInOrder("2025", "2026");
    }

    [Fact]
    public void GenerateReport_ExistingFiveFields_UnaffectedByNewDependency()
    {
        var oversold = Asset.Create("OVERSOLD", "ISIN1", "BVMF", "OVS");
        oversold.AddTransaction(Transaction.Create(new DateTime(2021, 1, 1), Transaction.TransactionType.Buy, 5m, 10m, 0m));
        oversold.AddTransaction(Transaction.Create(new DateTime(2021, 6, 1), Transaction.TransactionType.Sell, 8m, 12m, 0m));

        var unpriced = Asset.Create("UNPRICED", "ISIN2", "BVMF", "UNP");
        unpriced.AddTransaction(Transaction.Create(new DateTime(2026, 1, 5), Transaction.TransactionType.Buy, 1m, 10m, 0m));

        SeedActive(oversold, unpriced);

        var stillOpenHistoric = Asset.Create("STILLOPENHISTORIC", "ISIN3", "BVMF", "SOH");
        stillOpenHistoric.AddTransaction(Transaction.Create(new DateTime(2021, 1, 1), Transaction.TransactionType.Buy, 28m, 75m, 0m));
        SeedHistoricInto(stillOpenHistoric);

        var report = CreateService(new FakeTimeProvider(Today)).GenerateReport();

        using var _ = new AssertionScope();
        report.SalesExceedPurchases.Should().ContainSingle(f => f.AssetName == "OVERSOLD");
        report.UnpricedOpenHoldings.Should().HaveCount(2);
        report.UnclassifiedHoldings.Should().HaveCount(3);
        report.HistoricHoldingsStillOpen.Should().ContainSingle(f => f.AssetName == "STILLOPENHISTORIC");
        report.UnclassifiedAndUnpricedOpenHoldings.Should().HaveCount(2);
    }

    [Fact]
    public void GenerateReport_CleanPortfolio_ReturnsEveryNewCategoryEmptyOrZero()
    {
        var asset = Asset.Create("CLEAN", "ISIN1", "BVMF", "CLN", CountryCode.BR, "", GlobalAssetClass.Equity);
        asset.AddTransaction(Transaction.Create(new DateTime(2026, 1, 5), Transaction.TransactionType.Buy, 2m, 20m, 0m));
        asset.SetPrice(TodayDate, 22m, isManual: false);

        SeedActive(asset);

        var report = CreateService(new FakeTimeProvider(Today)).GenerateReport();

        using var _ = new AssertionScope();
        report.OpenHoldingsMissingCostBasis.Should().BeEmpty();
        report.StaleValuationCount.Should().Be(0);
        report.UnresolvedTaxClassifications.Should().BeEmpty();
    }

    private void SeedActive(params Asset[] assets)
    {
        _repository.Investments = Investments.Create();
        var broker = Broker.Create("XPI", "BRL");
        var portfolio = broker.CreatePortfolio("Default");
        foreach (var asset in assets)
        {
            portfolio.RegisterAsset(asset);
        }
        _repository.Investments.AddActiveBroker(broker);
    }

    private void SeedHistoric(Asset asset)
    {
        _repository.Investments = Investments.Create();
        var broker = Broker.Create("XPI", "BRL");
        broker.CreatePortfolio("Closed").RegisterAsset(asset);
        _repository.Investments.AddHistoricBroker(broker);
    }

    /// <summary>Adds a Historic asset while an Active broker already exists on <see cref="_repository"/>
    /// (set by an earlier <see cref="SeedActive"/> call in the same test), rather than replacing it.</summary>
    private void SeedHistoricInto(Asset asset)
    {
        var broker = Broker.Create("Avenue", "USD");
        broker.CreatePortfolio("Closed").RegisterAsset(asset);
        _repository.Investments!.AddHistoricBroker(broker);
    }

    private DataQualityReportService CreateService(TimeProvider? timeProvider = null) =>
        new(_repository, _tracer, _logger, TestHoldingValuationService.Create(timeProvider));
}
