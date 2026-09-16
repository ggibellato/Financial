using Financial.Investment.Application.DTOs;
using Financial.Investment.Application.Enums;
using Financial.Investment.Application.Interfaces;
using Financial.Investment.Domain.Entities;
using Financial.Shared.Abstractions.Currencies;
using Financial.Shared.Abstractions.Observability;
using Financial.Shared.Abstractions.Validation;
using Microsoft.Extensions.Logging;

namespace Financial.Investment.Application.Services;

internal readonly record struct PortfolioHolding(
    Asset Asset,
    Currency Currency,
    bool IsActive,
    decimal? MarketValue,
    decimal? UnrealisedGain)
{
    public bool HasTerminalValue => IsActive && MarketValue is not null;
}

internal readonly record struct ActiveScopeTotals(
    decimal MarketValue,
    decimal Invested,
    decimal UnrealisedGainLoss,
    int UnvaluedHoldingCount);

public sealed class PortfolioDashboardService : IPortfolioDashboardService
{
    private const string EntityType = "PortfolioDashboard";
    private const string OperationName = "GetDashboard";

    private readonly IInvestmentRepository _repository;
    private readonly ITelemetryTracer _tracer;
    private readonly ILogger<PortfolioDashboardService> _logger;
    private readonly IHoldingValuationService _holdingValuationService;
    private readonly IExchangeRateProvider _exchangeRateProvider;
    private readonly IReportingCurrencyProvider _reportingCurrencyProvider;
    private readonly TimeProvider _timeProvider;

    public PortfolioDashboardService(
        IInvestmentRepository repository,
        ITelemetryTracer tracer,
        ILogger<PortfolioDashboardService> logger,
        IHoldingValuationService holdingValuationService,
        IExchangeRateProvider exchangeRateProvider,
        IReportingCurrencyProvider reportingCurrencyProvider,
        TimeProvider? timeProvider = null)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _tracer = tracer ?? throw new ArgumentNullException(nameof(tracer));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _holdingValuationService = holdingValuationService ?? throw new ArgumentNullException(nameof(holdingValuationService));
        _exchangeRateProvider = exchangeRateProvider ?? throw new ArgumentNullException(nameof(exchangeRateProvider));
        _reportingCurrencyProvider = reportingCurrencyProvider ?? throw new ArgumentNullException(nameof(reportingCurrencyProvider));
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    public async Task<PortfolioDashboardDTO> GetDashboardAsync()
    {
        using var span = StartSpan(OperationName);
        try
        {
            var asOf = DateOnly.FromDateTime(_timeProvider.GetUtcNow().UtcDateTime);
            var holdings = CollectHoldings(_repository.GetInvestments());
            var result = await BuildDashboardAsync(holdings, asOf).ConfigureAwait(false);

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

    private ITelemetrySpan StartSpan(string operationName)
    {
        _logger.LogInformation("{Operation} started", operationName);
        return _tracer.StartServiceSpan("Investment", nameof(PortfolioDashboardService), operationName, EntityType);
    }

    private IReadOnlyList<PortfolioHolding> CollectHoldings(Investments investments)
    {
        var holdings = new List<PortfolioHolding>();
        AddHoldings(holdings, investments.ActiveBrokers, isActive: true);
        AddHoldings(holdings, investments.HistoricBrokers, isActive: false);
        return holdings;
    }

    private void AddHoldings(List<PortfolioHolding> holdings, IEnumerable<Broker> brokers, bool isActive)
    {
        foreach (var broker in brokers)
        {
            var currency = ParseCurrency(broker.Currency);
            foreach (var asset in broker.Portfolios.SelectMany(portfolio => portfolio.Assets))
            {
                var valuation = isActive ? _holdingValuationService.GetValuation(asset, InvestmentScope.Active) : null;
                holdings.Add(new PortfolioHolding(asset, currency, isActive, valuation?.MarketValue, valuation?.UnrealisedGain));
            }
        }
    }

    private static Currency ParseCurrency(string rawCurrency)
    {
        if (!EnumParser.TryParseEnum<Currency>(rawCurrency, out var currency))
        {
            throw new ArgumentException($"Broker currency \"{rawCurrency}\" is not recognized.", nameof(rawCurrency));
        }

        return currency;
    }

    private async Task<PortfolioDashboardDTO> BuildDashboardAsync(IReadOnlyList<PortfolioHolding> holdings, DateOnly asOf)
    {
        var active = SumActiveScope(holdings);
        var (incomeYtd, incomeLifetime) = SumIncome(holdings, asOf);
        var realisedGainLoss = SumRealisedGainLoss(holdings);
        var xirr = PortfolioXirrBuilder.Calculate(holdings, asOf.ToDateTime(TimeOnly.MinValue));
        var reportingCurrency = _reportingCurrencyProvider.GetReportingCurrency();

        PortfolioDashboardDTO BuildResult(PortfolioDashboardConversion? converted) => new()
        {
            MarketValue = active.MarketValue,
            Invested = active.Invested,
            UnrealisedGainLoss = active.UnrealisedGainLoss,
            RealisedGainLoss = realisedGainLoss,
            IncomeYtd = incomeYtd,
            IncomeLifetime = incomeLifetime,
            GrossXirr = xirr.GrossXirr,
            NetXirr = xirr.NetXirr,
            UnvaluedHoldingCount = active.UnvaluedHoldingCount,
            IsPartial = active.UnvaluedHoldingCount > 0,
            ReportingCurrency = reportingCurrency.ToString(),
            IsReportingCurrencyEnabled = converted is not null,
            ConvertedMarketValue = converted?.MarketValue,
            ConvertedInvested = converted?.Invested,
            ConvertedUnrealisedGainLoss = converted?.UnrealisedGainLoss,
            ConvertedRealisedGainLoss = converted?.RealisedGainLoss,
            ConvertedIncomeYtd = converted?.IncomeYtd,
            ConvertedIncomeLifetime = converted?.IncomeLifetime,
            ConvertedGrossXirr = converted?.GrossXirr,
            ConvertedNetXirr = converted?.NetXirr,
            IsReportingCurrencyPartial = converted?.IsPartial ?? false,
            IsReportingCurrencyUnavailable = converted?.IsUnavailable ?? false,
        };

        if (!_reportingCurrencyProvider.IsReportingCurrencyEnabled())
        {
            return BuildResult(converted: null);
        }

        return BuildResult(await PortfolioDashboardConvertedBuilder
            .BuildAsync(holdings, reportingCurrency, _exchangeRateProvider, asOf)
            .ConfigureAwait(false));
    }

    private static ActiveScopeTotals SumActiveScope(IReadOnlyList<PortfolioHolding> holdings)
    {
        decimal marketValue = 0m, invested = 0m, unrealisedGainLoss = 0m;
        var unvaluedHoldingCount = 0;

        foreach (var holding in holdings.Where(holding => holding.IsActive))
        {
            invested += AssetAmountBases.For(InvestmentScope.Active, AssetTotals.For(holding.Asset)).InvestedAmount;

            if (holding.MarketValue is not decimal holdingMarketValue)
            {
                unvaluedHoldingCount++;
                continue;
            }

            marketValue += holdingMarketValue;
            unrealisedGainLoss += holding.UnrealisedGain ?? 0m;
        }

        return new ActiveScopeTotals(marketValue, invested, unrealisedGainLoss, unvaluedHoldingCount);
    }

    /// <summary>Never read <see cref="Asset.RealizedGainLoss"/> here: it also folds in every credit's
    /// gross value, which this DTO already reports separately as income.</summary>
    private static decimal SumRealisedGainLoss(IReadOnlyList<PortfolioHolding> holdings) =>
        holdings.Sum(holding => holding.Asset.DisposalRecords
            .Where(disposal => disposal.Status == DisposalRecordStatus.Active)
            .Sum(disposal => disposal.GainLoss));

    private static (decimal Ytd, decimal Lifetime) SumIncome(IReadOnlyList<PortfolioHolding> holdings, DateOnly asOf)
    {
        var yearStart = new DateTime(asOf.Year, 1, 1);
        var credits = holdings.SelectMany(holding => holding.Asset.Credits).ToList();

        return (credits.Where(credit => credit.Date >= yearStart).Sum(credit => credit.NetAmount),
            credits.Sum(credit => credit.NetAmount));
    }
}
