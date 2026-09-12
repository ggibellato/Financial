using Financial.Investment.Application.Enums;
using Financial.Investment.Application.Interfaces;
using Financial.Investment.Domain.Entities;
using Financial.Investment.Domain.Rules;
using Financial.Shared.Abstractions.Observability;
using Microsoft.Extensions.Logging;

namespace Financial.Investment.Application.Services;

public sealed class HoldingValuationService : IHoldingValuationService
{
    private const string EntityType = "HoldingValuation";

    private readonly IXirrCalculationService _xirrCalculationService;
    private readonly ITelemetryTracer _tracer;
    private readonly ILogger<HoldingValuationService> _logger;
    private readonly TimeProvider _timeProvider;

    public HoldingValuationService(
        IXirrCalculationService xirrCalculationService,
        ITelemetryTracer tracer,
        ILogger<HoldingValuationService> logger,
        TimeProvider? timeProvider = null)
    {
        _xirrCalculationService = xirrCalculationService ?? throw new ArgumentNullException(nameof(xirrCalculationService));
        _tracer = tracer ?? throw new ArgumentNullException(nameof(tracer));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    public HoldingValuation GetValuation(Asset asset, InvestmentScope scope)
    {
        using var span = StartSpan("GetValuation");
        try
        {
            ArgumentNullException.ThrowIfNull(asset);

            var today = DateOnly.FromDateTime(_timeProvider.GetUtcNow().UtcDateTime);

            var valuation = scope == InvestmentScope.Historic
                ? HoldingValuationCalculator.NotMarkedToMarket(asset.Quantity, asset.AveragePrice)
                : HoldingValuationCalculator.Calculate(asset.Quantity, asset.AveragePrice, asset.GetPriceAsOf(today), today);

            if (valuation.MarketValue is not null)
            {
                var priceOnlyReturn = _xirrCalculationService.Calculate(AssetCashFlowBuilder.BuildWithoutCredits(asset), valuation.MarketValue.Value);
                var totalReturn = _xirrCalculationService.Calculate(AssetCashFlowBuilder.BuildWithCredits(asset), valuation.MarketValue.Value);
                var totalReturnNetOfTax = _xirrCalculationService.Calculate(AssetCashFlowBuilder.BuildNetOfTaxWithCredits(asset), valuation.MarketValue.Value);
                valuation = valuation with { PriceOnlyReturn = priceOnlyReturn, TotalReturn = totalReturn, TotalReturnNetOfTax = totalReturnNetOfTax };
            }

            span.MarkSuccess();
            _logger.LogInformation("{Operation} completed", "GetValuation");
            return valuation;
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
        return _tracer.StartServiceSpan("Investment", nameof(HoldingValuationService), operationName, EntityType);
    }
}
