using Financial.Application.DTOs;
using Financial.Domain.Entities;

namespace Financial.Application.Services;

internal readonly record struct AssetTotals(decimal TotalBought, decimal TotalSold, decimal TotalCredits);

internal static class PortfolioAssetSummaryBuilder
{
    internal static IReadOnlyList<PortfolioAssetSummaryItemDTO> Build(
        IEnumerable<Asset> assets,
        DateTime today,
        Func<AssetTotals, decimal> weightBasisSelector)
    {
        var computed = assets
            .Select(a => ComputeAssetData(a, today, weightBasisSelector))
            .ToList();
        var portfolioWeightBasis = computed.Sum(c => c.WeightBasis);

        return computed
            .OrderBy(c => c.AssetName, StringComparer.CurrentCultureIgnoreCase)
            .Select(c => ToDTO(c, CalculateWeight(c.WeightBasis, portfolioWeightBasis)))
            .ToList();
    }

    private static AssetComputedData ComputeAssetData(
        Asset asset,
        DateTime today,
        Func<AssetTotals, decimal> weightBasisSelector)
    {
        var (totalBought, totalSold, totalCredits) = NavigationMapper.CalculateTotals(asset);
        var totals = new AssetTotals(totalBought, totalSold, totalCredits);
        var weightBasis = weightBasisSelector(totals);
        var realizedGainLoss = NavigationMapper.CalculateRealizedGainLoss(asset);
        var averageSellPrice = NavigationMapper.CalculateAverageSellPrice(asset);

        var firstBuyDate = asset.Transactions
            .Where(t => t.Type == Transaction.TransactionType.Buy)
            .Select(t => (DateTime?)t.Date)
            .DefaultIfEmpty(null)
            .Min();

        var cashFlows = AssetCashFlowBuilder.BuildWithCredits(asset);
        var creditsAnalysis = ComputeCreditsAnalysis(asset, weightBasis, today);

        return new AssetComputedData(
            asset.Name, asset.Ticker, asset.Exchange, asset.Class,
            firstBuyDate, asset.Quantity, asset.AveragePrice, averageSellPrice,
            totalBought, totalSold, totalBought - totalSold, realizedGainLoss, weightBasis,
            totalCredits, cashFlows,
            creditsAnalysis.LastMonthCredits, creditsAnalysis.LastCreditMonth,
            creditsAnalysis.LastMonthCreditsPercent, creditsAnalysis.CreditFrequencyPerYear,
            creditsAnalysis.EstimatedAnnualCredits, creditsAnalysis.EstimatedAnnualPercent,
            creditsAnalysis.CurrentMonthCredits);
    }

    private static CreditsAnalysis ComputeCreditsAnalysis(Asset asset, decimal weightBasis, DateTime today)
    {
        var pastCredits = asset.Credits.Where(c => c.Date <= today).ToList();

        var lastCreditMonth = pastCredits
            .GroupBy(c => (c.Date.Year, c.Date.Month))
            .Select(g => g.Key)
            .OrderByDescending(g => g.Year).ThenByDescending(g => g.Month)
            .Cast<(int Year, int Month)?>()
            .FirstOrDefault();

        var lastMonthCreditsString = lastCreditMonth.HasValue
            ? $"{lastCreditMonth.Value.Year:D4}-{lastCreditMonth.Value.Month:D2}"
            : null;

        var lastMonthCredits = lastCreditMonth.HasValue
            ? pastCredits
                .Where(c => c.Date.Year == lastCreditMonth.Value.Year && c.Date.Month == lastCreditMonth.Value.Month)
                .Sum(c => c.Value)
            : 0m;

        decimal? lastMonthCreditsPercent = lastCreditMonth.HasValue && weightBasis != 0m
            ? lastMonthCredits / weightBasis * 100m
            : null;

        var frequencyPerYear = DetectCreditFrequency(asset.Credits);

        decimal? estimatedAnnualCredits = frequencyPerYear.HasValue
            ? lastMonthCredits * frequencyPerYear.Value
            : null;

        decimal? estimatedAnnualPercent = estimatedAnnualCredits.HasValue && weightBasis != 0m
            ? estimatedAnnualCredits.Value / weightBasis * 100m
            : null;

        var currentMonthCredits = asset.Credits
            .Where(c => c.Date.Year == today.Year && c.Date.Month == today.Month)
            .Sum(c => c.Value);

        return new CreditsAnalysis(
            lastMonthCredits, lastMonthCreditsString, lastMonthCreditsPercent,
            frequencyPerYear, estimatedAnnualCredits, estimatedAnnualPercent,
            currentMonthCredits);
    }

    private static int? DetectCreditFrequency(IEnumerable<Credit> credits)
    {
        var distinctMonths = credits
            .Select(c => c.Date.Year * 12 + (c.Date.Month - 1))
            .Distinct()
            .OrderBy(m => m)
            .ToList();

        if (distinctMonths.Count < 2)
            return null;

        var totalGap = distinctMonths[^1] - distinctMonths[0];
        var averageGap = (double)totalGap / (distinctMonths.Count - 1);

        return averageGap switch
        {
            <= 1.5 => 12,
            <= 3.5 => 4,
            <= 5.0 => 3,
            _ => null
        };
    }

    private static PortfolioAssetSummaryItemDTO ToDTO(AssetComputedData c, decimal weight) =>
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

    private static decimal CalculateWeight(decimal weightBasis, decimal portfolioWeightBasis) =>
        portfolioWeightBasis == 0m ? 0m : weightBasis / portfolioWeightBasis * 100m;

    private sealed record AssetComputedData(
        string AssetName, string Ticker, string Exchange, GlobalAssetClass Class,
        DateTime? FirstInvestmentDate, decimal CurrentQuantity, decimal AveragePrice, decimal? AverageSellPrice,
        decimal TotalBought, decimal TotalSold, decimal TotalInvested, decimal RealizedGainLoss, decimal WeightBasis,
        decimal TotalCredits, IReadOnlyList<AssetCashFlowDTO> CashFlows,
        decimal LastMonthCredits, string? LastCreditMonth, decimal? LastMonthCreditsPercent,
        int? CreditFrequencyPerYear, decimal? EstimatedAnnualCredits, decimal? EstimatedAnnualPercent,
        decimal CurrentMonthCredits);

    private sealed record CreditsAnalysis(
        decimal LastMonthCredits, string? LastCreditMonth, decimal? LastMonthCreditsPercent,
        int? CreditFrequencyPerYear, decimal? EstimatedAnnualCredits, decimal? EstimatedAnnualPercent,
        decimal CurrentMonthCredits);
}
