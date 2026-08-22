using Financial.Investment.Application.DTOs;
using Financial.Investment.Application.Enums;
using Financial.Investment.Application.Interfaces;
using Financial.Investment.Domain.Entities;
using Financial.Shared.Abstractions.Observability;
using Microsoft.Extensions.Logging;

namespace Financial.Investment.Application.Services;

public sealed class SummaryService : ISummaryService
{
    private const string EntityType = "AggregatedSummary";

    private readonly IInvestmentRepository _repository;
    private readonly ITelemetryTracer _tracer;
    private readonly ILogger<SummaryService> _logger;

    public SummaryService(IInvestmentRepository repository, ITelemetryTracer tracer, ILogger<SummaryService> logger)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _tracer = tracer ?? throw new ArgumentNullException(nameof(tracer));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public AggregatedSummaryDTO GetBrokerSummary(string brokerName, InvestmentScope scope = InvestmentScope.Active)
    {
        using var span = StartSpan("GetBrokerSummary");
        try
        {
            if (string.IsNullOrWhiteSpace(brokerName))
            {
                span.MarkSuccess();
                _logger.LogInformation("{Operation} completed", "GetBrokerSummary");
                return new AggregatedSummaryDTO();
            }

            var broker = _repository.GetBrokerList(scope).FirstOrDefault(b => b.Name == brokerName);
            if (broker is null)
            {
                span.MarkSuccess();
                _logger.LogInformation("{Operation} completed", "GetBrokerSummary");
                return new AggregatedSummaryDTO();
            }

            var assets = broker.Portfolios.SelectMany(p => p.Assets);

            var result = Aggregate(assets);
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

    public AggregatedSummaryDTO GetPortfolioSummary(string brokerName, string portfolioName, InvestmentScope scope = InvestmentScope.Active)
    {
        using var span = StartSpan("GetPortfolioSummary");
        try
        {
            if (string.IsNullOrWhiteSpace(brokerName) || string.IsNullOrWhiteSpace(portfolioName))
            {
                span.MarkSuccess();
                _logger.LogInformation("{Operation} completed", "GetPortfolioSummary");
                return new AggregatedSummaryDTO();
            }

            var assets = _repository.GetAssetsByBrokerPortfolio(brokerName, portfolioName, scope);
            var result = Aggregate(assets);

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

    private static AggregatedSummaryDTO Aggregate(IEnumerable<Asset> assets)
    {
        decimal totalBought = 0, totalSold = 0, totalCredits = 0;

        foreach (var asset in assets)
        {
            var (bought, sold, credits) = NavigationMapper.CalculateTotals(asset);
            totalBought += bought;
            totalSold += sold;
            totalCredits += credits;
        }

        return new AggregatedSummaryDTO
        {
            TotalBought = totalBought,
            TotalSold = totalSold,
            TotalCredits = totalCredits,
            TotalInvested = totalBought - totalSold,
        };
    }
}
