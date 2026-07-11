using Financial.Application.DTOs;
using Financial.Application.Interfaces;
using Financial.Domain.Entities;

namespace Financial.Application.Services;

public sealed class PortfolioAssetSummaryQueryService : IPortfolioAssetSummaryQueryService
{
    private readonly IRepository _repository;

    public PortfolioAssetSummaryQueryService(IRepository repository)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
    }

    public IReadOnlyList<PortfolioAssetSummaryItemDTO> GetPortfolioAssetsSummary(string brokerName, string portfolioName)
    {
        if (string.IsNullOrWhiteSpace(brokerName) || string.IsNullOrWhiteSpace(portfolioName))
            return [];

        var assets = _repository.GetAssetsByBrokerPortfolio(brokerName, portfolioName).ToList();

        if (assets.Count == 0)
            return [];

        var today = DateTime.Today;
        var computed = assets.Select(a => ComputeAssetData(a, today)).ToList();
        var portfolioTotalInvested = computed.Sum(c => c.TotalInvested);

        return NavigationMapper
            .OrderByNameWithEncerradasLast(computed, c => c.AssetName)
            .Select(c => ToDTO(c, CalculateWeight(c.TotalInvested, portfolioTotalInvested)))
            .ToList();
    }

    private static AssetComputedData ComputeAssetData(Asset asset, DateTime today)
    {
        var (totalBought, totalSold, totalCredits) = NavigationMapper.CalculateTotals(asset);
        var firstBuyDate = asset.Transactions
            .Where(t => t.Type == Transaction.TransactionType.Buy)
            .Select(t => (DateTime?)t.Date)
            .DefaultIfEmpty(null)
            .Min();

        var cashFlows = BuildCashFlows(asset);
        var creditsAnalysis = ComputeCreditsAnalysis(asset, totalBought - totalSold, today);

        return new AssetComputedData(
            asset.Name, asset.Ticker, asset.Exchange,
            firstBuyDate, asset.Quantity, asset.AveragePrice,
            totalBought, totalSold, totalBought - totalSold,
            totalCredits, cashFlows,
            creditsAnalysis.LastMonthCredits, creditsAnalysis.LastCreditMonth,
            creditsAnalysis.LastMonthCreditsPercent, creditsAnalysis.CreditFrequencyPerYear,
            creditsAnalysis.EstimatedAnnualCredits, creditsAnalysis.EstimatedAnnualPercent,
            creditsAnalysis.CurrentMonthCredits);
    }

    private static CreditsAnalysis ComputeCreditsAnalysis(Asset asset, decimal totalInvested, DateTime today)
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

        decimal? lastMonthCreditsPercent = lastCreditMonth.HasValue && totalInvested != 0m
            ? lastMonthCredits / totalInvested * 100m
            : null;

        var frequencyPerYear = DetectCreditFrequency(asset.Credits);

        decimal? estimatedAnnualCredits = frequencyPerYear.HasValue
            ? lastMonthCredits * frequencyPerYear.Value
            : null;

        decimal? estimatedAnnualPercent = estimatedAnnualCredits.HasValue && totalInvested != 0m
            ? estimatedAnnualCredits.Value / totalInvested * 100m
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

    private static IReadOnlyList<AssetCashFlowDTO> BuildCashFlows(Asset asset)
    {
        var flows = new List<AssetCashFlowDTO>();

        foreach (var t in asset.Transactions)
        {
            var amount = t.Type == Transaction.TransactionType.Buy ? -t.TotalPrice : t.TotalPrice;
            flows.Add(new AssetCashFlowDTO { Date = t.Date, Amount = amount });
        }

        foreach (var c in asset.Credits)
            flows.Add(new AssetCashFlowDTO { Date = c.Date, Amount = c.Value });

        flows.Sort((a, b) => a.Date.CompareTo(b.Date));
        return flows;
    }

    private static PortfolioAssetSummaryItemDTO ToDTO(AssetComputedData c, decimal weight) =>
        new()
        {
            AssetName = c.AssetName,
            Ticker = c.Ticker,
            Exchange = c.Exchange,
            FirstInvestmentDate = c.FirstInvestmentDate,
            CurrentQuantity = c.CurrentQuantity,
            AveragePrice = c.AveragePrice,
            TotalBought = c.TotalBought,
            TotalSold = c.TotalSold,
            TotalInvested = c.TotalInvested,
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

    private static decimal CalculateWeight(decimal totalInvested, decimal portfolioTotalInvested) =>
        portfolioTotalInvested == 0m ? 0m : totalInvested / portfolioTotalInvested * 100m;

    private sealed record AssetComputedData(
        string AssetName, string Ticker, string Exchange,
        DateTime? FirstInvestmentDate, decimal CurrentQuantity, decimal AveragePrice,
        decimal TotalBought, decimal TotalSold, decimal TotalInvested,
        decimal TotalCredits, IReadOnlyList<AssetCashFlowDTO> CashFlows,
        decimal LastMonthCredits, string? LastCreditMonth, decimal? LastMonthCreditsPercent,
        int? CreditFrequencyPerYear, decimal? EstimatedAnnualCredits, decimal? EstimatedAnnualPercent,
        decimal CurrentMonthCredits);

    private sealed record CreditsAnalysis(
        decimal LastMonthCredits, string? LastCreditMonth, decimal? LastMonthCreditsPercent,
        int? CreditFrequencyPerYear, decimal? EstimatedAnnualCredits, decimal? EstimatedAnnualPercent,
        decimal CurrentMonthCredits);
}
