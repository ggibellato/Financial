using Financial.Investment.Application.DTOs;
using Financial.Investment.Application.Interfaces;
using Financial.Investment.Domain.Entities;
using Financial.Shared.Abstractions.Currencies;
using Financial.Shared.Abstractions.Observability;
using Microsoft.Extensions.Logging;

namespace Financial.Investment.Application.Services;

internal readonly record struct AllocationHolding(Asset Asset, string BrokerName, Currency Currency);

public sealed class AllocationBreakdownService : IAllocationBreakdownService
{
    private const string EntityType = "AllocationBreakdown";
    private const string OperationName = "GetAllocationBreakdown";

    private readonly IInvestmentRepository _repository;
    private readonly IHoldingValuationService _holdingValuationService;
    private readonly ITelemetryTracer _tracer;
    private readonly ILogger<AllocationBreakdownService> _logger;
    private readonly IExchangeRateProvider _exchangeRateProvider;
    private readonly TimeProvider _timeProvider;

    public AllocationBreakdownService(
        IInvestmentRepository repository,
        IHoldingValuationService holdingValuationService,
        ITelemetryTracer tracer,
        ILogger<AllocationBreakdownService> logger,
        IExchangeRateProvider exchangeRateProvider,
        TimeProvider? timeProvider = null)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _holdingValuationService = holdingValuationService ?? throw new ArgumentNullException(nameof(holdingValuationService));
        _tracer = tracer ?? throw new ArgumentNullException(nameof(tracer));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _exchangeRateProvider = exchangeRateProvider ?? throw new ArgumentNullException(nameof(exchangeRateProvider));
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    public async Task<AllocationBreakdownDTO> GetAllocationBreakdownAsync(Currency? displayCurrency = null, Currency? brokerCurrencyFilter = null)
    {
        using var span = StartSpan(OperationName);
        try
        {
            var asOf = DateOnly.FromDateTime(_timeProvider.GetUtcNow().UtcDateTime);
            var holdings = CollectActiveHoldings(_repository.GetInvestments(), brokerCurrencyFilter);
            var result = await AllocationBreakdownBuilder
                .BuildAsync(holdings, _holdingValuationService, displayCurrency, _exchangeRateProvider, asOf)
                .ConfigureAwait(false);

            span.MarkSuccess();
            _logger.LogInformation("{Operation} completed", OperationName);
            return result;
        }
        catch (Exception ex)
        {
            span.MarkFailed(ex);
            throw;
        }
    }

    private static IReadOnlyList<AllocationHolding> CollectActiveHoldings(Investments investments, Currency? brokerCurrencyFilter)
    {
        var holdings = new List<AllocationHolding>();
        foreach (var broker in investments.ActiveBrokers)
        {
            var currency = BrokerCurrencyParser.Parse(broker.Currency);
            if (!BrokerCurrencyParser.Matches(currency, brokerCurrencyFilter))
            {
                continue;
            }

            foreach (var asset in broker.Portfolios.SelectMany(portfolio => portfolio.Assets))
            {
                holdings.Add(new AllocationHolding(asset, broker.Name, currency));
            }
        }

        return holdings;
    }

    private ITelemetrySpan StartSpan(string operationName)
    {
        _logger.LogInformation("{Operation} started", operationName);
        return _tracer.StartServiceSpan("Investment", nameof(AllocationBreakdownService), operationName, EntityType);
    }
}
