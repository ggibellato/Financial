using Financial.Investment.Application.Enums;
using Financial.Investment.Application.Services;
using Financial.Investment.Domain.Entities;
using Financial.Shared.Abstractions.Observability;
using Financial.TestUtilities;
using FluentAssertions;
using FluentAssertions.Execution;

namespace Financial.Investment.Application.Tests.Services;

public class DataQualityReportServiceTests
{
    private readonly StubInvestmentRepository _repository = new();
    private readonly RecordingTelemetryTracer _tracer = new();
    private readonly RecordingLogger<DataQualityReportService> _logger = new();

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

    private DataQualityReportService CreateService() => new(_repository, _tracer, _logger);
}
