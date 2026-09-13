using Financial.Investment.Application.DTOs;
using Financial.Investment.Application.Enums;
using Financial.Investment.Application.Interfaces;
using Financial.Investment.Domain.Entities;
using Financial.Shared.Abstractions.Currencies;
using Financial.Shared.Abstractions.Observability;
using Financial.Shared.Abstractions.Validation;
using Microsoft.Extensions.Logging;

namespace Financial.Investment.Application.Services;

public sealed class SummaryService : ISummaryService
{
    private const string EntityType = "AggregatedSummary";

    private readonly IInvestmentRepository _repository;
    private readonly ITelemetryTracer _tracer;
    private readonly ILogger<SummaryService> _logger;
    private readonly IHoldingValuationService _holdingValuationService;
    private readonly IXirrCalculationService _xirrCalculationService;
    private readonly IExchangeRateProvider _exchangeRateProvider;
    private readonly IReportingCurrencyProvider _reportingCurrencyProvider;
    private readonly TimeProvider _timeProvider;

    public SummaryService(
        IInvestmentRepository repository,
        ITelemetryTracer tracer,
        ILogger<SummaryService> logger,
        IHoldingValuationService holdingValuationService,
        IXirrCalculationService xirrCalculationService,
        IExchangeRateProvider exchangeRateProvider,
        IReportingCurrencyProvider reportingCurrencyProvider,
        TimeProvider? timeProvider = null)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _tracer = tracer ?? throw new ArgumentNullException(nameof(tracer));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _holdingValuationService = holdingValuationService ?? throw new ArgumentNullException(nameof(holdingValuationService));
        _xirrCalculationService = xirrCalculationService ?? throw new ArgumentNullException(nameof(xirrCalculationService));
        _exchangeRateProvider = exchangeRateProvider ?? throw new ArgumentNullException(nameof(exchangeRateProvider));
        _reportingCurrencyProvider = reportingCurrencyProvider ?? throw new ArgumentNullException(nameof(reportingCurrencyProvider));
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    public async Task<AggregatedSummaryDTO> GetBrokerSummaryAsync(string brokerName, InvestmentScope scope = InvestmentScope.Active)
    {
        using var span = StartSpan("GetBrokerSummary");
        try
        {
            if (string.IsNullOrWhiteSpace(brokerName))
            {
                span.MarkSuccess();
                _logger.LogInformation("{Operation} completed", "GetBrokerSummary");
                return EmptyResult();
            }

            var broker = _repository.GetBrokerList(scope).FirstOrDefault(b => b.Name == brokerName);
            if (broker is null)
            {
                span.MarkSuccess();
                _logger.LogInformation("{Operation} completed", "GetBrokerSummary");
                return EmptyResult();
            }

            var assets = broker.Portfolios.SelectMany(p => p.Assets);

            var result = await Aggregate(assets, scope, broker.Currency).ConfigureAwait(false);
            span.MarkSuccess();
            _logger.LogInformation("{Operation} completed", "GetBrokerSummary");
            return result;
        }
        catch (Exception ex)
        {
            span.MarkFailed(ex);
            throw;
        }
    }

    public async Task<AggregatedSummaryDTO> GetPortfolioSummaryAsync(string brokerName, string portfolioName, InvestmentScope scope = InvestmentScope.Active)
    {
        using var span = StartSpan("GetPortfolioSummary");
        try
        {
            if (string.IsNullOrWhiteSpace(brokerName) || string.IsNullOrWhiteSpace(portfolioName))
            {
                span.MarkSuccess();
                _logger.LogInformation("{Operation} completed", "GetPortfolioSummary");
                return EmptyResult();
            }

            var broker = _repository.GetBrokerList(scope).FirstOrDefault(b => b.Name == brokerName);
            if (broker is null)
            {
                span.MarkSuccess();
                _logger.LogInformation("{Operation} completed", "GetPortfolioSummary");
                return EmptyResult();
            }

            var assets = _repository.GetAssetsByBrokerPortfolio(brokerName, portfolioName, scope);
            var result = await Aggregate(assets, scope, broker.Currency).ConfigureAwait(false);

            span.MarkSuccess();
            _logger.LogInformation("{Operation} completed", "GetPortfolioSummary");
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
        return _tracer.StartServiceSpan("Investment", nameof(SummaryService), operationName, EntityType);
    }

    private AggregatedSummaryDTO EmptyResult() => new()
    {
        MarketValue = 0m,
        ReportingCurrency = _reportingCurrencyProvider.GetReportingCurrency().ToString(),
    };

    private async Task<AggregatedSummaryDTO> Aggregate(IEnumerable<Asset> assets, InvestmentScope scope, string brokerCurrencyRaw)
    {
        var assetList = assets as IReadOnlyList<Asset> ?? assets.ToList();

        decimal totalBought = 0, totalSold = 0, totalCredits = 0, totalInvested = 0;
        decimal marketValueSum = 0;
        decimal unrealisedGainSum = 0;
        var holdingCount = 0;
        var unvaluedHoldingCount = 0;

        foreach (var asset in assetList)
        {
            var totals = AssetTotals.For(asset);
            totalBought += totals.TotalBought;
            totalSold += totals.TotalSold;
            totalCredits += totals.TotalCredits;
            totalInvested += AssetAmountBases.For(scope, totals).InvestedAmount;

            holdingCount++;
            var valuation = _holdingValuationService.GetValuation(asset, scope);
            if (valuation.MarketValue is null)
            {
                unvaluedHoldingCount++;
            }
            else
            {
                marketValueSum += valuation.MarketValue.Value;
                unrealisedGainSum += valuation.UnrealisedGain ?? 0m;
            }
        }

        decimal? marketValue = holdingCount == 0
            ? 0m
            : unvaluedHoldingCount == holdingCount ? null : marketValueSum;

        decimal? priceOnlyReturn = null;
        decimal? totalReturn = null;
        decimal? totalReturnNetOfTax = null;

        var asOf = _timeProvider.GetUtcNow().UtcDateTime.Date;

        if (holdingCount > 0 && unvaluedHoldingCount == 0)
        {
            priceOnlyReturn = _xirrCalculationService.Calculate(AssetCashFlowBuilder.ConcatenateWithoutCredits(assetList), marketValueSum, asOf);
            totalReturn = _xirrCalculationService.Calculate(AssetCashFlowBuilder.ConcatenateWithCredits(assetList), marketValueSum, asOf);
            totalReturnNetOfTax = _xirrCalculationService.Calculate(AssetCashFlowBuilder.ConcatenateNetOfTaxWithCredits(assetList), marketValueSum, asOf);
        }

        var reportingCurrency = _reportingCurrencyProvider.GetReportingCurrency();
        if (!EnumParser.TryParseEnum<Currency>(brokerCurrencyRaw, out var brokerCurrency))
        {
            throw new ArgumentException($"Broker currency \"{brokerCurrencyRaw}\" is not recognized.", nameof(brokerCurrencyRaw));
        }

        var converted = await ConvertedSummaryBuilder.BuildAsync(
            assetList, brokerCurrency, reportingCurrency, marketValueSum, unrealisedGainSum,
            _exchangeRateProvider, _xirrCalculationService, asOf).ConfigureAwait(false);

        return new AggregatedSummaryDTO
        {
            TotalBought = totalBought,
            TotalSold = totalSold,
            TotalCredits = totalCredits,
            TotalInvested = totalInvested,
            MarketValue = marketValue,
            HoldingCount = holdingCount,
            UnvaluedHoldingCount = unvaluedHoldingCount,
            PriceOnlyReturn = priceOnlyReturn,
            TotalReturn = totalReturn,
            TotalReturnNetOfTax = totalReturnNetOfTax,
            ReportingCurrency = converted.ReportingCurrency.ToString(),
            ConvertedMarketValue = converted.ConvertedMarketValue,
            ConvertedInvested = converted.ConvertedInvested,
            ConvertedUnrealisedGainLoss = converted.ConvertedUnrealisedGainLoss,
            ConvertedTotalReturn = converted.ConvertedTotalReturn,
            ConvertedTotalReturnNetOfTax = converted.ConvertedTotalReturnNetOfTax,
            IsPartial = converted.IsPartial,
            IsReportingCurrencyUnavailable = converted.IsReportingCurrencyUnavailable,
        };
    }
}
