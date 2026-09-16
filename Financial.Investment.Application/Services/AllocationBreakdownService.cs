using Financial.Investment.Application.DTOs;
using Financial.Investment.Application.Interfaces;
using Financial.Investment.Domain.Entities;
using Financial.Shared.Abstractions.Currencies;
using Financial.Shared.Abstractions.Observability;
using Financial.Shared.Abstractions.Validation;
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

    public AllocationBreakdownService(
        IInvestmentRepository repository,
        IHoldingValuationService holdingValuationService,
        ITelemetryTracer tracer,
        ILogger<AllocationBreakdownService> logger)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _holdingValuationService = holdingValuationService ?? throw new ArgumentNullException(nameof(holdingValuationService));
        _tracer = tracer ?? throw new ArgumentNullException(nameof(tracer));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public AllocationBreakdownDTO GetAllocationBreakdown()
    {
        using var span = StartSpan(OperationName);
        try
        {
            var holdings = CollectActiveHoldings(_repository.GetInvestments());
            var result = AllocationBreakdownBuilder.Build(holdings, _holdingValuationService);

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

    private static IReadOnlyList<AllocationHolding> CollectActiveHoldings(Investments investments)
    {
        var holdings = new List<AllocationHolding>();
        foreach (var broker in investments.ActiveBrokers)
        {
            var currency = ParseCurrency(broker.Currency);
            foreach (var asset in broker.Portfolios.SelectMany(portfolio => portfolio.Assets))
            {
                holdings.Add(new AllocationHolding(asset, broker.Name, currency));
            }
        }

        return holdings;
    }

    private static Currency ParseCurrency(string rawCurrency)
    {
        if (!EnumParser.TryParseEnum<Currency>(rawCurrency, out var currency))
        {
            throw new ArgumentException($"Broker currency \"{rawCurrency}\" is not recognized.", nameof(rawCurrency));
        }

        return currency;
    }

    private ITelemetrySpan StartSpan(string operationName)
    {
        _logger.LogInformation("{Operation} started", operationName);
        return _tracer.StartServiceSpan("Investment", nameof(AllocationBreakdownService), operationName, EntityType);
    }
}
