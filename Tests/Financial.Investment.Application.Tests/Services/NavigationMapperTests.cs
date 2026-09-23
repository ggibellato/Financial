using Financial.Investment.Application.Services;
using Financial.Investment.Domain.Entities;
using Financial.Shared.Abstractions.Currencies;
using FluentAssertions;
using FluentAssertions.Execution;

namespace Financial.Investment.Application.Tests.Services;

public class NavigationMapperTests
{
    [Fact]
    public void MapTransaction_WithFxRateSnapshot_MapsCurrencyAndSnapshot()
    {
        var retrievedAt = new DateTimeOffset(2026, 7, 1, 8, 0, 0, TimeSpan.Zero);
        var snapshot = FxRateSnapshot.Create(Currency.GBP, 0.146m, FxRateSource.Frankfurter, retrievedAt);
        var transaction = Transaction.Create(
            new DateTime(2026, 7, 1), Transaction.TransactionType.Buy, 10m, 9.99m, 0m,
            currency: Currency.BRL, fxRateSnapshot: snapshot);

        var dto = NavigationMapper.MapTransaction(transaction);

        dto.Currency.Should().Be("BRL");
        dto.FxRateSnapshot.Should().NotBeNull();
        dto.FxRateSnapshot!.ToCurrency.Should().Be("GBP");
        dto.FxRateSnapshot.Rate.Should().Be(0.146m);
        dto.FxRateSnapshot.Source.Should().Be("Frankfurter");
        dto.FxRateSnapshot.RetrievedAt.Should().Be(retrievedAt);
    }

    [Fact]
    public void MapTransaction_WithoutFxRateSnapshot_MapsNullSnapshot()
    {
        var transaction = Transaction.Create(new DateTime(2026, 7, 1), Transaction.TransactionType.Buy, 10m, 9.99m, 0m, currency: Currency.GBP);

        var dto = NavigationMapper.MapTransaction(transaction);

        dto.Currency.Should().Be("GBP");
        dto.FxRateSnapshot.Should().BeNull();
    }

    [Fact]
    public void MapCredit_WithFxRateSnapshot_MapsCurrencyAndSnapshot()
    {
        var retrievedAt = new DateTimeOffset(2026, 7, 1, 8, 0, 0, TimeSpan.Zero);
        var snapshot = FxRateSnapshot.Create(Currency.GBP, 0.146m, FxRateSource.Frankfurter, retrievedAt);
        var credit = Credit.Create(
            new DateTime(2026, 7, 1), Credit.CreditType.Dividend, 100m,
            currency: Currency.BRL, fxRateSnapshot: snapshot);
        var asset = Asset.Create("PETR4", "ISIN1", "B3", "PETR4");

        var dto = NavigationMapper.MapCredit(credit, asset);

        dto.Currency.Should().Be("BRL");
        dto.FxRateSnapshot.Should().NotBeNull();
        dto.FxRateSnapshot!.ToCurrency.Should().Be("GBP");
        dto.FxRateSnapshot.Rate.Should().Be(0.146m);
        dto.FxRateSnapshot.Source.Should().Be("Frankfurter");
        dto.FxRateSnapshot.RetrievedAt.Should().Be(retrievedAt);
    }

    [Fact]
    public void MapCredit_WithoutFxRateSnapshot_MapsNullSnapshot()
    {
        var credit = Credit.Create(new DateTime(2026, 7, 1), Credit.CreditType.Dividend, 100m, currency: Currency.GBP);
        var asset = Asset.Create("PETR4", "ISIN1", "B3", "PETR4");

        var dto = NavigationMapper.MapCredit(credit, asset);

        dto.Currency.Should().Be("GBP");
        dto.FxRateSnapshot.Should().BeNull();
    }

    [Fact]
    public void MapCredit_WithIntermediationFee_MapsItAndNetAmountSubtractsIt()
    {
        var credit = Credit.Create(new DateTime(2026, 7, 1), Credit.CreditType.SecuritiesLendingIncome, 0.16m, withheld: 0.03m, intermediationFee: 0.04m);
        var asset = Asset.Create("PETR4", "ISIN1", "B3", "PETR4");

        var dto = NavigationMapper.MapCredit(credit, asset);

        dto.IntermediationFee.Should().Be(0.04m);
        dto.NetAmount.Should().Be(0.09m);
    }

    [Fact]
    public void MapCredit_WithoutSharesForDividend_LeavesAttributionFieldsNull()
    {
        var credit = Credit.Create(new DateTime(2026, 7, 1), Credit.CreditType.Dividend, 100m);
        var asset = Asset.Create("PETR4", "ISIN1", "B3", "PETR4");

        var dto = NavigationMapper.MapCredit(credit, asset);

        dto.SharesForDividend.Should().BeNull();
        dto.AttributedShares.Should().BeNull();
        dto.InvestedAmount.Should().BeNull();
        dto.YieldOnInvested.Should().BeNull();
        dto.YieldOnMarket.Should().BeNull();
    }

    [Fact]
    public void MapCredit_WithSharesForDividend_ComputesInvestedAmountAndYield()
    {
        var asset = Asset.Create("PETR4", "ISIN1", "B3", "PETR4");
        asset.AddTransaction(Transaction.Create(new DateTime(2026, 1, 1), Transaction.TransactionType.Buy, 1000m, 9m, 0m));
        var credit = Credit.Create(new DateTime(2026, 6, 1), Credit.CreditType.Dividend, 400m, sharesForDividend: 800m);

        var dto = NavigationMapper.MapCredit(credit, asset);

        dto.SharesForDividend.Should().Be(800m);
        dto.AttributedShares.Should().Be(800m);
        dto.AverageCostPerShare.Should().Be(9m);
        dto.InvestedAmount.Should().Be(7200m);
        dto.YieldOnInvested.Should().BeApproximately(5.5556m, 0.0001m);
    }

    [Fact]
    public void MapCredit_WithoutSharesForDividendButWithAnOpenPosition_AttributesToTheEntirePosition()
    {
        var asset = Asset.Create("PETR4", "ISIN1", "B3", "PETR4");
        asset.AddTransaction(Transaction.Create(new DateTime(2026, 1, 1), Transaction.TransactionType.Buy, 1000m, 9m, 0m));
        var credit = Credit.Create(new DateTime(2026, 6, 1), Credit.CreditType.Dividend, 400m);

        var dto = NavigationMapper.MapCredit(credit, asset);

        dto.SharesForDividend.Should().BeNull("the user left it blank, so the raw entered value must stay null");
        dto.AttributedShares.Should().Be(1000m);
        dto.InvestedAmount.Should().Be(9000m);
        dto.YieldOnInvested.Should().BeApproximately(4.4444m, 0.0001m);
    }

    [Fact]
    public void MapCorporateAction_Split_MapsFieldsAndLeavesCalculationStatusNull()
    {
        var asset = Asset.Create("PETR4", "ISIN1", "B3", "PETR4");
        var split = CorporateAction.CreateSplit(new DateTime(2026, 3, 1), 2.0m, "2-for-1 split");

        var dto = NavigationMapper.MapCorporateAction(split, asset);

        using var _ = new AssertionScope();
        dto.Id.Should().Be(split.Id);
        dto.Type.Should().Be(CorporateAction.CorporateActionType.Split);
        dto.EffectiveDate.Should().Be(new DateTime(2026, 3, 1));
        dto.RatioFactor.Should().Be(2.0m);
        dto.Note.Should().Be("2-for-1 split");
        dto.Role.Should().BeNull();
        dto.CalculationStatus.Should().BeNull("a split never has a linked TaxClassification");
    }

    [Fact]
    public void MapCorporateAction_MergerTargetWithNoMatchingTaxRule_CalculationStatusIsRequiresReview()
    {
        var asset = Asset.Create("XCORP", "ISIN1", "B3", "XCORP");
        var investments = Investments.Create();
        var correlationId = Guid.NewGuid();
        var target = CorporateAction.CreateMergerTarget(
            new DateTime(2026, 4, 1), "Acquired by BigCo", correlationId, "BCIA11", 150m, 2000m);
        asset.RecordCorporateAction(target, CostBasisMethod.AverageCost, investments, Currency.BRL);

        var dto = NavigationMapper.MapCorporateAction(target, asset);

        using var _ = new AssertionScope();
        dto.Type.Should().Be(CorporateAction.CorporateActionType.Merger);
        dto.Role.Should().Be(CorporateAction.CorporateActionRole.Target);
        dto.LinkedAssetName.Should().Be("BCIA11");
        dto.ConvertedQuantity.Should().Be(150m);
        dto.CarriedCostBasis.Should().Be(2000m);
        dto.CalculationStatus.Should().Be(CalculationStatus.RequiresReview);
    }

    [Fact]
    public void MapCorporateAction_SpinOffNewWithMatchingTaxRule_CalculationStatusIsFinal()
    {
        var asset = Asset.Create("SPINCO", "ISIN1", "B3", "SPINCO");
        var investments = Investments.Create();
        investments.CreateTaxRule(Jurisdiction.BR, EventCategory.CorporateAction, "BR corporate action rule", "desc", new DateOnly(2026, 1, 1), null);
        var correlationId = Guid.NewGuid();
        var newAssetRecord = CorporateAction.CreateSpinOffNew(
            new DateTime(2026, 4, 1), "Spin-off", correlationId, "PARENT", 5m, 120m);
        asset.RecordCorporateAction(newAssetRecord, CostBasisMethod.AverageCost, investments, Currency.BRL);

        var dto = NavigationMapper.MapCorporateAction(newAssetRecord, asset);

        dto.CalculationStatus.Should().Be(CalculationStatus.Final);
    }

    [Fact]
    public void MapCorporateAction_LinkedClassificationSuperseded_CalculationStatusIsNull()
    {
        var asset = Asset.Create("XCORP", "ISIN1", "B3", "XCORP");
        var investments = Investments.Create();
        var correlationId = Guid.NewGuid();
        var target = CorporateAction.CreateMergerTarget(
            new DateTime(2026, 4, 1), "Acquired by BigCo", correlationId, "BCIA11", 150m, 2000m);
        asset.RecordCorporateAction(target, CostBasisMethod.AverageCost, investments, Currency.BRL);
        asset.RetractCorporateAction(target.Id, CostBasisMethod.AverageCost, investments);

        var dto = NavigationMapper.MapCorporateAction(target, asset);

        dto.CalculationStatus.Should().BeNull("only the active classification is matched, and retraction superseded it with no replacement");
    }
}
