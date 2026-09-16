using Financial.Investment.Application.Services;
using Financial.Investment.Domain.Entities;
using FluentAssertions;
using FluentAssertions.Execution;
using Xunit;

namespace Financial.Investment.Application.Tests.Services;

public class UpcomingIncomeBuilderTests
{
    private static readonly DateTime PurchaseDate = new(2024, 12, 1);

    [Fact]
    public void TryBuildEntry_MonthlyFrequency_AddsOneMonth()
    {
        var asset = PayerOn("A1", new DateTime(2025, 1, 15), new DateTime(2025, 2, 15), new DateTime(2025, 3, 15));

        var entry = UpcomingIncomeBuilder.TryBuildEntry(asset, "Alpha");

        entry!.ProjectedNextDate.Should().Be(new DateTime(2025, 4, 15));
    }

    [Fact]
    public void TryBuildEntry_QuarterlyFrequency_AddsThreeMonths()
    {
        var asset = PayerOn("A1", new DateTime(2025, 1, 15), new DateTime(2025, 4, 15), new DateTime(2025, 7, 15));

        var entry = UpcomingIncomeBuilder.TryBuildEntry(asset, "Alpha");

        entry!.ProjectedNextDate.Should().Be(new DateTime(2025, 10, 15));
    }

    [Fact]
    public void TryBuildEntry_FourMonthlyFrequency_AddsFourMonths()
    {
        var asset = PayerOn("A1", new DateTime(2025, 1, 15), new DateTime(2025, 5, 15), new DateTime(2025, 9, 15));

        var entry = UpcomingIncomeBuilder.TryBuildEntry(asset, "Alpha");

        entry!.ProjectedNextDate.Should().Be(new DateTime(2026, 1, 15));
    }

    [Fact]
    public void TryBuildEntry_NoDetectableFrequency_ReturnsNull()
    {
        var singleCredit = PayerOn("A1", new DateTime(2025, 1, 15));
        var tooLargeGap = PayerOn("A2", new DateTime(2025, 1, 15), new DateTime(2025, 8, 15));

        using var _ = new AssertionScope();
        UpcomingIncomeBuilder.TryBuildEntry(singleCredit, "Alpha").Should().BeNull();
        UpcomingIncomeBuilder.TryBuildEntry(tooLargeGap, "Alpha").Should().BeNull();
    }

    [Fact]
    public void TryBuildEntry_MultipleCredits_SelectsMaxDateCreditForBothFields()
    {
        var asset = BoughtAsset("A1");
        asset.AddCredit(Credit.Create(new DateTime(2025, 2, 15), Credit.CreditType.Dividend, 500m));
        asset.AddCredit(Credit.Create(new DateTime(2025, 3, 15), Credit.CreditType.Dividend, 40m));
        asset.AddCredit(Credit.Create(new DateTime(2025, 1, 15), Credit.CreditType.Dividend, 900m));

        var entry = UpcomingIncomeBuilder.TryBuildEntry(asset, "Alpha");

        using var _ = new AssertionScope();
        entry!.LastCreditDate.Should().Be(new DateTime(2025, 3, 15));
        entry.ProjectedAmount.Should().Be(40m);
        entry.AssetName.Should().Be("A1");
        entry.BrokerName.Should().Be("Alpha");
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
}
