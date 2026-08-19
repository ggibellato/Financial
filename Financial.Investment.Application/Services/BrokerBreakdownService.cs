using Financial.Investment.Application.DTOs;
using Financial.Investment.Application.Enums;
using Financial.Investment.Application.Interfaces;
using Financial.Investment.Domain.Entities;
using Financial.Shared.Abstractions;

namespace Financial.Investment.Application.Services;

public sealed class BrokerBreakdownService : IBrokerBreakdownService
{
    private const string EntityType = "PortfolioBreakdown";

    private readonly IInvestmentRepository _repository;
    private readonly ITelemetryTracer _tracer;

    public BrokerBreakdownService(IInvestmentRepository repository, ITelemetryTracer tracer)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _tracer = tracer ?? throw new ArgumentNullException(nameof(tracer));
    }

    public IReadOnlyList<PortfolioBreakdownItemDTO> GetBrokerBreakdown(string brokerName, InvestmentScope scope = InvestmentScope.Active)
    {
        using var span = StartSpan("GetBrokerBreakdown");
        try
        {
            if (string.IsNullOrWhiteSpace(brokerName))
            {
                span.SetAttribute(TelemetryAttributeKeys.OperationResult, TelemetryOperationResults.Success);
                return [];
            }

            var broker = _repository.GetBrokerList(scope).FirstOrDefault(b => b.Name == brokerName);
            if (broker is null)
            {
                span.SetAttribute(TelemetryAttributeKeys.OperationResult, TelemetryOperationResults.Success);
                return [];
            }

            var result = scope == InvestmentScope.Historic
                ? BrokerBreakdownBuilder.Build(broker, CalculateGrossBought)
                : BrokerBreakdownBuilder.Build(broker, CalculateNetInvested);

            span.SetAttribute(TelemetryAttributeKeys.OperationResult, TelemetryOperationResults.Success);
            return result;
        }
        catch (Exception ex)
        {
            span.SetAttribute(TelemetryAttributeKeys.OperationResult, TelemetryOperationResults.Failed);
            span.RecordException(ex);
            throw;
        }
    }

    private ITelemetrySpan StartSpan(string operationName)
    {
        var span = _tracer.StartSpan($"Investment.BrokerBreakdownService.{operationName}");
        span.SetAttribute(TelemetryAttributeKeys.BoundedContext, "Investment");
        span.SetAttribute(TelemetryAttributeKeys.EntityType, EntityType);
        span.SetAttribute(TelemetryAttributeKeys.OperationName, operationName);
        return span;
    }

    private static decimal CalculateNetInvested(Asset asset)
    {
        var (totalBought, totalSold, _) = NavigationMapper.CalculateTotals(asset);
        return totalBought - totalSold;
    }

    private static decimal CalculateGrossBought(Asset asset) =>
        NavigationMapper.CalculateTotals(asset).TotalBought;
}
