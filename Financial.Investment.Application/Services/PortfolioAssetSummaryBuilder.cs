using Financial.Investment.Application.DTOs;
using Financial.Investment.Application.Enums;
using Financial.Investment.Application.Interfaces;
using Financial.Investment.Domain.Entities;
using Financial.Investment.Domain.Rules;

namespace Financial.Investment.Application.Services;

internal static class PortfolioAssetSummaryBuilder
{
    internal static IReadOnlyList<PortfolioAssetSummaryItemDTO> Build(
        IEnumerable<Asset> assets,
        DateTime today,
        InvestmentScope scope,
        IHoldingValuationService holdingValuationService)
    {
        var computed = assets
            .Select(a => ComputeAssetData(a, today, scope, holdingValuationService))
            .ToList();
        var portfolioWeightBasis = computed
            .Where(c => c.WeightBasis.HasValue)
            .Sum(c => c.WeightBasis!.Value);

        return computed
            .OrderBy(c => c.AssetName, StringComparer.CurrentCultureIgnoreCase)
            .Select(c => ToDTO(c, CalculateWeight(c.WeightBasis, portfolioWeightBasis)))
            .ToList();
    }

    private static AssetComputedData ComputeAssetData(
        Asset asset, DateTime today, InvestmentScope scope, IHoldingValuationService holdingValuationService)
    {
        var totals = AssetTotals.For(asset);
        var valuation = holdingValuationService.GetValuation(asset, scope);
        var bases = AssetAmountBases.For(scope, totals, valuation.MarketValue);
        var realizedGainLoss = asset.RealizedGainLoss;
        var averageSellPrice = asset.AverageSellPrice;

        var firstBuyDate = asset.Transactions
            .Where(t => t.Type == Transaction.TransactionType.Buy)
            .Select(t => (DateTime?)t.Date)
            .DefaultIfEmpty(null)
            .Min();

        var cashFlows = AssetCashFlowBuilder.BuildWithCredits(asset);
        var creditsAnalysis = CreditsAnalysisCalculator.Calculate(asset.Credits, bases.IncomeYieldBasis, today);

        return new AssetComputedData(
            asset.Name, asset.Ticker, asset.Exchange, asset.Class,
            firstBuyDate, asset.Quantity, asset.AveragePrice, averageSellPrice,
            totals.TotalBought, totals.TotalSold, bases.InvestedAmount, realizedGainLoss, bases.WeightBasis,
            totals.TotalCredits, cashFlows, valuation,
            creditsAnalysis.LastMonthCredits, creditsAnalysis.LastCreditMonth,
            creditsAnalysis.LastMonthCreditsPercent, creditsAnalysis.CreditFrequencyPerYear,
            creditsAnalysis.EstimatedAnnualCredits, creditsAnalysis.EstimatedAnnualPercent,
            creditsAnalysis.CurrentMonthCredits);
    }

    private static PortfolioAssetSummaryItemDTO ToDTO(AssetComputedData c, decimal? weight) =>
        new()
        {
            AssetName = c.AssetName,
            Ticker = c.Ticker,
            Exchange = c.Exchange,
            Class = c.Class,
            FirstInvestmentDate = c.FirstInvestmentDate,
            CurrentQuantity = c.CurrentQuantity,
            AveragePrice = c.AveragePrice,
            AverageSellPrice = c.AverageSellPrice,
            TotalBought = c.TotalBought,
            TotalSold = c.TotalSold,
            TotalInvested = c.TotalInvested,
            RealizedGainLoss = c.RealizedGainLoss,
            PortfolioWeight = weight,
            MarketValue = c.Valuation.MarketValue,
            CostOfUnitsHeld = c.Valuation.CostOfUnitsHeld,
            UnrealisedGain = c.Valuation.UnrealisedGain,
            PriceAsOfDate = c.Valuation.PriceAsOfDate,
            IsPriceStale = c.Valuation.IsPriceStale,
            PriceOnlyReturn = c.Valuation.PriceOnlyReturn,
            TotalReturn = c.Valuation.TotalReturn,
            TotalReturnNetOfTax = c.Valuation.TotalReturnNetOfTax,
            TotalCredits = c.TotalCredits,
            CashFlows = c.CashFlows,
            LastMonthCredits = c.LastMonthCredits,
            LastCreditMonth = c.LastCreditMonth,
            LastMonthCreditsPercent = c.LastMonthCreditsPercent,
            CreditFrequencyPerYear = c.CreditFrequencyPerYear,
            EstimatedAnnualCredits = c.EstimatedAnnualCredits,
            EstimatedAnnualPercent = c.EstimatedAnnualPercent,
            CurrentMonthCredits = c.CurrentMonthCredits
        };

    private static decimal? CalculateWeight(decimal? weightBasis, decimal portfolioWeightBasis)
    {
        if (weightBasis is null) return null;
        return portfolioWeightBasis == 0m ? 0m : weightBasis.Value / portfolioWeightBasis * 100m;
    }

    private sealed record AssetComputedData(
        string AssetName, string Ticker, string Exchange, GlobalAssetClass Class,
        DateTime? FirstInvestmentDate, decimal CurrentQuantity, decimal AveragePrice, decimal? AverageSellPrice,
        decimal TotalBought, decimal TotalSold, decimal TotalInvested, decimal RealizedGainLoss, decimal? WeightBasis,
        decimal TotalCredits, IReadOnlyList<AssetCashFlowDTO> CashFlows, HoldingValuation Valuation,
        decimal LastMonthCredits, string? LastCreditMonth, decimal? LastMonthCreditsPercent,
        int? CreditFrequencyPerYear, decimal? EstimatedAnnualCredits, decimal? EstimatedAnnualPercent,
        decimal CurrentMonthCredits);
}
