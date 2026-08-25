using Financial.Investment.Application.DTOs;
using Financial.Investment.Application.Enums;
using Financial.Investment.Application.Interfaces;
using Financial.Investment.Domain.Entities;
using Financial.Investment.Domain.Rules;
using Financial.Shared.Abstractions.Observability;
using Microsoft.Extensions.Logging;

namespace Financial.Investment.Application.Services;

public sealed class BrokerBreakdownService : IBrokerBreakdownService
{
    private const string EntityType = "PortfolioBreakdown";

    private readonly IInvestmentRepository _repository;
    private readonly ITelemetryTracer _tracer;
    private readonly ILogger<BrokerBreakdownService> _logger;

    public BrokerBreakdownService(IInvestmentRepository repository, ITelemetryTracer tracer, ILogger<BrokerBreakdownService> logger)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _tracer = tracer ?? throw new ArgumentNullException(nameof(tracer));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public IReadOnlyList<PortfolioBreakdownItemDTO> GetBrokerBreakdown(string brokerName, InvestmentScope scope = InvestmentScope.Active)
    {
        using var span = StartSpan("GetBrokerBreakdown");
        try
        {
            if (string.IsNullOrWhiteSpace(brokerName))
            {
                span.MarkSuccess();
                _logger.LogInformation("{Operation} completed", "GetBrokerBreakdown");
                return [];
            }

            var broker = _repository.GetBrokerList(scope).FirstOrDefault(b => b.Name == brokerName);
            if (broker is null)
            {
                span.MarkSuccess();
                _logger.LogInformation("{Operation} completed", "GetBrokerBreakdown");
                return [];
            }

            var result = scope == InvestmentScope.Historic
                ? BrokerBreakdownBuilder.Build(broker, CalculateGrossBought)
                : BrokerBreakdownBuilder.Build(broker, CalculateNetInvested);

            span.MarkSuccess();
            _logger.LogInformation("{Operation} completed", "GetBrokerBreakdown");
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
        return _tracer.StartServiceSpan("Investment", nameof(BrokerBreakdownService), operationName, EntityType);
    }

    private static decimal CalculateNetInvested(Asset asset)
    {
        var (totalBought, totalSold, _) = AssetTotalsCalculator.CalculateTotals(asset);
        return totalBought - totalSold;
    }

    private static decimal CalculateGrossBought(Asset asset) =>
        AssetTotalsCalculator.CalculateTotals(asset).TotalBought;
}
