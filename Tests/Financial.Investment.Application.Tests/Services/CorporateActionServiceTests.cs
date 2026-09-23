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

public class CorporateActionServiceTests
{
    private static readonly ITelemetryTracer Tracer = new RecordingTelemetryTracer();

    private readonly StubInvestmentRepository _repository = new()
    {
        Brokers = [Broker.Create("XPI", "BRL")]
    };

    [Fact]
    public void Constructor_WithNullRepository_Throws()
    {
        Action act = () => new CorporateActionService(null!, CreateNavigationService(), Tracer, NullLogger<CorporateActionService>.Instance);
        act.Should().Throw<ArgumentNullException>().WithParameterName("repository");
    }

    [Fact]
    public void Constructor_WithNullNavigationService_Throws()
    {
        Action act = () => new CorporateActionService(_repository, null!, Tracer, NullLogger<CorporateActionService>.Instance);
        act.Should().Throw<ArgumentNullException>().WithParameterName("navigationService");
    }

    [Fact]
    public void Constructor_WithNullTracer_Throws()
    {
        Action act = () => new CorporateActionService(_repository, CreateNavigationService(), null!, NullLogger<CorporateActionService>.Instance);
        act.Should().Throw<ArgumentNullException>().WithParameterName("tracer");
    }

    [Fact]
    public void Constructor_WithNullLogger_Throws()
    {
        Action act = () => new CorporateActionService(_repository, CreateNavigationService(), Tracer, null!);
        act.Should().Throw<ArgumentNullException>().WithParameterName("logger");
    }

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
    }

    [Fact]
    public async Task AddSplitAsync_ValidRequest_RecordsSuccessfulSpan()
    {
        _repository.Asset = MakeAssetWithPosition();
        var tracer = new RecordingTelemetryTracer();
        var service = new CorporateActionService(_repository, CreateNavigationService(), tracer, NullLogger<CorporateActionService>.Instance);

        await service.AddSplitAsync(new CorporateActionSplitCreateDTO
        {
            BrokerName = "XPI",
            PortfolioName = "Default",
            AssetName = "AAAA",
            EffectiveDate = new DateTime(2024, 6, 1),
            RatioFactor = 2.0m
        });

        var span = tracer.Spans.Should().ContainSingle().Which;
        span.Name.Should().Be("Investment.CorporateActionService.AddSplit");
        span.Attributes[TelemetryAttributeKeys.BoundedContext].Should().Be("Investment");
        span.Attributes[TelemetryAttributeKeys.EntityType].Should().Be("CorporateAction");
        span.Attributes[TelemetryAttributeKeys.OperationResult].Should().Be(TelemetryOperationResults.Success);
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
    public async Task AddSplitAsync_InvalidRatioFactor_ThrowsAndWritesNothing()
    {
        _repository.Asset = MakeAssetWithPosition();

        var act = async () => await CreateService().AddSplitAsync(new CorporateActionSplitCreateDTO
        {
            BrokerName = "XPI",
            PortfolioName = "Default",
            AssetName = "AAAA",
            EffectiveDate = new DateTime(2024, 6, 1),
            RatioFactor = 1.0m
        });

        await act.Should().ThrowAsync<ArgumentException>();
        _repository.WriteCallCount.Should().Be(0);
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
    public async Task DeleteCorporateActionAsync_WhenALaterSpecificIdDisposalDependsOnTheSplitLots_ThrowsWithSpecificMessageAndWritesNothing()
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
            .WithMessage("Cannot delete: a later disposal depends on lots created by this split.");
        asset.CorporateActions.Should().ContainSingle();
        _repository.WriteCallCount.Should().Be(0);
    }

    [Fact]
    public async Task AddSplitAsync_WhenRepositoryThrowsUnexpectedly_Rethrows()
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
    }

    [Fact]
    public async Task AddMergerAsync_CreateTargetAssetInline_ClosesSourceAndCarriesConvertedPositionToNewTarget()
    {
        var (broker, portfolio, source) = MakeMergerFixture();
        _repository.Broker = broker;
        _repository.Brokers = [broker];

        var result = await CreateService().AddMergerAsync(new CorporateActionMergerCreateDTO
        {
            BrokerName = "XPI",
            PortfolioName = "Default",
            SourceAssetName = "SRC",
            EffectiveDate = new DateTime(2024, 6, 1),
            ExchangeRatio = 2.0m,
            TargetAssetName = "TGT",
            CreateTargetAssetInline = true
        });

        result.Should().NotBeNull();
        result!.Source!.Quantity.Should().Be(0m, "the source position must fully close");
        result.Target!.Quantity.Should().Be(20m, "10 source units x 2.0 exchange ratio");
        result.Target.AveragePrice.Should().Be(2.5m, "the source's 50 total cost basis carried over 20 received units");

        var targetAsset = portfolio.FindAsset("TGT");
        targetAsset.Should().NotBeNull();
        var classification = targetAsset!.TaxClassifications.Should().ContainSingle().Subject;
        classification.CalculationStatus.Should().Be(CalculationStatus.RequiresReview, "no TaxRule is configured in this fixture");
        classification.SourceType.Should().Be(SourceType.CorporateAction);

        source.CorporateActions.Should().ContainSingle().Which.Role.Should().Be(CorporateAction.CorporateActionRole.Source);
        targetAsset.CorporateActions.Should().ContainSingle().Which.Role.Should().Be(CorporateAction.CorporateActionRole.Target);
        _repository.WriteCallCount.Should().Be(1);
    }

    [Fact]
    public async Task AddMergerAsync_ExistingTargetAsset_IncreasesTargetPositionAndCarriesCostBasis()
    {
        var (broker, portfolio, _) = MakeMergerFixture();
        var target = Asset.Create("TGT", "ISIN2", "BVMF", "TGT");
        target.AddTransaction(Transaction.Create(new DateTime(2023, 1, 1), Transaction.TransactionType.Buy, 10m, 2m, 0m));
        portfolio.RegisterAsset(target);
        _repository.Broker = broker;
        _repository.Brokers = [broker];

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

    [Fact]
    public async Task AddMergerAsync_TargetNameCollidesWithExistingDistinctAsset_ThrowsAndWritesNothing()
    {
        var (broker, portfolio, source) = MakeMergerFixture();
        portfolio.RegisterAsset(Asset.Create("TGT", "ISIN2", "BVMF", "TGT"));
        _repository.Broker = broker;
        _repository.Brokers = [broker];

        var act = async () => await CreateService().AddMergerAsync(new CorporateActionMergerCreateDTO
        {
            BrokerName = "XPI",
            PortfolioName = "Default",
            SourceAssetName = "SRC",
            EffectiveDate = new DateTime(2024, 6, 1),
            ExchangeRatio = 2.0m,
            TargetAssetName = "TGT",
            CreateTargetAssetInline = true
        });

        await act.Should().ThrowAsync<InvestmentRuleViolationException>();
        source.CorporateActions.Should().BeEmpty();
        _repository.WriteCallCount.Should().Be(0);
    }

    [Fact]
    public async Task AddMergerAsync_ZeroQuantitySourceHolding_ThrowsAndWritesNothing()
    {
        var broker = Broker.Create("XPI", "BRL");
        var portfolio = broker.AddPortfolio("Default");
        portfolio.RegisterAsset(Asset.Create("SRC", "ISIN1", "BVMF", "SRC"));
        _repository.Broker = broker;
        _repository.Brokers = [broker];

        var act = async () => await CreateService().AddMergerAsync(new CorporateActionMergerCreateDTO
        {
            BrokerName = "XPI",
            PortfolioName = "Default",
            SourceAssetName = "SRC",
            EffectiveDate = new DateTime(2024, 6, 1),
            ExchangeRatio = 2.0m,
            TargetAssetName = "TGT",
            CreateTargetAssetInline = true
        });

        await act.Should().ThrowAsync<InvestmentRuleViolationException>();
        _repository.WriteCallCount.Should().Be(0);
        portfolio.FindAsset("TGT").Should().BeNull("no target must be created when the source step fails");
    }

    [Fact]
    public async Task AddMergerAsync_SourceAssetNotFound_ThrowsKeyNotFound()
    {
        var broker = Broker.Create("XPI", "BRL");
        broker.AddPortfolio("Default");
        _repository.Broker = broker;
        _repository.Brokers = [broker];

        var act = async () => await CreateService().AddMergerAsync(new CorporateActionMergerCreateDTO
        {
            BrokerName = "XPI",
            PortfolioName = "Default",
            SourceAssetName = "UNKNOWN",
            EffectiveDate = new DateTime(2024, 6, 1),
            ExchangeRatio = 2.0m,
            TargetAssetName = "TGT",
            CreateTargetAssetInline = true
        });

        await act.Should().ThrowAsync<KeyNotFoundException>();
        _repository.WriteCallCount.Should().Be(0);
    }

    [Fact]
    public async Task AddMergerAsync_ValidRequest_RecordsSuccessfulSpan()
    {
        var (broker, _, _) = MakeMergerFixture();
        var tracer = new RecordingTelemetryTracer();
        var service = new CorporateActionService(
            new StubInvestmentRepository { Broker = broker, Brokers = [broker] }, CreateNavigationService(), tracer, NullLogger<CorporateActionService>.Instance);

        await service.AddMergerAsync(new CorporateActionMergerCreateDTO
        {
            BrokerName = "XPI",
            PortfolioName = "Default",
            SourceAssetName = "SRC",
            EffectiveDate = new DateTime(2024, 6, 1),
            ExchangeRatio = 2.0m,
            TargetAssetName = "TGT",
            CreateTargetAssetInline = true
        });

        var span = tracer.Spans.Should().ContainSingle().Which;
        span.Name.Should().Be("Investment.CorporateActionService.AddMerger");
        span.Attributes[TelemetryAttributeKeys.OperationResult].Should().Be(TelemetryOperationResults.Success);
    }

    [Fact]
    public async Task UpdateMergerAsync_UnknownId_ReturnsNull()
    {
        var (broker, _, _) = MakeMergerFixture();
        _repository.Broker = broker;
        _repository.Brokers = [broker];

        var result = await CreateService().UpdateMergerAsync(new CorporateActionMergerUpdateDTO
        {
            BrokerName = "XPI",
            PortfolioName = "Default",
            SourceAssetName = "SRC",
            Id = Guid.NewGuid(),
            EffectiveDate = new DateTime(2024, 6, 1),
            ExchangeRatio = 2.0m
        });

        result.Should().BeNull();
        _repository.WriteCallCount.Should().Be(0);
    }

    [Fact]
    public async Task UpdateMergerAsync_EmptyId_ReturnsNull()
    {
        var (broker, _, _) = MakeMergerFixture();
        _repository.Broker = broker;
        _repository.Brokers = [broker];

        var result = await CreateService().UpdateMergerAsync(new CorporateActionMergerUpdateDTO
        {
            BrokerName = "XPI",
            PortfolioName = "Default",
            SourceAssetName = "SRC",
            Id = Guid.Empty,
            EffectiveDate = new DateTime(2024, 6, 1),
            ExchangeRatio = 2.0m
        });

        result.Should().BeNull();
    }

    [Fact]
    public async Task UpdateMergerAsync_ExistingId_UpdatesBothLinkedRecordsWithNewRatio()
    {
        var (broker, portfolio, source) = MakeMergerFixture();
        _repository.Broker = broker;
        _repository.Brokers = [broker];

        var added = await CreateService().AddMergerAsync(new CorporateActionMergerCreateDTO
        {
            BrokerName = "XPI",
            PortfolioName = "Default",
            SourceAssetName = "SRC",
            EffectiveDate = new DateTime(2024, 6, 1),
            ExchangeRatio = 2.0m,
            TargetAssetName = "TGT",
            CreateTargetAssetInline = true
        });
        added.Should().NotBeNull();
        var sourceActionId = source.CorporateActions.Single().Id;
        var targetActionId = portfolio.FindAsset("TGT")!.CorporateActions.Single().Id;

        var result = await CreateService().UpdateMergerAsync(new CorporateActionMergerUpdateDTO
        {
            BrokerName = "XPI",
            PortfolioName = "Default",
            SourceAssetName = "SRC",
            Id = sourceActionId,
            EffectiveDate = new DateTime(2024, 6, 1),
            ExchangeRatio = 3.0m
        });

        result.Should().NotBeNull();
        result!.Target!.Quantity.Should().Be(30m, "10 source units x the revised 3.0 exchange ratio");
        var targetAsset = portfolio.FindAsset("TGT")!;
        targetAsset.CorporateActions.Should().ContainSingle().Which.Id.Should().Be(
            targetActionId, "the target record's own id must be preserved across the update");
        source.CorporateActions.Should().ContainSingle().Which.Id.Should().Be(sourceActionId, "the source record's id must be preserved across the update");
    }

    [Fact]
    public async Task DeleteCorporateActionAsync_ExistingMergerId_RemovesBothLinkedRecordsAndRestoresSourcePosition()
    {
        var (broker, portfolio, source) = MakeMergerFixture();
        _repository.Broker = broker;
        _repository.Brokers = [broker];

        var added = await CreateService().AddMergerAsync(new CorporateActionMergerCreateDTO
        {
            BrokerName = "XPI",
            PortfolioName = "Default",
            SourceAssetName = "SRC",
            EffectiveDate = new DateTime(2024, 6, 1),
            ExchangeRatio = 2.0m,
            TargetAssetName = "TGT",
            CreateTargetAssetInline = true
        });
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

    private CorporateActionService CreateService() =>
        new(_repository, CreateNavigationService(), Tracer, NullLogger<CorporateActionService>.Instance);

    private NavigationService CreateNavigationService() =>
        new(_repository, TestHoldingValuationService.Create(), Tracer, NullLogger<NavigationService>.Instance);

    private static Asset MakeAssetWithPosition(string name = "AAAA")
    {
        var asset = Asset.Create(name, "ISIN", "BVMF", name);
        asset.AddTransaction(Transaction.Create(new DateTime(2024, 1, 1), Transaction.TransactionType.Buy, 10m, 5m, 0m));
        return asset;
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
}
