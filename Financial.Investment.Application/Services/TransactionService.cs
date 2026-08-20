using Financial.Investment.Application.DTOs;
using Financial.Investment.Application.Enums;
using Financial.Investment.Application.Interfaces;
using Financial.Investment.Application.Validation;
using Financial.Investment.Domain.Entities;
using Financial.Shared.Abstractions;
using Microsoft.Extensions.Logging;

namespace Financial.Investment.Application.Services;

public sealed class TransactionService : ITransactionService, ITransactionQueryService
{
    private const string EntityType = "Transaction";

    private readonly IInvestmentRepository _repository;
    private readonly INavigationService _navigationService;
    private readonly ITelemetryTracer _tracer;
    private readonly ILogger<TransactionService> _logger;

    public TransactionService(IInvestmentRepository repository, INavigationService navigationService, ITelemetryTracer tracer, ILogger<TransactionService> logger)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _navigationService = navigationService ?? throw new ArgumentNullException(nameof(navigationService));
        _tracer = tracer ?? throw new ArgumentNullException(nameof(tracer));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<AssetDetailsDTO?> AddTransactionAsync(TransactionCreateDTO request)
    {
        using var span = StartSpan("AddTransaction");
        try
        {
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
                    var transaction = Transaction.Create(request.Date, transactionType, request.Quantity, request.UnitPrice, request.Fees);
                    asset.AddTransaction(transaction);
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
                    var updatedTransaction = Transaction.CreateWithId(request.Id, request.Date, transactionType, request.Quantity, request.UnitPrice, request.Fees);
                    return asset.UpdateTransaction(updatedTransaction);
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
                asset => asset.RemoveTransaction(request.Id)).ConfigureAwait(false);

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
