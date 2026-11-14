using Financial.Investment.Application.DTOs;
using Financial.Investment.Application.Services;
using Financial.Investment.Domain.Entities;
using Financial.TestUtilities;
using FluentAssertions;
using FluentAssertions.Execution;
using Xunit;

namespace Financial.Investment.Application.Tests.Services;

[Trait("Category", "Unit")]
public class UpcomingIncomeServiceTests
{
    private static readonly DateTime PurchaseDate = new(2024, 12, 1);
    private static readonly DateTime SaleDate = new(2025, 10, 1);

    private readonly StubInvestmentRepository _repository = new();
    private readonly RecordingTelemetryTracer _tracer = new();
    private readonly RecordingLogger<UpcomingIncomeService> _logger = new();

    [Theory]
    [InlineData(1)]
    [InlineData(3)]
    [InlineData(4)]
    public void GetUpcomingIncome_RegularPayer_ProjectsOneIntervalAfterTheLastCredit(int intervalMonths)
    {
        var firstCredit = new DateTime(2025, 1, 15);
        var lastCredit = firstCredit.AddMonths(2 * intervalMonths);
        SeedActive(MakeBroker("Alpha", PayerOn("A1", firstCredit, firstCredit.AddMonths(intervalMonths), lastCredit)));

        var result = CreateService().GetUpcomingIncome();

        using var _ = new AssertionScope();
        var entry = result.Should().ContainSingle().Subject;
        entry.AssetName.Should().Be("A1");
        entry.BrokerName.Should().Be("Alpha");
        entry.LastCreditDate.Should().Be(lastCredit);
        entry.ProjectedNextDate.Should().Be(lastCredit.AddMonths(intervalMonths));
        _tracer.ShouldHaveSucceededSpan("Investment.UpcomingIncomeService.GetUpcomingIncome");
    }

    [Theory]
    [InlineData(new int[0])]
    [InlineData(new[] { 0 })]
    [InlineData(new[] { 0, 7 })]
    public void GetUpcomingIncome_NoDetectableFrequency_OmittedFromResult(int[] creditMonthOffsets)
    {
        var creditDates = creditMonthOffsets.Select(offset => new DateTime(2025, 1, 15).AddMonths(offset)).ToArray();
        SeedActive(MakeBroker("Alpha", PayerOn("A1", creditDates)));

        var result = CreateService().GetUpcomingIncome();

        result.Should().BeEmpty();
    }

    [Fact]
    public void GetUpcomingIncome_ProjectedAmount_MatchesMostRecentCreditNetAmount()
    {
        var asset = BoughtAsset("A1");
        asset.AddCredit(Credit.Create(new DateTime(2025, 2, 15), Credit.CreditType.Dividend, 200m, withheld: 30m));
        asset.AddCredit(Credit.Create(new DateTime(2025, 1, 15), Credit.CreditType.Dividend, 100m));
        SeedActive(MakeBroker("Alpha", asset));

        var result = CreateService().GetUpcomingIncome();

        result.Should().ContainSingle().Which.ProjectedAmount.Should().Be(170m);
    }

    [Fact]
    public void GetUpcomingIncome_HistoricHolding_NeverProjected()
    {
        var investments = Investments.Create();
        investments.AddActiveBroker(MakeBroker("Alpha", MonthlyPayer("A1")));
        investments.AddHistoricBroker(MakeBroker("Beta", ClosedMonthlyPayer("B1")));
        _repository.Investments = investments;

        var result = CreateService().GetUpcomingIncome();

        using var _ = new AssertionScope();
        result.Should().ContainSingle();
        result.Should().NotContain(entry => entry.AssetName == "B1");
    }

    [Fact]
    public void GetUpcomingIncome_UnpricedActiveHolding_StillProjected()
    {
        var asset = MonthlyPayer("A1");
        SeedActive(MakeBroker("Alpha", asset));

        var result = CreateService().GetUpcomingIncome();

        using var _ = new AssertionScope();
        asset.PriceSnapshots.Should().BeEmpty();
        result.Should().ContainSingle(entry => entry.AssetName == "A1");
    }

    [Fact]
    public void GetUpcomingIncome_SortedAscendingByProjectedNextDate_ThenByAssetName()
    {
        SeedActive(MakeBroker(
            "Alpha",
            PayerOn("ZED", new DateTime(2025, 1, 10), new DateTime(2025, 2, 10)),
            PayerOn("MID", new DateTime(2025, 3, 10), new DateTime(2025, 4, 10)),
            PayerOn("alpha", new DateTime(2025, 1, 10), new DateTime(2025, 2, 10)),
            PayerOn("EARLY", new DateTime(2024, 11, 10), new DateTime(2024, 12, 10))));

        var result = CreateService().GetUpcomingIncome();

        result.Select(entry => entry.AssetName).Should().ContainInOrder("EARLY", "alpha", "ZED", "MID");
    }

    [Fact]
    public void GetUpcomingIncome_WhenRepositoryThrows_RecordsFailedSpanAndRethrows()
    {
        _repository.ThrowOnGetInvestments = new InvalidOperationException("simulated failure");

        Action act = () => CreateService().GetUpcomingIncome();

        act.Should().Throw<InvalidOperationException>();
        using var _ = new AssertionScope();
        _tracer.ShouldHaveFailedSpan<InvalidOperationException>("Investment.UpcomingIncomeService.GetUpcomingIncome");
        _logger.Entries.Should().NotContain(entry => entry.Message.Contains("simulated failure"));
    }

    private UpcomingIncomeService CreateService() => new(_repository, _tracer, _logger);

    private void SeedActive(params Broker[] brokers)
    {
        var investments = Investments.Create();
        foreach (var broker in brokers)
        {
            investments.AddActiveBroker(broker);
        }

        _repository.Investments = investments;
    }

    private static Broker MakeBroker(string name, params Asset[] assets)
    {
        var broker = Broker.Create(name, "GBP");
        var portfolio = broker.AddPortfolio("Default");
        foreach (var asset in assets)
        {
            portfolio.AddAsset(asset);
        }

        return broker;
    }

    private static Asset BoughtAsset(string name)
    {
        var asset = Asset.Create(name, $"ISIN-{name}", "BVMF", name);
        asset.AddTransaction(Transaction.Create(PurchaseDate, Transaction.TransactionType.Buy, 10m, 5m, 0m));
        return asset;
    }

    private static Asset PayerOn(string name, params DateTime[] creditDates)
    {
        var asset = BoughtAsset(name);
        foreach (var creditDate in creditDates)
        {
            asset.AddCredit(Credit.Create(creditDate, Credit.CreditType.Dividend, 12m));
        }

        return asset;
    }

    private static Asset MonthlyPayer(string name) =>
        PayerOn(name, new DateTime(2025, 1, 15), new DateTime(2025, 2, 15), new DateTime(2025, 3, 15));

    private static Asset ClosedMonthlyPayer(string name)
    {
        var asset = MonthlyPayer(name);
        asset.AddTransaction(Transaction.Create(SaleDate, Transaction.TransactionType.Sell, 10m, 6m, 0m));
        return asset;
    }
}
