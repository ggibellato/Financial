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
    public async Task DeleteSplitAsync_EmptyId_ReturnsNull()
    {
        _repository.Asset = MakeAssetWithPosition();

        var result = await CreateService().DeleteSplitAsync(new CorporateActionDeleteDTO
        {
            BrokerName = "XPI",
            PortfolioName = "Default",
            AssetName = "AAAA",
            Id = Guid.Empty
        });

        result.Should().BeNull();
    }

    [Fact]
    public async Task DeleteSplitAsync_ExistingId_RemovesAndReturnsAssetDetails()
    {
        var asset = MakeAssetWithPosition();
        var actionId = Guid.NewGuid();
        asset.RecordCorporateAction(CorporateAction.CreateSplitWithId(actionId, new DateTime(2024, 6, 1), 2.0m));
        _repository.Asset = asset;

        var result = await CreateService().DeleteSplitAsync(new CorporateActionDeleteDTO
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
    public async Task DeleteSplitAsync_UnknownId_ReturnsNull()
    {
        _repository.Asset = MakeAssetWithPosition();

        var result = await CreateService().DeleteSplitAsync(new CorporateActionDeleteDTO
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
    public async Task DeleteSplitAsync_WhenALaterSpecificIdDisposalDependsOnTheSplitLots_ThrowsWithSpecificMessageAndWritesNothing()
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

        var act = async () => await CreateService().DeleteSplitAsync(new CorporateActionDeleteDTO
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
}
