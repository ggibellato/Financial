using Financial.Investment.Application.DTOs;
using Financial.Investment.Application.Enums;
using Financial.Investment.Application.Interfaces;
using Financial.Investment.Application.Validation;
using Financial.Investment.Domain.Entities;
using Financial.Shared.Abstractions;

namespace Financial.Investment.Application.Services;

public sealed class NavigationService : INavigationService
{
    private const string EntityType = "Navigation";

    private readonly IInvestmentRepository _repository;
    private readonly ITelemetryTracer _tracer;

    public NavigationService(IInvestmentRepository repository, ITelemetryTracer tracer)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _tracer = tracer ?? throw new ArgumentNullException(nameof(tracer));
    }

    public TreeNodeDTO GetNavigationTree(InvestmentScope scope = InvestmentScope.Active)
    {
        using var span = StartSpan("GetNavigationTree");
        try
        {
            var brokers = GetBrokers(scope);

            var rootNode = new TreeNodeDTO
            {
                NodeType = TreeNodeType.Investments,
                DisplayName = "All Investments"
            };

            foreach (var broker in brokers)
            {
                rootNode.Children.Add(NavigationMapper.BuildBrokerTreeNode(broker));
            }

            span.SetAttribute(TelemetryAttributeKeys.OperationResult, TelemetryOperationResults.Success);
            return rootNode;
        }
        catch (Exception ex)
        {
            span.SetAttribute(TelemetryAttributeKeys.OperationResult, TelemetryOperationResults.Failed);
            span.RecordException(ex);
            throw;
        }
    }

    public AssetDetailsDTO? GetAssetDetails(string brokerName, string portfolioName, string assetName, InvestmentScope scope = InvestmentScope.Active)
    {
        using var span = StartSpan("GetAssetDetails");
        try
        {
            if (AssetContextValidator.IsInvalid(brokerName, portfolioName, assetName))
            {
                span.SetAttribute(TelemetryAttributeKeys.OperationResult, TelemetryOperationResults.Success);
                return null;
            }

            var asset = _repository.GetAsset(brokerName, portfolioName, assetName, scope);

            if (asset == null)
            {
                span.SetAttribute(TelemetryAttributeKeys.OperationResult, TelemetryOperationResults.Success);
                return null;
            }

            var transactions = asset.Transactions
                .Select(NavigationMapper.MapTransaction)
                .OrderByDescending(t => t.Date)
                .ToList();

            var credits = asset.Credits
                .Select(NavigationMapper.MapCredit)
                .OrderByDescending(c => c.Date)
                .ToList();

            var priceHistory = asset.PriceHistory
                .Select(NavigationMapper.MapPriceEntry)
                .OrderByDescending(p => p.Date)
                .ToList();

            var (totalBought, totalSold, totalCredits) = NavigationMapper.CalculateTotals(asset);

            span.SetAttribute(TelemetryAttributeKeys.OperationResult, TelemetryOperationResults.Success);
            return new AssetDetailsDTO
            {
                Name = asset.Name,
                BrokerName = brokerName,
                PortfolioName = portfolioName,
                Ticker = asset.Ticker,
                ISIN = asset.ISIN,
                Exchange = asset.Exchange,
                Country = asset.Country,
                LocalTypeCode = asset.LocalTypeCode,
                Class = asset.Class,
                Quantity = asset.Quantity,
                AveragePrice = asset.AveragePrice,
                AverageSellPrice = asset.AverageSellPrice,
                PositionType = NavigationMapper.PositionTypeFor(asset, scope),
                TotalBought = totalBought,
                TotalSold = totalSold,
                TotalCredits = totalCredits,
                RealizedGainLoss = asset.RealizedGainLoss,
                Transactions = transactions,
                Credits = credits,
                PriceHistory = priceHistory,
                CashFlowsWithCredits = AssetCashFlowBuilder.BuildWithCredits(asset),
                CashFlowsWithoutCredits = AssetCashFlowBuilder.BuildWithoutCredits(asset)
            };
        }
        catch (Exception ex)
        {
            span.SetAttribute(TelemetryAttributeKeys.OperationResult, TelemetryOperationResults.Failed);
            span.RecordException(ex);
            throw;
        }
    }

    public IEnumerable<BrokerNodeDTO> GetBrokers(InvestmentScope scope = InvestmentScope.Active)
    {
        using var span = StartSpan("GetBrokers");
        try
        {
            var brokers = _repository.GetBrokerList(scope).OrderBy(b => b.Name, StringComparer.CurrentCultureIgnoreCase);
            var result = brokers.Select(broker => NavigationMapper.MapBroker(broker, scope)).ToList();

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

    public IEnumerable<AssetNodeDTO> GetAssetsByBrokerPortfolio(string brokerName, string portfolioName)
    {
        using var span = StartSpan("GetAssetsByBrokerPortfolio");
        try
        {
            var result = _repository.GetAssetsByBrokerPortfolio(brokerName, portfolioName)
                .Select(asset => NavigationMapper.MapAsset(asset, InvestmentScope.Active))
                .ToList();

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
        var span = _tracer.StartSpan($"Investment.NavigationService.{operationName}");
        span.SetAttribute(TelemetryAttributeKeys.BoundedContext, "Investment");
        span.SetAttribute(TelemetryAttributeKeys.EntityType, EntityType);
        span.SetAttribute(TelemetryAttributeKeys.OperationName, operationName);
        return span;
    }
}
