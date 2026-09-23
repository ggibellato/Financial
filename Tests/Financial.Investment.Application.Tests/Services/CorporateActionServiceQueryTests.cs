using Financial.Investment.Application.Enums;
using Financial.Investment.Application.Interfaces;
using Financial.Investment.Application.Services;
using Financial.Investment.Domain.Entities;
using Financial.Shared.Abstractions.Currencies;
using Financial.Shared.Abstractions.Observability;
using Financial.TestUtilities;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;

namespace Financial.Investment.Application.Tests.Services;

public class CorporateActionServiceQueryTests
{
    private static readonly ITelemetryTracer Tracer = new RecordingTelemetryTracer();

    private readonly StubInvestmentRepository _repository = new();

    [Fact]
    public void GetCorporateActionsByPortfolio_ReturnsThatPortfoliosCorporateActions()
    {
        var asset = MakeAssetWithSplit("AAAA");
        _repository.AssetsByBrokerPortfolio = [asset];
        _repository.AssetsByBroker = [MakeAssetWithSplit("SHOULD_NOT_APPEAR")];

        var result = CreateService().GetCorporateActionsByPortfolio("XPI", "Default");

        result.Should().ContainSingle(i => i.AssetName == "AAAA" && i.Type == CorporateAction.CorporateActionType.Split);
    }

    [Fact]
    public void GetCorporateActionsByPortfolio_ReturnsEmptyForUnknownPortfolio()
    {
        var result = CreateService().GetCorporateActionsByPortfolio("XPI", "UNKNOWN");

        result.Should().BeEmpty();
    }

    [Theory]
    [InlineData(null, "Default")]
    [InlineData("", "Default")]
    [InlineData("   ", "Default")]
    [InlineData("XPI", null)]
    [InlineData("XPI", "")]
    [InlineData("XPI", "   ")]
    public void GetCorporateActionsByPortfolio_ReturnsEmptyOnNullOrWhitespaceParameters(string? brokerName, string? portfolioName)
    {
        _repository.AssetsByBrokerPortfolio = [MakeAssetWithSplit("AAAA")];

        var result = CreateService().GetCorporateActionsByPortfolio(brokerName!, portfolioName!);

        result.Should().BeEmpty();
    }

    [Fact]
    public void GetCorporateActionsByPortfolio_ForwardsHistoricScopeToRepository()
    {
        CreateService().GetCorporateActionsByPortfolio("XPI", "Default", InvestmentScope.Historic);

        _repository.LastGetAssetsByBrokerPortfolioScope.Should().Be(InvestmentScope.Historic);
    }

    [Fact]
    public void GetCorporateActionsByPortfolio_WhenRepositoryThrowsUnexpectedly_Rethrows()
    {
        _repository.ThrowOnGetAssetsByBrokerPortfolio = new InvalidOperationException("simulated failure");

        Action act = () => CreateService().GetCorporateActionsByPortfolio("XPI", "Default");

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void GetCorporateActionsByPortfolio_OrdersMultipleAssetsByEffectiveDateAscending()
    {
        var assetA = Asset.Create("AAAA", "ISIN1", "B3", "AAAA");
        assetA.AddTransaction(Transaction.Create(new DateTime(2025, 1, 1), Transaction.TransactionType.Buy, 10m, 5m, 0m));
        assetA.RecordCorporateAction(CorporateAction.CreateSplit(new DateTime(2026, 3, 1), 2.0m), CostBasisMethod.AverageCost, Investments.Create());

        var assetB = Asset.Create("BBBB", "ISIN2", "B3", "BBBB");
        assetB.AddTransaction(Transaction.Create(new DateTime(2025, 1, 1), Transaction.TransactionType.Buy, 10m, 5m, 0m));
        assetB.RecordCorporateAction(CorporateAction.CreateSplit(new DateTime(2026, 1, 1), 2.0m), CostBasisMethod.AverageCost, Investments.Create());

        _repository.AssetsByBrokerPortfolio = [assetA, assetB];

        var result = CreateService().GetCorporateActionsByPortfolio("XPI", "Default");

        result.Select(i => i.EffectiveDate).Should().BeInAscendingOrder();
        result[0].AssetName.Should().Be("BBBB");
        result[1].AssetName.Should().Be("AAAA");
    }

    [Fact]
    public void GetCorporateActionsByPortfolio_MergerTargetWithNoMatchingTaxRule_CalculationStatusIsRequiresReview()
    {
        var asset = Asset.Create("XCORP", "ISIN1", "B3", "XCORP");
        var investments = Investments.Create();
        var target = CorporateAction.CreateMergerTarget(
            new DateTime(2026, 4, 1), "Acquired by BigCo", Guid.NewGuid(), "BCIA11", 150m, 2000m);
        asset.RecordCorporateAction(target, CostBasisMethod.AverageCost, investments, Currency.BRL);
        _repository.AssetsByBrokerPortfolio = [asset];

        var result = CreateService().GetCorporateActionsByPortfolio("XPI", "Default");

        var item = result.Should().ContainSingle().Subject;
        item.Type.Should().Be(CorporateAction.CorporateActionType.Merger);
        item.Role.Should().Be(CorporateAction.CorporateActionRole.Target);
        item.LinkedAssetName.Should().Be("BCIA11");
        item.CalculationStatus.Should().Be(CalculationStatus.RequiresReview);
    }

    [Fact]
    public void GetCorporateActionsByPortfolio_Split_LeavesCalculationStatusNull()
    {
        _repository.AssetsByBrokerPortfolio = [MakeAssetWithSplit("AAAA")];

        var result = CreateService().GetCorporateActionsByPortfolio("XPI", "Default");

        result.Should().ContainSingle().Which.CalculationStatus.Should().BeNull("a split never has a linked TaxClassification");
    }

    private CorporateActionService CreateService() => new(_repository, new NavigationService(_repository, TestHoldingValuationService.Create(), Tracer, NullLogger<NavigationService>.Instance), Tracer, NullLogger<CorporateActionService>.Instance);

    private static Asset MakeAssetWithSplit(string name)
    {
        var asset = Asset.Create(name, "ISIN", "BVMF", name);
        asset.AddTransaction(Transaction.Create(new DateTime(2026, 1, 1), Transaction.TransactionType.Buy, 10m, 5m, 0m));
        asset.RecordCorporateAction(CorporateAction.CreateSplit(new DateTime(2026, 3, 1), 2.0m), CostBasisMethod.AverageCost, Investments.Create());
        return asset;
    }
}
