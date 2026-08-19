using Financial.Investment.Application.DTOs;
using Financial.Investment.Application.Enums;
using Financial.Investment.Application.Interfaces;
using Financial.Investment.Application.Validation;
using Financial.Investment.Domain.Entities;
using Financial.Shared.Abstractions;
using Microsoft.Extensions.Logging;

namespace Financial.Investment.Application.Services;

public sealed class CreditService : ICreditService, ICreditQueryService
{
    private const string EntityType = "Credit";

    private readonly IInvestmentRepository _repository;
    private readonly INavigationService _navigationService;
    private readonly ITelemetryTracer _tracer;
    private readonly ILogger<CreditService> _logger;

    public CreditService(IInvestmentRepository repository, INavigationService navigationService, ITelemetryTracer tracer, ILogger<CreditService> logger)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _navigationService = navigationService ?? throw new ArgumentNullException(nameof(navigationService));
        _tracer = tracer ?? throw new ArgumentNullException(nameof(tracer));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<AssetDetailsDTO?> AddCreditAsync(CreditCreateDTO request)
    {
        using var span = StartSpan("AddCredit");
        try
        {
            var result = await AssetMutationHelper.ExecuteParsedMutationAsync<Credit.CreditType>(
                _repository,
                _navigationService,
                request.BrokerName,
                request.PortfolioName,
                request.AssetName,
                request.Type,
                CreditTypeParser.TryParse,
                (asset, creditType) =>
                {
                    var credit = Credit.Create(request.Date, creditType, request.Value);
                    asset.AddCredit(credit);
                    return true;
                }).ConfigureAwait(false);

            span.SetAttribute(TelemetryAttributeKeys.OperationResult, TelemetryOperationResults.Success);
            _logger.LogInformation("{Operation} completed", "AddCredit");
            return result;
        }
        catch (Exception ex)
        {
            span.SetAttribute(TelemetryAttributeKeys.OperationResult, TelemetryOperationResults.Failed);
            span.RecordException(ex);
            throw;
        }
    }

    public async Task<AssetDetailsDTO?> UpdateCreditAsync(CreditUpdateDTO request)
    {
        using var span = StartSpan("UpdateCredit");
        span.SetAttribute(TelemetryAttributeKeys.EntityId, request.Id.ToString());
        try
        {
            if (request.Id == Guid.Empty)
            {
                span.SetAttribute(TelemetryAttributeKeys.OperationResult, TelemetryOperationResults.Success);
                _logger.LogInformation("{Operation} completed", "UpdateCredit");
                return null;
            }

            var result = await AssetMutationHelper.ExecuteParsedMutationAsync<Credit.CreditType>(
                _repository,
                _navigationService,
                request.BrokerName,
                request.PortfolioName,
                request.AssetName,
                request.Type,
                CreditTypeParser.TryParse,
                (asset, creditType) =>
                {
                    var updatedCredit = Credit.CreateWithId(request.Id, request.Date, creditType, request.Value);
                    return asset.UpdateCredit(updatedCredit);
                }).ConfigureAwait(false);

            span.SetAttribute(TelemetryAttributeKeys.OperationResult, TelemetryOperationResults.Success);
            _logger.LogInformation("{Operation} completed", "UpdateCredit");
            return result;
        }
        catch (Exception ex)
        {
            span.SetAttribute(TelemetryAttributeKeys.OperationResult, TelemetryOperationResults.Failed);
            span.RecordException(ex);
            throw;
        }
    }

    public async Task<AssetDetailsDTO?> DeleteCreditAsync(CreditDeleteDTO request)
    {
        using var span = StartSpan("DeleteCredit");
        span.SetAttribute(TelemetryAttributeKeys.EntityId, request.Id.ToString());
        try
        {
            if (request.Id == Guid.Empty)
            {
                span.SetAttribute(TelemetryAttributeKeys.OperationResult, TelemetryOperationResults.Success);
                _logger.LogInformation("{Operation} completed", "DeleteCredit");
                return null;
            }

            var result = await AssetMutationHelper.ExecuteAssetMutationAsync(
                _repository,
                _navigationService,
                request.BrokerName,
                request.PortfolioName,
                request.AssetName,
                asset => asset.RemoveCredit(request.Id)).ConfigureAwait(false);

            span.SetAttribute(TelemetryAttributeKeys.OperationResult, TelemetryOperationResults.Success);
            _logger.LogInformation("{Operation} completed", "DeleteCredit");
            return result;
        }
        catch (Exception ex)
        {
            span.SetAttribute(TelemetryAttributeKeys.OperationResult, TelemetryOperationResults.Failed);
            span.RecordException(ex);
            throw;
        }
    }

    public IReadOnlyList<CreditDTO> GetCreditsByBroker(string brokerName, InvestmentScope scope = InvestmentScope.Active)
    {
        using var span = StartSpan("GetCreditsByBroker");
        try
        {
            if (string.IsNullOrWhiteSpace(brokerName))
            {
                span.SetAttribute(TelemetryAttributeKeys.OperationResult, TelemetryOperationResults.Success);
                _logger.LogInformation("{Operation} completed", "GetCreditsByBroker");
                return Array.Empty<CreditDTO>();
            }

            var result = _repository.GetAssetsByBroker(brokerName, scope)
                .SelectMany(asset => asset.Credits)
                .Select(NavigationMapper.MapCredit)
                .OrderByDescending(credit => credit.Date)
                .ToList();

            span.SetAttribute(TelemetryAttributeKeys.OperationResult, TelemetryOperationResults.Success);
            _logger.LogInformation("{Operation} completed", "GetCreditsByBroker");
            return result;
        }
        catch (Exception ex)
        {
            span.SetAttribute(TelemetryAttributeKeys.OperationResult, TelemetryOperationResults.Failed);
            span.RecordException(ex);
            throw;
        }
    }

    public IReadOnlyList<CreditDTO> GetCreditsByPortfolio(string brokerName, string portfolioName, InvestmentScope scope = InvestmentScope.Active)
    {
        using var span = StartSpan("GetCreditsByPortfolio");
        try
        {
            if (string.IsNullOrWhiteSpace(brokerName) || string.IsNullOrWhiteSpace(portfolioName))
            {
                span.SetAttribute(TelemetryAttributeKeys.OperationResult, TelemetryOperationResults.Success);
                _logger.LogInformation("{Operation} completed", "GetCreditsByPortfolio");
                return Array.Empty<CreditDTO>();
            }

            var result = _repository.GetAssetsByBrokerPortfolio(brokerName, portfolioName, scope)
                .SelectMany(asset => asset.Credits)
                .Select(NavigationMapper.MapCredit)
                .OrderByDescending(credit => credit.Date)
                .ToList();

            span.SetAttribute(TelemetryAttributeKeys.OperationResult, TelemetryOperationResults.Success);
            _logger.LogInformation("{Operation} completed", "GetCreditsByPortfolio");
            return result;
        }
        catch (Exception ex)
        {
            span.SetAttribute(TelemetryAttributeKeys.OperationResult, TelemetryOperationResults.Failed);
            span.RecordException(ex);
            throw;
        }
    }

    private ITelemetrySpan StartSpan(string operationName)
    {
        _logger.LogInformation("{Operation} started", operationName);
        var span = _tracer.StartSpan($"Investment.CreditService.{operationName}");
        span.SetAttribute(TelemetryAttributeKeys.BoundedContext, "Investment");
        span.SetAttribute(TelemetryAttributeKeys.EntityType, EntityType);
        span.SetAttribute(TelemetryAttributeKeys.OperationName, operationName);
        return span;
    }
}
