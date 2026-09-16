namespace Financial.Investment.Application.DTOs;

public sealed class PortfolioDashboardDTO
{
    public decimal MarketValue { get; init; }
    public decimal Invested { get; init; }
    public decimal UnrealisedGainLoss { get; init; }
    public decimal RealisedGainLoss { get; init; }
    public decimal IncomeYtd { get; init; }
    public decimal IncomeLifetime { get; init; }
    public decimal? GrossXirr { get; init; }
    public decimal? NetXirr { get; init; }
    public int UnvaluedHoldingCount { get; init; }
    public bool IsPartial { get; init; }
    public string ReportingCurrency { get; init; } = string.Empty;
    public bool IsReportingCurrencyEnabled { get; init; }
    public decimal? ConvertedMarketValue { get; init; }
    public decimal? ConvertedInvested { get; init; }
    public decimal? ConvertedUnrealisedGainLoss { get; init; }
    public decimal? ConvertedRealisedGainLoss { get; init; }
    public decimal? ConvertedIncomeYtd { get; init; }
    public decimal? ConvertedIncomeLifetime { get; init; }
    public decimal? ConvertedGrossXirr { get; init; }
    public decimal? ConvertedNetXirr { get; init; }
    public bool IsReportingCurrencyPartial { get; init; }
    public bool IsReportingCurrencyUnavailable { get; init; }
}
