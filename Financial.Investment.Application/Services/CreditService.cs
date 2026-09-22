using Financial.Investment.Application.DTOs;
using Financial.Investment.Application.Enums;
using Financial.Investment.Application.Interfaces;
using Financial.Investment.Application.Validation;
using Financial.Investment.Domain.Entities;
using Financial.Shared.Abstractions.Currencies;
using Financial.Shared.Abstractions.Observability;
using Microsoft.Extensions.Logging;

namespace Financial.Investment.Application.Services;

public sealed class CreditService : ICreditService, ICreditQueryService
{
    private const string EntityType = "Credit";

    private readonly IInvestmentRepository _repository;
    private readonly INavigationService _navigationService;
    private readonly IExchangeRateProvider _exchangeRateProvider;
    private readonly IReportingCurrencyProvider _reportingCurrencyProvider;
    private readonly TimeProvider _timeProvider;
    private readonly ITelemetryTracer _tracer;
    private readonly ILogger<CreditService> _logger;

    public CreditService(
        IInvestmentRepository repository,
        INavigationService navigationService,
        IExchangeRateProvider exchangeRateProvider,
        IReportingCurrencyProvider reportingCurrencyProvider,
        TimeProvider timeProvider,
        ITelemetryTracer tracer,
        ILogger<CreditService> logger)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _navigationService = navigationService ?? throw new ArgumentNullException(nameof(navigationService));
        _exchangeRateProvider = exchangeRateProvider ?? throw new ArgumentNullException(nameof(exchangeRateProvider));
        _reportingCurrencyProvider = reportingCurrencyProvider ?? throw new ArgumentNullException(nameof(reportingCurrencyProvider));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
        _tracer = tracer ?? throw new ArgumentNullException(nameof(tracer));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<AssetDetailsDTO?> AddCreditAsync(CreditCreateDTO request)
    {
        using var span = StartSpan("AddCredit");
        try
        {
            var captured = await FxEntryCaptureHelper.CaptureAsync(
                _repository, _exchangeRateProvider, _reportingCurrencyProvider, _timeProvider, request.BrokerName, request.Date).ConfigureAwait(false);
            if (captured is null)
            {
                span.MarkSuccess();
                _logger.LogInformation("{Operation} completed", "AddCredit");
                return null;
            }

            var (currency, fxRateSnapshot) = captured.Value;

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
                    var credit = Credit.Create(request.Date, creditType, request.Value, request.Withheld, currency, fxRateSnapshot, request.SharesForDividend, request.IntermediationFee);
                    asset.AddCredit(credit, _repository.GetInvestments());
                    return true;
                }).ConfigureAwait(false);

            span.MarkSuccess();
            _logger.LogInformation("{Operation} completed", "AddCredit");
            return result;
        }
        catch (Exception ex)
        {
            span.MarkFailed(ex);
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
                span.MarkSuccess();
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
                    var existing = asset.Credits.FirstOrDefault(c => c.Id == request.Id);
                    var updatedCredit = Credit.CreateWithId(
                        request.Id, request.Date, creditType, request.Value, request.Withheld,
                        existing?.Currency ?? default, existing?.FxRateSnapshot, request.SharesForDividend, request.IntermediationFee);
                    return asset.UpdateCredit(updatedCredit, _repository.GetInvestments());
                }).ConfigureAwait(false);

            span.MarkSuccess();
            _logger.LogInformation("{Operation} completed", "UpdateCredit");
            return result;
        }
        catch (Exception ex)
        {
            span.MarkFailed(ex);
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
                span.MarkSuccess();
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

            span.MarkSuccess();
            _logger.LogInformation("{Operation} completed", "DeleteCredit");
            return result;
        }
        catch (Exception ex)
        {
            span.MarkFailed(ex);
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
                span.MarkSuccess();
                _logger.LogInformation("{Operation} completed", "GetCreditsByBroker");
                return Array.Empty<CreditDTO>();
            }

            var result = _repository.GetAssetsByBroker(brokerName, scope)
                .SelectMany(asset => asset.Credits.Select(credit => NavigationMapper.MapCredit(credit, asset)))
                .OrderByDescending(credit => credit.Date)
                .ToList();

            span.MarkSuccess();
            _logger.LogInformation("{Operation} completed", "GetCreditsByBroker");
            return result;
        }
        catch (Exception ex)
        {
            span.MarkFailed(ex);
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
                span.MarkSuccess();
                _logger.LogInformation("{Operation} completed", "GetCreditsByPortfolio");
                return Array.Empty<CreditDTO>();
            }

            var result = _repository.GetAssetsByBrokerPortfolio(brokerName, portfolioName, scope)
                .SelectMany(asset => asset.Credits.Select(credit => NavigationMapper.MapCredit(credit, asset)))
                .OrderByDescending(credit => credit.Date)
                .ToList();

            span.MarkSuccess();
            _logger.LogInformation("{Operation} completed", "GetCreditsByPortfolio");
            return result;
        }
        catch (Exception ex)
        {
            span.MarkFailed(ex);
            throw;
        }
    }

    private ITelemetrySpan StartSpan(string operationName)
    {
        _logger.LogInformation("{Operation} started", operationName);
        return _tracer.StartServiceSpan("Investment", nameof(CreditService), operationName, EntityType);
    }
}
