using Financial.Investment.Application.DTOs;
using Financial.Investment.Application.Enums;
using Financial.Investment.Application.Interfaces;
using Financial.Shared.Abstractions.Observability;
using Microsoft.Extensions.Logging;

namespace Financial.Investment.Application.Services;

public sealed class PortfolioAssetSummaryService : IPortfolioAssetSummaryService
{
    private const string EntityType = "PortfolioAssetSummary";

    private readonly IInvestmentRepository _repository;
    private readonly ITelemetryTracer _tracer;
    private readonly ILogger<PortfolioAssetSummaryService> _logger;

    public PortfolioAssetSummaryService(IInvestmentRepository repository, ITelemetryTracer tracer, ILogger<PortfolioAssetSummaryService> logger)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
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

            var result = scope == InvestmentScope.Historic
                ? PortfolioAssetSummaryBuilder.Build(assets, DateTime.Today, CalculateGrossBought)
                : PortfolioAssetSummaryBuilder.Build(assets, DateTime.Today, CalculateNetInvested);

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

    private ITelemetrySpan StartSpan(string operationName)
    {
        _logger.LogInformation("{Operation} started", operationName);
        return _tracer.StartServiceSpan("Investment", nameof(PortfolioAssetSummaryService), operationName, EntityType);
    }

    private static decimal CalculateNetInvested(AssetTotals totals) => totals.TotalBought - totals.TotalSold;

    private static decimal CalculateGrossBought(AssetTotals totals) => totals.TotalBought;
}
