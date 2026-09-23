using Financial.Investment.Application.DTOs;
using Financial.Investment.Application.Enums;
using Financial.Investment.Application.Interfaces;
using Financial.Investment.Application.Validation;
using Financial.Investment.Domain.Entities;
using Financial.Investment.Domain.Rules;
using Financial.Shared.Abstractions.Currencies;
using Financial.Shared.Abstractions.Observability;
using Microsoft.Extensions.Logging;

namespace Financial.Investment.Application.Services;

public sealed class TransactionService : ITransactionService, ITransactionQueryService
{
    private const string EntityType = "Transaction";

    private readonly IInvestmentRepository _repository;
    private readonly INavigationService _navigationService;
    private readonly IExchangeRateProvider _exchangeRateProvider;
    private readonly IReportingCurrencyProvider _reportingCurrencyProvider;
    private readonly TimeProvider _timeProvider;
    private readonly ITelemetryTracer _tracer;
    private readonly ILogger<TransactionService> _logger;

    public TransactionService(
        IInvestmentRepository repository,
        INavigationService navigationService,
        IExchangeRateProvider exchangeRateProvider,
        IReportingCurrencyProvider reportingCurrencyProvider,
        TimeProvider timeProvider,
        ITelemetryTracer tracer,
        ILogger<TransactionService> logger)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _navigationService = navigationService ?? throw new ArgumentNullException(nameof(navigationService));
        _exchangeRateProvider = exchangeRateProvider ?? throw new ArgumentNullException(nameof(exchangeRateProvider));
        _reportingCurrencyProvider = reportingCurrencyProvider ?? throw new ArgumentNullException(nameof(reportingCurrencyProvider));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
        _tracer = tracer ?? throw new ArgumentNullException(nameof(tracer));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<AssetDetailsDTO?> AddTransactionAsync(TransactionCreateDTO request)
    {
        using var span = StartSpan("AddTransaction");
        try
        {
            var captured = await FxEntryCaptureHelper.CaptureAsync(
                _repository, _exchangeRateProvider, _reportingCurrencyProvider, _timeProvider, request.BrokerName, request.Date).ConfigureAwait(false);
            if (captured is null)
            {
                span.MarkSuccess();
                _logger.LogInformation("{Operation} completed", "AddTransaction");
                return null;
            }

            var (currency, fxRateSnapshot) = captured.Value;

            var result = await AssetMutationHelper.ExecuteParsedMutationAsync<Transaction.TransactionType>(
                _repository,
                _navigationService,
                request.BrokerName,
                request.PortfolioName,
                request.AssetName,
                request.Type,
                TransactionTypeParser.TryParse,
                (asset, transactionType) =>
                {
                    var transaction = Transaction.Create(request.Date, transactionType, request.Quantity, request.UnitPrice, request.Fees, request.Withheld, currency, fxRateSnapshot);
                    var method = ResolveCostBasisMethod(request.BrokerName);
                    var allocation = request.SpecificLotAllocations?
                        .Select(entry => new SpecificLotAllocation(entry.SourceTransactionId, entry.Quantity))
                        .ToList();
                    asset.RecordTransaction(transaction, method, allocation, _repository.GetInvestments());
                    return true;
                }).ConfigureAwait(false);

            span.MarkSuccess();
            _logger.LogInformation("{Operation} completed", "AddTransaction");
            return result;
        }
        catch (Exception ex)
        {
            span.MarkFailed(ex);
            throw;
        }
    }

    public async Task<AssetDetailsDTO?> UpdateTransactionAsync(TransactionUpdateDTO request)
    {
        using var span = StartSpan("UpdateTransaction");
        span.SetAttribute(TelemetryAttributeKeys.EntityId, request.Id.ToString());
        try
        {
            if (request.Id == Guid.Empty)
            {
                span.MarkSuccess();
                _logger.LogInformation("{Operation} completed", "UpdateTransaction");
                return null;
            }

            var result = await AssetMutationHelper.ExecuteParsedMutationAsync<Transaction.TransactionType>(
                _repository,
                _navigationService,
                request.BrokerName,
                request.PortfolioName,
                request.AssetName,
                request.Type,
                TransactionTypeParser.TryParse,
                (asset, transactionType) =>
                {
                    var existing = asset.Transactions.FirstOrDefault(t => t.Id == request.Id);
                    var updatedTransaction = Transaction.CreateWithId(
                        request.Id, request.Date, transactionType, request.Quantity, request.UnitPrice, request.Fees, request.Withheld,
                        existing?.Currency ?? default, existing?.FxRateSnapshot);
                    var method = ResolveCostBasisMethod(request.BrokerName);
                    return asset.ReviseTransaction(updatedTransaction, method, _repository.GetInvestments());
                }).ConfigureAwait(false);

            span.MarkSuccess();
            _logger.LogInformation("{Operation} completed", "UpdateTransaction");
            return result;
        }
        catch (Exception ex)
        {
            span.MarkFailed(ex);
            throw;
        }
    }

    public async Task<AssetDetailsDTO?> DeleteTransactionAsync(TransactionDeleteDTO request)
    {
        using var span = StartSpan("DeleteTransaction");
        span.SetAttribute(TelemetryAttributeKeys.EntityId, request.Id.ToString());
        try
        {
            if (request.Id == Guid.Empty)
            {
                span.MarkSuccess();
                _logger.LogInformation("{Operation} completed", "DeleteTransaction");
                return null;
            }

            var result = await AssetMutationHelper.ExecuteAssetMutationAsync(
                _repository,
                _navigationService,
                request.BrokerName,
                request.PortfolioName,
                request.AssetName,
                asset => asset.RetractTransaction(request.Id, ResolveCostBasisMethod(request.BrokerName), _repository.GetInvestments())).ConfigureAwait(false);

            span.MarkSuccess();
            _logger.LogInformation("{Operation} completed", "DeleteTransaction");
            return result;
        }
        catch (Exception ex)
        {
            span.MarkFailed(ex);
            throw;
        }
    }

    public IReadOnlyList<TransactionSummaryItemDTO> GetTransactionsByBroker(string brokerName, InvestmentScope scope = InvestmentScope.Active)
    {
        using var span = StartSpan("GetTransactionsByBroker");
        try
        {
            if (string.IsNullOrWhiteSpace(brokerName))
            {
                span.MarkSuccess();
                _logger.LogInformation("{Operation} completed", "GetTransactionsByBroker");
                return Array.Empty<TransactionSummaryItemDTO>();
            }

            var result = MapAndSort(_repository.GetAssetsByBroker(brokerName, scope));
            span.MarkSuccess();
            _logger.LogInformation("{Operation} completed", "GetTransactionsByBroker");
            return result;
        }
        catch (Exception ex)
        {
            span.MarkFailed(ex);
            throw;
        }
    }

    public IReadOnlyList<TransactionSummaryItemDTO> GetTransactionsByPortfolio(string brokerName, string portfolioName, InvestmentScope scope = InvestmentScope.Active)
    {
        using var span = StartSpan("GetTransactionsByPortfolio");
        try
        {
            if (string.IsNullOrWhiteSpace(brokerName) || string.IsNullOrWhiteSpace(portfolioName))
            {
                span.MarkSuccess();
                _logger.LogInformation("{Operation} completed", "GetTransactionsByPortfolio");
                return Array.Empty<TransactionSummaryItemDTO>();
            }

            var result = MapAndSort(_repository.GetAssetsByBrokerPortfolio(brokerName, portfolioName, scope));
            span.MarkSuccess();
            _logger.LogInformation("{Operation} completed", "GetTransactionsByPortfolio");
            return result;
        }
        catch (Exception ex)
        {
            span.MarkFailed(ex);
            throw;
        }
    }

    public IReadOnlyList<TransactionTypeEffectDTO> GetTransactionTypeEffects() =>
        Enum.GetValues<Transaction.TransactionType>()
            .Select(type =>
            {
                var effect = TransactionTypeEffects.For(type);
                return new TransactionTypeEffectDTO
                {
                    Type = type.ToString(),
                    QuantityEffect = effect.Quantity.ToString(),
                    CashEffect = effect.Cash.ToString()
                };
            })
            .ToList();

    private CostBasisMethod ResolveCostBasisMethod(string brokerName) =>
        AssetMutationHelper.ResolveCostBasisMethod(_repository, brokerName);

    private ITelemetrySpan StartSpan(string operationName)
    {
        _logger.LogInformation("{Operation} started", operationName);
        return _tracer.StartServiceSpan("Investment", nameof(TransactionService), operationName, EntityType);
    }

    private static IReadOnlyList<TransactionSummaryItemDTO> MapAndSort(IEnumerable<Asset> assets)
    {
        return assets
            .SelectMany(asset => asset.Transactions.Select(transaction => NavigationMapper.MapTransactionSummaryItem(asset, transaction)))
            .OrderBy(item => item.Date)
            .ThenBy(item => item.AssetName, StringComparer.CurrentCultureIgnoreCase)
            .ToList();
    }
}
