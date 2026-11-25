using Financial.Investment.Application.DTOs;
using Financial.Investment.Application.Enums;
using Financial.Investment.Application.Interfaces;
using Financial.Investment.Domain.Entities;
using Financial.Shared.Abstractions.Observability;
using Financial.Shared.Abstractions.Time;
using Microsoft.Extensions.Logging;

namespace Financial.Investment.Application.Services;

public sealed class PortfolioAssetSummaryService : IPortfolioAssetSummaryService
{
    private const string EntityType = "PortfolioAssetSummary";

    private readonly IInvestmentRepository _repository;
    private readonly IHoldingValuationService _holdingValuationService;
    private readonly TimeProvider _timeProvider;
    private readonly ITelemetryTracer _tracer;
    private readonly ILogger<PortfolioAssetSummaryService> _logger;

    public PortfolioAssetSummaryService(
        IInvestmentRepository repository,
        IHoldingValuationService holdingValuationService,
        TimeProvider timeProvider,
        ITelemetryTracer tracer,
        ILogger<PortfolioAssetSummaryService> logger)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _holdingValuationService = holdingValuationService ?? throw new ArgumentNullException(nameof(holdingValuationService));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
        _tracer = tracer ?? throw new ArgumentNullException(nameof(tracer));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public IReadOnlyList<PortfolioAssetSummaryItemDTO> GetPortfolioAssetsSummary(string brokerName, string portfolioName, InvestmentScope scope = InvestmentScope.Active)
    {
        using var span = StartSpan("GetPortfolioAssetsSummary");
        try
        {
            if (string.IsNullOrWhiteSpace(brokerName) || string.IsNullOrWhiteSpace(portfolioName))
            {
                span.MarkSuccess();
                _logger.LogInformation("{Operation} completed", "GetPortfolioAssetsSummary");
                return [];
            }

            var assets = _repository.GetAssetsByBrokerPortfolio(brokerName, portfolioName, scope).ToList();
            if (assets.Count == 0)
            {
                span.MarkSuccess();
                _logger.LogInformation("{Operation} completed", "GetPortfolioAssetsSummary");
                return [];
            }

            var result = PortfolioAssetSummaryBuilder.Build(assets, _timeProvider.GetLocalDate(), scope, _holdingValuationService);

            span.MarkSuccess();
            _logger.LogInformation("{Operation} completed", "GetPortfolioAssetsSummary");
            return result;
        }
        catch (Exception ex)
        {
            span.MarkFailed(ex);
            throw;
        }
    }

    public BrokerHoldingsDTO GetBrokerHoldings(string brokerName)
    {
        using var span = StartSpan("GetBrokerHoldings");
        try
        {
            var broker = _repository.GetBrokerList(InvestmentScope.Active).FirstOrDefault(b => b.Name == brokerName);
            var today = _timeProvider.GetLocalDate();
            var result = broker is null
                ? new BrokerHoldingsDTO()
                : BrokerHoldingsBuilder.Build(HeldCurrencies(broker), broker.Portfolios.Select(p => BuildGroupSource(p, today)));

            span.MarkSuccess();
            _logger.LogInformation("{Operation} completed", "GetBrokerHoldings");
            return result;
        }
        catch (Exception ex)
        {
            span.MarkFailed(ex);
            throw;
        }
    }

    private static IReadOnlyList<string> HeldCurrencies(Broker broker)
    {
        var currencies = broker.Portfolios
            .SelectMany(p => p.Assets)
            .Where(a => a.Quantity > 0m)
            .SelectMany(a => a.Transactions)
            .Select(t => t.Currency.ToString())
            .Distinct()
            .Order(StringComparer.Ordinal)
            .ToList();
        return currencies.Count == 0 ? [broker.Currency] : currencies;
    }

    private HoldingsGroupSource BuildGroupSource(Portfolio portfolio, DateTime today) =>
        new(portfolio.Name, PortfolioAssetSummaryBuilder.Build(
            portfolio.Assets.Where(a => a.Quantity > 0m),
            today,
            InvestmentScope.Active,
            _holdingValuationService));

    private ITelemetrySpan StartSpan(string operationName)
    {
        _logger.LogInformation("{Operation} started", operationName);
        return _tracer.StartServiceSpan("Investment", nameof(PortfolioAssetSummaryService), operationName, EntityType);
    }
}
