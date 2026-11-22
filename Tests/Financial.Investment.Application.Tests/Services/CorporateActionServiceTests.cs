using Financial.Investment.Application.DTOs;
using Financial.Investment.Application.Interfaces;
using Financial.Investment.Application.Services;
using Financial.Investment.Domain.Entities;
using Financial.Investment.Domain.Exceptions;
using Financial.Investment.Domain.Rules;
using Financial.Shared.Abstractions.Observability;
using Financial.TestUtilities;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;

namespace Financial.Investment.Application.Tests.Services;

[Trait("Category", "Unit")]
public class CorporateActionServiceTests
{
    private readonly RecordingTelemetryTracer _tracer = new();

    private readonly StubInvestmentRepository _repository = new()
    {
        Brokers = [Broker.Create("XPI", "BRL")]
    };

    [Fact]
    public async Task AddSplitAsync_ValidRequest_RecordsSplitAndReturnsAssetDetails()
    {
        var asset = MakeAssetWithPosition();
        _repository.Asset = asset;

        var result = await CreateService().AddSplitAsync(new CorporateActionSplitCreateDTO
        {
            BrokerName = "XPI",
            PortfolioName = "Default",
            AssetName = "AAAA",
            EffectiveDate = new DateTime(2024, 6, 1),
            RatioFactor = 2.0m,
            Note = "2-for-1"
        });

        result.Should().NotBeNull();
        asset.CorporateActions.Should().ContainSingle().Which.RatioFactor.Should().Be(2.0m);
        asset.Quantity.Should().Be(20m);
        _repository.WriteCallCount.Should().Be(1);
        _tracer.ShouldHaveSucceededSpan("Investment.CorporateActionService.AddSplit")
            .Attributes[TelemetryAttributeKeys.EntityType].Should().Be("CorporateAction");
    }

    [Fact]
    public async Task AddSplitAsync_AssetNotFound_ReturnsNull()
    {
        _repository.Asset = null;

        var result = await CreateService().AddSplitAsync(new CorporateActionSplitCreateDTO
        {
            BrokerName = "XPI",
            PortfolioName = "Default",
            AssetName = "UNKNOWN",
            EffectiveDate = new DateTime(2024, 6, 1),
            RatioFactor = 2.0m
        });

        result.Should().BeNull();
    }

    [Fact]
    public async Task AddSplitAsync_BlankAssetName_ReturnsNull()
    {
        _repository.Asset = MakeAssetWithPosition();

        var result = await CreateService().AddSplitAsync(new CorporateActionSplitCreateDTO
        {
            BrokerName = "XPI",
            PortfolioName = "Default",
            AssetName = "",
            EffectiveDate = new DateTime(2024, 6, 1),
            RatioFactor = 2.0m
        });

        result.Should().BeNull();
    }

    [Fact]
    public async Task AddSplitAsync_ZeroQuantityHolding_ThrowsAndWritesNothing()
    {
        _repository.Asset = Asset.Create("AAAA", "ISIN", "BVMF", "AAAA");

        var act = async () => await CreateService().AddSplitAsync(new CorporateActionSplitCreateDTO
        {
            BrokerName = "XPI",
            PortfolioName = "Default",
            AssetName = "AAAA",
            EffectiveDate = new DateTime(2024, 6, 1),
            RatioFactor = 2.0m
        });

        await act.Should().ThrowAsync<InvestmentRuleViolationException>();
        _repository.WriteCallCount.Should().Be(0);
    }

    [Fact]
    public async Task UpdateSplitAsync_EmptyId_ReturnsNull()
    {
        _repository.Asset = MakeAssetWithPosition();

        var result = await CreateService().UpdateSplitAsync(new CorporateActionSplitUpdateDTO
        {
            BrokerName = "XPI",
            PortfolioName = "Default",
            AssetName = "AAAA",
            Id = Guid.Empty,
            EffectiveDate = new DateTime(2024, 6, 1),
            RatioFactor = 2.0m
        });

        result.Should().BeNull();
        _tracer.ShouldHaveSucceededSpan("Investment.CorporateActionService.UpdateSplit");
    }

    [Fact]
    public async Task UpdateSplitAsync_ExistingId_UpdatesAndReturnsAssetDetails()
    {
        var asset = MakeAssetWithPosition();
        var actionId = Guid.NewGuid();
        asset.RecordCorporateAction(CorporateAction.CreateSplitWithId(actionId, new DateTime(2024, 6, 1), 2.0m));
        _repository.Asset = asset;

        var result = await CreateService().UpdateSplitAsync(new CorporateActionSplitUpdateDTO
        {
            BrokerName = "XPI",
            PortfolioName = "Default",
            AssetName = "AAAA",
            Id = actionId,
            EffectiveDate = new DateTime(2024, 6, 1),
            RatioFactor = 4.0m
        });

        result.Should().NotBeNull();
        asset.CorporateActions.Should().ContainSingle().Which.RatioFactor.Should().Be(4.0m);
        asset.Quantity.Should().Be(40m);
        _tracer.ShouldHaveSucceededSpan("Investment.CorporateActionService.UpdateSplit")
            .Attributes[TelemetryAttributeKeys.EntityId].Should().Be(actionId.ToString());
    }

    [Fact]
    public async Task UpdateSplitAsync_UnknownId_ReturnsNull()
    {
        _repository.Asset = MakeAssetWithPosition();

        var result = await CreateService().UpdateSplitAsync(new CorporateActionSplitUpdateDTO
        {
            BrokerName = "XPI",
            PortfolioName = "Default",
            AssetName = "AAAA",
            Id = Guid.NewGuid(),
            EffectiveDate = new DateTime(2024, 6, 1),
            RatioFactor = 2.0m
        });

        result.Should().BeNull();
        _repository.WriteCallCount.Should().Be(0);
    }

    [Fact]
    public async Task DeleteCorporateActionAsync_EmptyId_ReturnsNull()
    {
        _repository.Asset = MakeAssetWithPosition();

        var result = await CreateService().DeleteCorporateActionAsync(new CorporateActionDeleteDTO
        {
            BrokerName = "XPI",
            PortfolioName = "Default",
            AssetName = "AAAA",
            Id = Guid.Empty
        });

        result.Should().BeNull();
        _tracer.ShouldHaveSucceededSpan("Investment.CorporateActionService.DeleteCorporateAction");
    }

    [Fact]
    public async Task DeleteCorporateActionAsync_ExistingSplitId_RemovesAndReturnsAssetDetails()
    {
        var asset = MakeAssetWithPosition();
        var actionId = Guid.NewGuid();
        asset.RecordCorporateAction(CorporateAction.CreateSplitWithId(actionId, new DateTime(2024, 6, 1), 2.0m));
        _repository.Asset = asset;

        var result = await CreateService().DeleteCorporateActionAsync(new CorporateActionDeleteDTO
        {
            BrokerName = "XPI",
            PortfolioName = "Default",
            AssetName = "AAAA",
            Id = actionId
        });

        result.Should().NotBeNull();
        asset.CorporateActions.Should().BeEmpty();
        asset.Quantity.Should().Be(10m);
        _tracer.ShouldHaveSucceededSpan("Investment.CorporateActionService.DeleteCorporateAction")
            .Attributes[TelemetryAttributeKeys.EntityId].Should().Be(actionId.ToString());
    }

    [Fact]
    public async Task DeleteCorporateActionAsync_UnknownId_ReturnsNull()
    {
        _repository.Asset = MakeAssetWithPosition();

        var result = await CreateService().DeleteCorporateActionAsync(new CorporateActionDeleteDTO
        {
            BrokerName = "XPI",
            PortfolioName = "Default",
            AssetName = "AAAA",
            Id = Guid.NewGuid()
        });

        result.Should().BeNull();
        _repository.WriteCallCount.Should().Be(0);
    }

    [Fact]
    public async Task DeleteCorporateActionAsync_WhenALaterSpecificIdSaleDependsOnTheSplitLots_ThrowsWithSpecificMessageAndWritesNothing()
    {
        var specificIdBroker = Broker.Create("XPI", "BRL");
        specificIdBroker.SetCostBasisMethod(CostBasisMethod.SpecificId);
        _repository.Brokers = [specificIdBroker];

        var asset = Asset.Create("AAAA", "ISIN", "BVMF", "AAAA");
        var lot1 = Transaction.Create(new DateTime(2024, 1, 1), Transaction.TransactionType.Buy, 5m, 5m, 0m);
        asset.AddTransaction(lot1);
        var actionId = Guid.NewGuid();
        asset.RecordCorporateAction(CorporateAction.CreateSplitWithId(actionId, new DateTime(2024, 2, 1), 2.0m), CostBasisMethod.SpecificId);
        asset.RecordTransaction(
            Transaction.Create(new DateTime(2024, 3, 1), Transaction.TransactionType.Sell, 10m, 6m, 0m),
            CostBasisMethod.SpecificId,
            [new SpecificLotAllocation(lot1.Id, 10m)]);
        _repository.Asset = asset;

        var act = async () => await CreateService().DeleteCorporateActionAsync(new CorporateActionDeleteDTO
        {
            BrokerName = "XPI",
            PortfolioName = "Default",
            AssetName = "AAAA",
            Id = actionId
        });

        (await act.Should().ThrowAsync<InvestmentRuleViolationException>())
            .WithMessage("Cannot delete: a later sale depends on lots created by this split.");
        asset.CorporateActions.Should().ContainSingle();
        _repository.WriteCallCount.Should().Be(0);
    }

    [Fact]
    public async Task AddSplitAsync_WhenRepositoryThrowsUnexpectedly_RecordsFailedSpanAndRethrows()
    {
        _repository.Asset = MakeAssetWithPosition();
        _repository.ThrowOnApplyAndSaveAsync = new InvalidOperationException("simulated failure");

        var act = async () => await CreateService().AddSplitAsync(new CorporateActionSplitCreateDTO
        {
            BrokerName = "XPI",
            PortfolioName = "Default",
            AssetName = "AAAA",
            EffectiveDate = new DateTime(2024, 6, 1),
            RatioFactor = 2.0m
        });

        await act.Should().ThrowAsync<InvalidOperationException>();
        _tracer.ShouldHaveFailedSpan<InvalidOperationException>("Investment.CorporateActionService.AddSplit");
    }

    [Fact]
    public async Task AddMergerAsync_CreateTargetAssetInline_ClosesSourceAndCarriesConvertedPositionToNewTarget()
    {
        var (_, portfolio, source) = SeedMergerFixture();

        var result = await CreateService().AddMergerAsync(ValidMergerRequest());

        result.Should().NotBeNull();
        result!.Source!.Quantity.Should().Be(0m, "the source position must fully close");
        result.Target!.Quantity.Should().Be(20m, "10 source units x 2.0 exchange ratio");
        result.Target.AveragePrice.Should().Be(2.5m, "the source's 50 total cost basis carried over 20 received units");

        var targetAsset = portfolio.FindAsset("TGT");
        targetAsset.Should().NotBeNull();
        targetAsset!.ISIN.Should().BeEmpty();
        targetAsset.Exchange.Should().BeEmpty();
        targetAsset.Ticker.Should().BeEmpty();
        targetAsset.LocalTypeCode.Should().BeEmpty();
        targetAsset.Country.Should().Be(CountryCode.Unknown);
        var classification = targetAsset.TaxClassifications.Should().ContainSingle().Subject;
        classification.CalculationStatus.Should().Be(CalculationStatus.RequiresReview, "no TaxRule is configured in this fixture");
        classification.SourceType.Should().Be(SourceType.CorporateAction);

        source.CorporateActions.Should().ContainSingle().Which.Role.Should().Be(CorporateAction.CorporateActionRole.Source);
        targetAsset.CorporateActions.Should().ContainSingle().Which.Role.Should().Be(CorporateAction.CorporateActionRole.Target);
        _repository.WriteCallCount.Should().Be(1);
        _tracer.ShouldHaveSucceededSpan("Investment.CorporateActionService.AddMerger");
    }

    [Fact]
    public async Task AddMergerAsync_ExistingTargetAsset_IncreasesTargetPositionAndCarriesCostBasis()
    {
        var (_, portfolio, _) = SeedMergerFixture();
        var target = Asset.Create("TGT", "ISIN2", "BVMF", "TGT");
        target.AddTransaction(Transaction.Create(new DateTime(2023, 1, 1), Transaction.TransactionType.Buy, 10m, 2m, 0m));
        portfolio.RegisterAsset(target);

        var result = await CreateService().AddMergerAsync(new CorporateActionMergerCreateDTO
        {
            BrokerName = "XPI",
            PortfolioName = "Default",
            SourceAssetName = "SRC",
            EffectiveDate = new DateTime(2024, 6, 1),
            ExchangeRatio = 1.0m,
            TargetAssetName = "TGT",
            CreateTargetAssetInline = false
        });

        result.Should().NotBeNull();
        result!.Target!.Quantity.Should().Be(20m, "the existing 10 units plus the 10 converted units");
        result.Target.AveragePrice.Should().Be(3.5m, "(10 x 2 + 50) / 20");
    }

    [Theory]
    [InlineData("TGT")]
    [InlineData("tgt")]
    [InlineData("Tgt")]
    public async Task AddMergerAsync_TargetNameCollidesWithExistingDistinctAssetInAnyCase_ThrowsAndWritesNothing(string targetName)
    {
        var (_, portfolio, source) = SeedMergerFixture();
        portfolio.RegisterAsset(Asset.Create("TGT", "ISIN2", "BVMF", "TGT"));

        var act = async () => await CreateService().AddMergerAsync(ValidMergerRequest(targetAssetName: targetName));

        (await act.Should().ThrowAsync<InvestmentDuplicateNameException>())
            .WithMessage($"An asset named \"{targetName}\" already exists in portfolio \"Default\".");
        source.CorporateActions.Should().BeEmpty();
        _repository.WriteCallCount.Should().Be(0);
    }

    [Fact]
    public async Task AddMergerAsync_ExistingTargetLookedUpInAnotherCase_IsNotFound()
    {
        var (_, portfolio, source) = SeedMergerFixture();
        portfolio.RegisterAsset(Asset.Create("TGT", "ISIN2", "BVMF", "TGT"));

        var act = async () => await CreateService().AddMergerAsync(new CorporateActionMergerCreateDTO
        {
            BrokerName = "XPI",
            PortfolioName = "Default",
            SourceAssetName = "SRC",
            EffectiveDate = new DateTime(2024, 6, 1),
            ExchangeRatio = 2.0m,
            TargetAssetName = "tgt",
            CreateTargetAssetInline = false
        });

        await act.Should().ThrowAsync<KeyNotFoundException>();
        source.CorporateActions.Should().BeEmpty();
        _repository.WriteCallCount.Should().Be(0);
    }

    [Fact]
    public async Task AddMergerAsync_ZeroQuantitySourceHolding_ThrowsAndWritesNothing()
    {
        var broker = Broker.Create("XPI", "BRL");
        var portfolio = broker.AddPortfolio("Default");
        portfolio.RegisterAsset(Asset.Create("SRC", "ISIN1", "BVMF", "SRC"));
        Seed(broker);

        var act = async () => await CreateService().AddMergerAsync(ValidMergerRequest());

        await act.Should().ThrowAsync<InvestmentRuleViolationException>();
        _repository.WriteCallCount.Should().Be(0);
        portfolio.FindAsset("TGT").Should().BeNull("no target must be created when the source step fails");
    }

    [Fact]
    public async Task AddMergerAsync_SourceAssetNotFound_ThrowsKeyNotFound()
    {
        var broker = Broker.Create("XPI", "BRL");
        broker.AddPortfolio("Default");
        Seed(broker);

        var act = async () => await CreateService().AddMergerAsync(ValidMergerRequest(sourceAssetName: "UNKNOWN"));

        await act.Should().ThrowAsync<KeyNotFoundException>();
        _repository.WriteCallCount.Should().Be(0);
        _tracer.ShouldHaveFailedSpan<KeyNotFoundException>("Investment.CorporateActionService.AddMerger");
    }

    [Theory]
    [InlineData("SRC", true)]
    [InlineData(" ", false)]
    [InlineData("SRC", false)]
    public async Task UpdateMergerAsync_EmptyIdBlankSourceOrUnknownId_ReturnsNullAndWritesNothing(string sourceAssetName, bool emptyId)
    {
        SeedMergerFixture();

        var result = await CreateService().UpdateMergerAsync(
            ValidMergerUpdateRequest(emptyId ? Guid.Empty : Guid.NewGuid(), sourceAssetName: sourceAssetName));

        result.Should().BeNull();
        _repository.WriteCallCount.Should().Be(0);
        _tracer.ShouldHaveSucceededSpan("Investment.CorporateActionService.UpdateMerger");
    }

    [Fact]
    public async Task UpdateMergerAsync_ExistingId_UpdatesBothLinkedRecordsWithNewRatio()
    {
        var (_, portfolio, source) = SeedMergerFixture();

        var added = await CreateService().AddMergerAsync(ValidMergerRequest());
        added.Should().NotBeNull();
        var sourceActionId = source.CorporateActions.Single().Id;
        var targetActionId = portfolio.FindAsset("TGT")!.CorporateActions.Single().Id;

        var result = await CreateService().UpdateMergerAsync(ValidMergerUpdateRequest(sourceActionId, exchangeRatio: 3.0m));

        result.Should().NotBeNull();
        result!.Target!.Quantity.Should().Be(30m, "10 source units x the revised 3.0 exchange ratio");
        (result.Target.Quantity * result.Target.AveragePrice).Should().BeApproximately(50m, 0.0001m, "the source's 50 cost basis is carried, not its quantity over its price");
        _repository.WriteCallCount.Should().Be(2);
        var targetAsset = portfolio.FindAsset("TGT")!;
        targetAsset.CorporateActions.Should().ContainSingle().Which.Id.Should().Be(
            targetActionId, "the target record's own id must be preserved across the update");
        source.CorporateActions.Should().ContainSingle().Which.Id.Should().Be(sourceActionId, "the source record's id must be preserved across the update");
        _tracer.ShouldHaveSucceededSpan("Investment.CorporateActionService.UpdateMerger")
            .Attributes[TelemetryAttributeKeys.EntityId].Should().Be(sourceActionId.ToString());
    }

    [Fact]
    public async Task DeleteCorporateActionAsync_ExistingMergerId_RemovesBothLinkedRecordsAndRestoresSourcePosition()
    {
        var (_, portfolio, source) = SeedMergerFixture();

        var added = await CreateService().AddMergerAsync(ValidMergerRequest());
        added.Should().NotBeNull();
        var sourceActionId = source.CorporateActions.Single().Id;

        var result = await CreateService().DeleteCorporateActionAsync(new CorporateActionDeleteDTO
        {
            BrokerName = "XPI",
            PortfolioName = "Default",
            AssetName = "SRC",
            Id = sourceActionId
        });

        result.Should().NotBeNull();
        result!.Quantity.Should().Be(10m, "deleting the merger must restore the source's original position");
        source.CorporateActions.Should().BeEmpty();
        portfolio.FindAsset("TGT")!.CorporateActions.Should().BeEmpty("the linked target record must be removed alongside the source one");
    }

    [Fact]
    public async Task DeleteCorporateActionAsync_MergerWhoseTargetLotsFundALaterSale_RestoresTheSourceRecordAndWritesNothing()
    {
        var (broker, portfolio, source) = SeedMergerFixture();
        broker.SetCostBasisMethod(CostBasisMethod.SpecificId);
        await CreateService().AddMergerAsync(ValidMergerRequest());
        var target = portfolio.FindAsset("TGT")!;
        var targetLotId = target.CorporateActions.Single().Id;
        target.RecordTransaction(
            Transaction.Create(new DateTime(2024, 9, 1), Transaction.TransactionType.Sell, 5m, 4m, 0m),
            CostBasisMethod.SpecificId,
            [new SpecificLotAllocation(targetLotId, 5m)]);
        var sourceActionId = source.CorporateActions.Single().Id;
        var writesBefore = _repository.WriteCallCount;

        var act = async () => await CreateService().DeleteCorporateActionAsync(new CorporateActionDeleteDTO
        {
            BrokerName = "XPI",
            PortfolioName = "Default",
            AssetName = "SRC",
            Id = sourceActionId
        });

        (await act.Should().ThrowAsync<InvestmentRuleViolationException>())
            .WithMessage("Cannot delete: a later sale depends on lots created by this merger.");
        source.CorporateActions.Should().ContainSingle().Which.Id.Should().Be(sourceActionId);
        source.Quantity.Should().Be(0m, "the restored merger still closes the source position");
        target.CorporateActions.Should().ContainSingle();
        _repository.WriteCallCount.Should().Be(writesBefore);
        _tracer.ShouldHaveFailedSpan<InvestmentRuleViolationException>("Investment.CorporateActionService.DeleteCorporateAction");
    }

    [Fact]
    public async Task AddMergerAsync_BrokerCurrencyIsNotRecognised_ThrowsNamingItAndWritesNothing()
    {
        var broker = Broker.Create("XPI", "XYZ");
        var portfolio = broker.AddPortfolio("Default");
        var source = Asset.Create("SRC", "ISIN1", "BVMF", "SRC");
        source.AddTransaction(Transaction.Create(new DateTime(2024, 1, 1), Transaction.TransactionType.Buy, 10m, 5m, 0m));
        portfolio.RegisterAsset(source);
        Seed(broker);

        var act = async () => await CreateService().AddMergerAsync(ValidMergerRequest());

        (await act.Should().ThrowAsync<ArgumentException>()).WithMessage("*XYZ*");
        source.CorporateActions.Should().BeEmpty();
        _repository.WriteCallCount.Should().Be(0);
    }

    [Fact]
    public async Task AddMergerAsync_BlankTargetName_ReturnsNullAndWritesNothing()
    {
        var (_, _, source) = SeedMergerFixture();

        var result = await CreateService().AddMergerAsync(ValidMergerRequest(targetAssetName: " "));

        result.Should().BeNull();
        source.CorporateActions.Should().BeEmpty();
        _repository.WriteCallCount.Should().Be(0);
        _tracer.ShouldHaveSucceededSpan("Investment.CorporateActionService.AddMerger");
    }

    [Fact]
    public async Task AddSpinOffAsync_BlankNewAssetName_ReturnsNullAndWritesNothing()
    {
        var (_, _, parent) = SeedSpinOffFixture();

        var result = await CreateService().AddSpinOffAsync(ValidSpinOffRequest(newAssetName: ""));

        result.Should().BeNull();
        parent.CorporateActions.Should().BeEmpty();
        _repository.WriteCallCount.Should().Be(0);
        _tracer.ShouldHaveSucceededSpan("Investment.CorporateActionService.AddSpinOff");
    }

    [Fact]
    public async Task UpdateSplitAsync_WhenTheSaveFails_RecordsFailedSpanAndRethrows()
    {
        var asset = MakeAssetWithPosition();
        var split = CorporateAction.CreateSplit(new DateTime(2024, 6, 1), 2.0m);
        asset.RecordCorporateAction(split);
        _repository.Asset = asset;
        _repository.ThrowOnApplyAndSaveAsync = new InvalidOperationException("simulated failure");

        var act = async () => await CreateService().UpdateSplitAsync(new CorporateActionSplitUpdateDTO
        {
            Id = split.Id,
            BrokerName = "XPI",
            PortfolioName = "Default",
            AssetName = "AAAA",
            EffectiveDate = split.EffectiveDate,
            RatioFactor = 3.0m
        });

        await act.Should().ThrowAsync<InvalidOperationException>();
        _tracer.ShouldHaveFailedSpan<InvalidOperationException>("Investment.CorporateActionService.UpdateSplit");
    }

    [Fact]
    public async Task UpdateMergerAsync_WhenTheSaveFails_RecordsFailedSpanAndRethrows()
    {
        var (_, _, source) = SeedMergerFixture();
        await CreateService().AddMergerAsync(ValidMergerRequest());
        _repository.ThrowOnApplyAndSaveAsync = new InvalidOperationException("simulated failure");

        var act = async () => await CreateService().UpdateMergerAsync(ValidMergerUpdateRequest(source.CorporateActions.Single().Id, exchangeRatio: 3.0m));

        await act.Should().ThrowAsync<InvalidOperationException>();
        _tracer.ShouldHaveFailedSpan<InvalidOperationException>("Investment.CorporateActionService.UpdateMerger");
    }

    [Fact]
    public async Task AddSpinOffAsync_CreateNewAssetInline_ReducesParentCostBasisAndCreatesNewHoldingAtCarriedUnitCost()
    {
        var (_, portfolio, parent) = SeedSpinOffFixture();

        var result = await CreateService().AddSpinOffAsync(ValidSpinOffRequest());

        result.Should().NotBeNull();
        result!.Parent!.Quantity.Should().Be(10m, "a spin-off never changes the parent's quantity");
        (result.Parent.Quantity * result.Parent.AveragePrice).Should().Be(85m, "15% of the 100 total cost basis (10 x 10) moves to the new asset");
        result.New!.Quantity.Should().Be(5m, "the entered received quantity, not derived from a ratio");
        result.New.AveragePrice.Should().Be(3m, "15 carried cost basis / 5 units received");

        var newAsset = portfolio.FindAsset("NEWCO");
        newAsset.Should().NotBeNull();
        var classification = newAsset!.TaxClassifications.Should().ContainSingle().Subject;
        classification.CalculationStatus.Should().Be(CalculationStatus.RequiresReview, "no TaxRule is configured in this fixture");
        classification.SourceType.Should().Be(SourceType.CorporateAction);

        parent.CorporateActions.Should().ContainSingle().Which.Role.Should().Be(CorporateAction.CorporateActionRole.Parent);
        newAsset.CorporateActions.Should().ContainSingle().Which.Role.Should().Be(CorporateAction.CorporateActionRole.New);
        _repository.WriteCallCount.Should().Be(1);
        _tracer.ShouldHaveSucceededSpan("Investment.CorporateActionService.AddSpinOff");
    }

    [Fact]
    public async Task AddSpinOffAsync_ExistingNewAsset_IncreasesNewAssetPositionAndCarriesCostBasis()
    {
        var (_, portfolio, _) = SeedSpinOffFixture();
        var newAsset = Asset.Create("NEWCO", "ISIN2", "BVMF", "NEWCO");
        newAsset.AddTransaction(Transaction.Create(new DateTime(2023, 1, 1), Transaction.TransactionType.Buy, 10m, 2m, 0m));
        portfolio.RegisterAsset(newAsset);

        var result = await CreateService().AddSpinOffAsync(new CorporateActionSpinOffCreateDTO
        {
            BrokerName = "XPI",
            PortfolioName = "Default",
            ParentAssetName = "PARENT",
            EffectiveDate = new DateTime(2024, 6, 1),
            QuantityReceived = 10m,
            AllocationPercentage = 20m,
            NewAssetName = "NEWCO",
            CreateNewAssetInline = false
        });

        result.Should().NotBeNull();
        result!.New!.Quantity.Should().Be(20m, "the existing 10 units plus the 10 received units");
        result.New.AveragePrice.Should().Be(2m, "(10 x 2 + 20 carried) / 20");
    }

    [Fact]
    public async Task AddSpinOffAsync_ZeroQuantityParentHolding_Succeeds()
    {
        var broker = Broker.Create("XPI", "BRL");
        var portfolio = broker.AddPortfolio("Default");
        portfolio.RegisterAsset(Asset.Create("PARENT", "ISIN1", "BVMF", "PARENT"));
        Seed(broker);

        var result = await CreateService().AddSpinOffAsync(ValidSpinOffRequest(allocationPercentage: 0m));

        result.Should().NotBeNull("unlike Split/Merger, a spin-off is allowed against a zero-quantity parent");
        result!.Parent!.Quantity.Should().Be(0m);
        result.New!.Quantity.Should().Be(5m);
        result.New.AveragePrice.Should().Be(0m, "0% allocation carries zero cost basis to the new asset");
    }

    [Fact]
    public async Task AddSpinOffAsync_NewAssetNameCollidesWithExistingDistinctAsset_ThrowsAndWritesNothing()
    {
        var (_, portfolio, parent) = SeedSpinOffFixture();
        portfolio.RegisterAsset(Asset.Create("NEWCO", "ISIN2", "BVMF", "NEWCO"));

        var act = async () => await CreateService().AddSpinOffAsync(ValidSpinOffRequest());

        await act.Should().ThrowAsync<InvestmentDuplicateNameException>();
        parent.CorporateActions.Should().BeEmpty("the parent record must roll back when the new-asset step fails");
        _repository.WriteCallCount.Should().Be(0);
    }

    [Fact]
    public async Task AddSpinOffAsync_ParentAssetNotFound_ThrowsKeyNotFound()
    {
        var broker = Broker.Create("XPI", "BRL");
        broker.AddPortfolio("Default");
        Seed(broker);

        var act = async () => await CreateService().AddSpinOffAsync(ValidSpinOffRequest(parentAssetName: "UNKNOWN"));

        await act.Should().ThrowAsync<KeyNotFoundException>();
        _repository.WriteCallCount.Should().Be(0);
        _tracer.ShouldHaveFailedSpan<KeyNotFoundException>("Investment.CorporateActionService.AddSpinOff");
    }

    [Theory]
    [InlineData("PARENT", true)]
    [InlineData(" ", false)]
    [InlineData("PARENT", false)]
    public async Task UpdateSpinOffAsync_EmptyIdBlankParentOrUnknownId_ReturnsNullAndWritesNothing(string parentAssetName, bool emptyId)
    {
        SeedSpinOffFixture();

        var result = await CreateService().UpdateSpinOffAsync(
            ValidSpinOffUpdateRequest(emptyId ? Guid.Empty : Guid.NewGuid(), parentAssetName: parentAssetName));

        result.Should().BeNull();
        _repository.WriteCallCount.Should().Be(0);
        _tracer.ShouldHaveSucceededSpan("Investment.CorporateActionService.UpdateSpinOff");
    }

    [Fact]
    public async Task UpdateSpinOffAsync_ExistingId_UpdatesBothLinkedRecordsWithNewAllocation()
    {
        var (_, portfolio, parent) = SeedSpinOffFixture();

        var added = await CreateService().AddSpinOffAsync(ValidSpinOffRequest());
        added.Should().NotBeNull();
        var parentActionId = parent.CorporateActions.Single().Id;
        var newActionId = portfolio.FindAsset("NEWCO")!.CorporateActions.Single().Id;

        var result = await CreateService().UpdateSpinOffAsync(ValidSpinOffUpdateRequest(parentActionId, quantityReceived: 8m, allocationPercentage: 25m));

        result.Should().NotBeNull();
        (result!.Parent!.Quantity * result.Parent.AveragePrice).Should().Be(75m, "75% of the 100 total cost basis remains on the parent after the revised 25% allocation");
        result.New!.Quantity.Should().Be(8m, "the revised entered received quantity");
        _repository.WriteCallCount.Should().Be(2);
        var newAsset = portfolio.FindAsset("NEWCO")!;
        newAsset.CorporateActions.Should().ContainSingle().Which.Id.Should().Be(newActionId, "the new-asset record's own id must be preserved across the update");
        parent.CorporateActions.Should().ContainSingle().Which.Id.Should().Be(parentActionId, "the parent record's id must be preserved across the update");
        _tracer.ShouldHaveSucceededSpan("Investment.CorporateActionService.UpdateSpinOff")
            .Attributes[TelemetryAttributeKeys.EntityId].Should().Be(parentActionId.ToString());
    }

    [Fact]
    public async Task DeleteCorporateActionAsync_ExistingSpinOffId_RemovesBothLinkedRecordsAndRestoresParentPosition()
    {
        var (_, portfolio, parent) = SeedSpinOffFixture();

        var added = await CreateService().AddSpinOffAsync(ValidSpinOffRequest());
        added.Should().NotBeNull();
        var parentActionId = parent.CorporateActions.Single().Id;

        var result = await CreateService().DeleteCorporateActionAsync(new CorporateActionDeleteDTO
        {
            BrokerName = "XPI",
            PortfolioName = "Default",
            AssetName = "PARENT",
            Id = parentActionId
        });

        result.Should().NotBeNull();
        (result!.Quantity * result.AveragePrice).Should().Be(100m, "deleting the spin-off must restore the parent's original cost basis");
        parent.CorporateActions.Should().BeEmpty();
        portfolio.FindAsset("NEWCO")!.CorporateActions.Should().BeEmpty("the linked new-asset record must be removed alongside the parent one");
    }

    [Theory]
    [InlineData(null)]
    [InlineData(GlobalAssetClass.Cryptocurrency)]
    public async Task AddMergerAsync_CreateTargetAssetInline_GivesTheNewTargetTheEnteredIdentity(GlobalAssetClass? enteredClass)
    {
        var (_, portfolio, _) = SeedMergerFixture();
        var request = ValidMergerRequest();
        request.TargetISIN = "BRTGTXACNOR1";
        request.TargetExchange = "BVMF";
        request.TargetTicker = "TGT11";
        request.TargetCountry = CountryCode.BR;
        request.TargetLocalTypeCode = "FII";
        request.TargetClass = enteredClass;

        await CreateService().AddMergerAsync(request);

        var target = portfolio.FindAsset("TGT")!;
        target.ISIN.Should().Be("BRTGTXACNOR1");
        target.Exchange.Should().Be("BVMF");
        target.Ticker.Should().Be("TGT11");
        target.Country.Should().Be(CountryCode.BR);
        target.LocalTypeCode.Should().Be("FII");
        target.Class.Should().Be(enteredClass ?? GlobalAssetClassMapping.Resolve(CountryCode.BR, "FII"));
    }

    [Fact]
    public async Task UpdateMergerAsync_InvalidNewRatio_RestoresBothRecordsAndWritesNothing()
    {
        var (_, portfolio, source) = SeedMergerFixture();
        await CreateService().AddMergerAsync(ValidMergerRequest());
        var sourceActionId = source.CorporateActions.Single().Id;
        var target = portfolio.FindAsset("TGT")!;
        var targetActionId = target.CorporateActions.Single().Id;

        var act = async () => await CreateService().UpdateMergerAsync(ValidMergerUpdateRequest(sourceActionId, exchangeRatio: 0m));

        await act.Should().ThrowAsync<ArgumentException>();
        source.CorporateActions.Should().ContainSingle().Which.Id.Should().Be(sourceActionId);
        target.CorporateActions.Should().ContainSingle().Which.Id.Should().Be(targetActionId);
        target.Quantity.Should().Be(20m);
        _repository.WriteCallCount.Should().Be(1);
        _tracer.ShouldHaveFailedSpan<ArgumentException>("Investment.CorporateActionService.UpdateMerger");
    }

    [Fact]
    public async Task UpdateMergerAsync_TargetFedByTwoMergers_RevisesOnlyTheMatchingTargetRecord()
    {
        var (portfolio, secondSource) = await SeedTargetFedByTwoMergersAsync();

        await CreateService().UpdateMergerAsync(ValidMergerUpdateRequest(secondSource.CorporateActions.Single().Id, sourceAssetName: "SRC2"));

        var target = portfolio.FindAsset("TGT")!;
        target.CorporateActions.Should().HaveCount(2);
        target.Quantity.Should().Be(28m, "20 units from SRC unchanged plus 4 x the revised 2.0 from SRC2");
    }

    [Fact]
    public async Task DeleteCorporateActionAsync_TargetFedByTwoMergers_RemovesOnlyTheMatchingTargetRecord()
    {
        var (portfolio, secondSource) = await SeedTargetFedByTwoMergersAsync();
        _repository.Asset = secondSource;

        await CreateService().DeleteCorporateActionAsync(new CorporateActionDeleteDTO
        {
            BrokerName = "XPI",
            PortfolioName = "Default",
            AssetName = "SRC2",
            Id = secondSource.CorporateActions.Single().Id
        });

        var target = portfolio.FindAsset("TGT")!;
        target.CorporateActions.Should().ContainSingle().Which.LinkedAssetName.Should().Be("SRC");
        target.Quantity.Should().Be(20m);
    }

    [Fact]
    public async Task UpdateSpinOffAsync_InvalidNewAllocation_RestoresBothRecordsAndWritesNothing()
    {
        var (_, portfolio, parent) = SeedSpinOffFixture();
        await CreateService().AddSpinOffAsync(ValidSpinOffRequest());
        var parentActionId = parent.CorporateActions.Single().Id;
        var newAsset = portfolio.FindAsset("NEWCO")!;
        var newActionId = newAsset.CorporateActions.Single().Id;

        var act = async () => await CreateService().UpdateSpinOffAsync(ValidSpinOffUpdateRequest(parentActionId, allocationPercentage: 150m));

        await act.Should().ThrowAsync<ArgumentException>();
        parent.CorporateActions.Should().ContainSingle().Which.Id.Should().Be(parentActionId);
        newAsset.CorporateActions.Should().ContainSingle().Which.Id.Should().Be(newActionId);
        newAsset.Quantity.Should().Be(5m);
        _repository.WriteCallCount.Should().Be(1);
        _tracer.ShouldHaveFailedSpan<ArgumentException>("Investment.CorporateActionService.UpdateSpinOff");
    }

    private async Task<(Portfolio Portfolio, Asset SecondSource)> SeedTargetFedByTwoMergersAsync()
    {
        var (_, portfolio, _) = SeedMergerFixture();
        var secondSource = Asset.Create("SRC2", "ISIN2", "BVMF", "SRC2");
        secondSource.AddTransaction(Transaction.Create(new DateTime(2024, 1, 1), Transaction.TransactionType.Buy, 4m, 5m, 0m));
        portfolio.RegisterAsset(secondSource);
        await CreateService().AddMergerAsync(ValidMergerRequest());
        var secondMerger = ValidMergerRequest();
        secondMerger.SourceAssetName = "SRC2";
        secondMerger.ExchangeRatio = 1.0m;
        secondMerger.CreateTargetAssetInline = false;
        await CreateService().AddMergerAsync(secondMerger);
        return (portfolio, secondSource);
    }

    private CorporateActionService CreateService() =>
        new(_repository, CreateNavigationService(), TestClock.At(), _tracer, NullLogger<CorporateActionService>.Instance);

    private NavigationService CreateNavigationService() =>
        new(_repository, TestHoldingValuationService.Create(), _tracer, NullLogger<NavigationService>.Instance);

    private static CorporateActionMergerCreateDTO ValidMergerRequest(string targetAssetName = "TGT", string sourceAssetName = "SRC") => new()
    {
        BrokerName = "XPI",
        PortfolioName = "Default",
        SourceAssetName = sourceAssetName,
        EffectiveDate = new DateTime(2024, 6, 1),
        ExchangeRatio = 2.0m,
        TargetAssetName = targetAssetName,
        CreateTargetAssetInline = true
    };

    private static CorporateActionSpinOffCreateDTO ValidSpinOffRequest(
        string newAssetName = "NEWCO", string parentAssetName = "PARENT", decimal allocationPercentage = 15m) => new()
    {
        BrokerName = "XPI",
        PortfolioName = "Default",
        ParentAssetName = parentAssetName,
        EffectiveDate = new DateTime(2024, 6, 1),
        QuantityReceived = 5m,
        AllocationPercentage = allocationPercentage,
        NewAssetName = newAssetName,
        CreateNewAssetInline = true
    };

    private static CorporateActionMergerUpdateDTO ValidMergerUpdateRequest(Guid id, decimal exchangeRatio = 2.0m, string sourceAssetName = "SRC") => new()
    {
        BrokerName = "XPI",
        PortfolioName = "Default",
        SourceAssetName = sourceAssetName,
        Id = id,
        EffectiveDate = new DateTime(2024, 6, 1),
        ExchangeRatio = exchangeRatio
    };

    private static CorporateActionSpinOffUpdateDTO ValidSpinOffUpdateRequest(
        Guid id, decimal quantityReceived = 5m, decimal allocationPercentage = 15m, string parentAssetName = "PARENT") => new()
    {
        BrokerName = "XPI",
        PortfolioName = "Default",
        ParentAssetName = parentAssetName,
        Id = id,
        EffectiveDate = new DateTime(2024, 6, 1),
        QuantityReceived = quantityReceived,
        AllocationPercentage = allocationPercentage
    };

    private static Asset MakeAssetWithPosition(string name = "AAAA")
    {
        var asset = Asset.Create(name, "ISIN", "BVMF", name);
        asset.AddTransaction(Transaction.Create(new DateTime(2024, 1, 1), Transaction.TransactionType.Buy, 10m, 5m, 0m));
        return asset;
    }

    private (Broker Broker, Portfolio Portfolio, Asset Source) SeedMergerFixture()
    {
        var fixture = MakeMergerFixture();
        Seed(fixture.Broker);
        return fixture;
    }

    private (Broker Broker, Portfolio Portfolio, Asset Parent) SeedSpinOffFixture()
    {
        var fixture = MakeSpinOffFixture();
        Seed(fixture.Broker);
        return fixture;
    }

    private void Seed(Broker broker)
    {
        _repository.Broker = broker;
        _repository.Brokers = [broker];
    }

    private static (Broker Broker, Portfolio Portfolio, Asset Source) MakeMergerFixture(decimal sourceQuantity = 10m, decimal sourceUnitPrice = 5m)
    {
        var broker = Broker.Create("XPI", "BRL");
        var portfolio = broker.AddPortfolio("Default");
        var source = Asset.Create("SRC", "ISIN1", "BVMF", "SRC");
        source.AddTransaction(Transaction.Create(new DateTime(2024, 1, 1), Transaction.TransactionType.Buy, sourceQuantity, sourceUnitPrice, 0m));
        portfolio.RegisterAsset(source);
        return (broker, portfolio, source);
    }

    private static (Broker Broker, Portfolio Portfolio, Asset Parent) MakeSpinOffFixture(decimal parentQuantity = 10m, decimal parentUnitPrice = 10m)
    {
        var broker = Broker.Create("XPI", "BRL");
        var portfolio = broker.AddPortfolio("Default");
        var parent = Asset.Create("PARENT", "ISIN1", "BVMF", "PARENT");
        parent.AddTransaction(Transaction.Create(new DateTime(2024, 1, 1), Transaction.TransactionType.Buy, parentQuantity, parentUnitPrice, 0m));
        portfolio.RegisterAsset(parent);
        return (broker, portfolio, parent);
    }
}
