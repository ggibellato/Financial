using Financial.Investment.Application.DTOs;
using Financial.Investment.Application.Enums;
using Financial.Investment.Application.Interfaces;
using Financial.Investment.Domain.Entities;
using Financial.Investment.Domain.Rules;
using Financial.Shared.Abstractions.Observability;
using Microsoft.Extensions.Logging;

namespace Financial.Investment.Application.Services;

public sealed class DataQualityReportService : IDataQualityReportService
{
    private const string EntityType = "DataQualityReport";

    private readonly IInvestmentRepository _repository;
    private readonly ITelemetryTracer _tracer;
    private readonly ILogger<DataQualityReportService> _logger;

    public DataQualityReportService(IInvestmentRepository repository, ITelemetryTracer tracer, ILogger<DataQualityReportService> logger)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _tracer = tracer ?? throw new ArgumentNullException(nameof(tracer));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public DataQualityReportDTO GenerateReport()
    {
        using var span = StartSpan("GenerateReport");
        try
        {
            var investments = _repository.GetInvestments();
            var activeHoldings = Flatten(investments.ActiveBrokers, InvestmentScope.Active);
            var historicHoldings = Flatten(investments.HistoricBrokers, InvestmentScope.Historic);
            var allHoldings = activeHoldings.Concat(historicHoldings).ToList();

            var salesExceedPurchases = allHoldings
                .Select(h => (h.BrokerName, h.PortfolioName, h.Asset, Violation: SaleCoverageRule.FindFirstUncoveredSale(h.Asset.Transactions)))
                .Where(x => x.Violation is not null)
                .Select(x => new SalesExceedPurchasesFinding(
                    x.BrokerName, x.PortfolioName, x.Asset.Name,
                    x.Violation!.OffendingSale.Date, x.Violation.QuantityHeld, x.Violation.Shortfall))
                .OrderBy(f => f.BrokerName).ThenBy(f => f.PortfolioName).ThenBy(f => f.AssetName)
                .ToList();

            var unpricedOpenHoldings = activeHoldings
                .Where(h => h.Asset.PriceSnapshots.Count == 0)
                .Select(h => new UnpricedOpenHoldingFinding(h.BrokerName, h.PortfolioName, h.Asset.Name))
                .OrderBy(f => f.BrokerName).ThenBy(f => f.PortfolioName).ThenBy(f => f.AssetName)
                .ToList();

            var unclassifiedHoldings = allHoldings
                .Where(h => h.Asset.Class == GlobalAssetClass.Unknown)
                .Select(h => new UnclassifiedHoldingFinding(h.BrokerName, h.PortfolioName, h.Asset.Name, h.Scope))
                .OrderBy(f => f.BrokerName).ThenBy(f => f.PortfolioName).ThenBy(f => f.AssetName)
                .ToList();

            var unpricedKeys = unpricedOpenHoldings.Select(f => (f.BrokerName, f.PortfolioName, f.AssetName)).ToHashSet();
            var unclassifiedAndUnpricedOpenHoldings = unclassifiedHoldings
                .Where(f => f.Scope == InvestmentScope.Active && unpricedKeys.Contains((f.BrokerName, f.PortfolioName, f.AssetName)))
                .ToList();

            var historicHoldingsStillOpen = historicHoldings
                .Where(h => h.Asset.Quantity != 0)
                .Select(h => new HistoricHoldingStillOpenFinding(
                    h.BrokerName, h.PortfolioName, h.Asset.Name, h.Asset.Quantity, OpenPositionCostCalculator.CostOfUnitsHeld(h.Asset)))
                .OrderBy(f => f.BrokerName).ThenBy(f => f.PortfolioName).ThenBy(f => f.AssetName)
                .ToList();

            var result = new DataQualityReportDTO
            {
                SalesExceedPurchases = salesExceedPurchases,
                UnpricedOpenHoldings = unpricedOpenHoldings,
                UnclassifiedHoldings = unclassifiedHoldings,
                HistoricHoldingsStillOpen = historicHoldingsStillOpen,
                UnclassifiedAndUnpricedOpenHoldings = unclassifiedAndUnpricedOpenHoldings,
            };

            span.MarkSuccess();
            _logger.LogInformation("{Operation} completed", "GenerateReport");
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
        return _tracer.StartServiceSpan("Investment", nameof(DataQualityReportService), operationName, EntityType);
    }

    private static List<(string BrokerName, string PortfolioName, Asset Asset, InvestmentScope Scope)> Flatten(
        IEnumerable<Broker> brokers, InvestmentScope scope) =>
        brokers
            .SelectMany(b => b.Portfolios.SelectMany(p => p.Assets.Select(a => (b.Name, p.Name, a, scope))))
            .ToList();
}
