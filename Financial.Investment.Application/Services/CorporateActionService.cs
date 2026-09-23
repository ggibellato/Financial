using Financial.Investment.Application.DTOs;
using Financial.Investment.Application.Enums;
using Financial.Investment.Application.Interfaces;
using Financial.Investment.Domain.Entities;
using Financial.Shared.Abstractions.Observability;
using Microsoft.Extensions.Logging;

namespace Financial.Investment.Application.Services;

public sealed class CorporateActionService : ICorporateActionService
{
    private const string EntityType = "CorporateAction";

    private readonly IInvestmentRepository _repository;
    private readonly INavigationService _navigationService;
    private readonly ITelemetryTracer _tracer;
    private readonly ILogger<CorporateActionService> _logger;

    public CorporateActionService(
        IInvestmentRepository repository,
        INavigationService navigationService,
        ITelemetryTracer tracer,
        ILogger<CorporateActionService> logger)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _navigationService = navigationService ?? throw new ArgumentNullException(nameof(navigationService));
        _tracer = tracer ?? throw new ArgumentNullException(nameof(tracer));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<AssetDetailsDTO?> AddSplitAsync(CorporateActionSplitCreateDTO request)
    {
        using var span = StartSpan("AddSplit");
        try
        {
            var result = await AssetMutationHelper.ExecuteAssetMutationAsync(
                _repository,
                _navigationService,
                request.BrokerName,
                request.PortfolioName,
                request.AssetName,
                asset =>
                {
                    var corporateAction = CorporateAction.CreateSplit(request.EffectiveDate, request.RatioFactor, request.Note);
                    var method = ResolveCostBasisMethod(request.BrokerName);
                    asset.RecordCorporateAction(corporateAction, method, _repository.GetInvestments());
                    return true;
                }).ConfigureAwait(false);

            span.MarkSuccess();
            _logger.LogInformation("{Operation} completed", "AddSplit");
            return result;
        }
        catch (Exception ex)
        {
            span.MarkFailed(ex);
            throw;
        }
    }

    public async Task<AssetDetailsDTO?> UpdateSplitAsync(CorporateActionSplitUpdateDTO request)
    {
        using var span = StartSpan("UpdateSplit");
        span.SetAttribute(TelemetryAttributeKeys.EntityId, request.Id.ToString());
        try
        {
            if (request.Id == Guid.Empty)
            {
                span.MarkSuccess();
                _logger.LogInformation("{Operation} completed", "UpdateSplit");
                return null;
            }

            var result = await AssetMutationHelper.ExecuteAssetMutationAsync(
                _repository,
                _navigationService,
                request.BrokerName,
                request.PortfolioName,
                request.AssetName,
                asset =>
                {
                    var updatedCorporateAction = CorporateAction.CreateSplitWithId(request.Id, request.EffectiveDate, request.RatioFactor, request.Note);
                    var method = ResolveCostBasisMethod(request.BrokerName);
                    return asset.ReviseCorporateAction(updatedCorporateAction, method, _repository.GetInvestments());
                }).ConfigureAwait(false);

            span.MarkSuccess();
            _logger.LogInformation("{Operation} completed", "UpdateSplit");
            return result;
        }
        catch (Exception ex)
        {
            span.MarkFailed(ex);
            throw;
        }
    }

    public async Task<AssetDetailsDTO?> DeleteSplitAsync(CorporateActionDeleteDTO request)
    {
        using var span = StartSpan("DeleteSplit");
        span.SetAttribute(TelemetryAttributeKeys.EntityId, request.Id.ToString());
        try
        {
            if (request.Id == Guid.Empty)
            {
                span.MarkSuccess();
                _logger.LogInformation("{Operation} completed", "DeleteSplit");
                return null;
            }

            var result = await AssetMutationHelper.ExecuteAssetMutationAsync(
                _repository,
                _navigationService,
                request.BrokerName,
                request.PortfolioName,
                request.AssetName,
                asset => asset.RetractCorporateAction(request.Id, ResolveCostBasisMethod(request.BrokerName), _repository.GetInvestments())).ConfigureAwait(false);

            span.MarkSuccess();
            _logger.LogInformation("{Operation} completed", "DeleteSplit");
            return result;
        }
        catch (Exception ex)
        {
            span.MarkFailed(ex);
            throw;
        }
    }

    private CostBasisMethod ResolveCostBasisMethod(string brokerName) =>
        _repository.GetBrokerList(InvestmentScope.Active).FirstOrDefault(b => string.Equals(b.Name, brokerName, StringComparison.Ordinal))?.CostBasisMethod
        ?? CostBasisMethod.AverageCost;

    private ITelemetrySpan StartSpan(string operationName)
    {
        _logger.LogInformation("{Operation} started", operationName);
        return _tracer.StartServiceSpan("Investment", nameof(CorporateActionService), operationName, EntityType);
    }
}
