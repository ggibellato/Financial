namespace Financial.Investment.Application.DTOs;

public sealed class AggregatedSummaryDTO
{
    public decimal TotalBought { get; init; }
    public decimal TotalSold { get; init; }
    public decimal TotalCredits { get; init; }
    public decimal TotalInvested { get; init; }
    public decimal? MarketValue { get; init; }
    public int HoldingCount { get; init; }
    public int UnvaluedHoldingCount { get; init; }
    public decimal? PriceOnlyReturn { get; init; }
    public decimal? TotalReturn { get; init; }
    public decimal? TotalReturnNetOfTax { get; init; }

    /// <summary>The currency every <c>Converted*</c> figure below is expressed in.</summary>
    public string ReportingCurrency { get; init; } = string.Empty;

    /// <summary>False when the reporting-currency setting is turned off; every <c>Converted*</c>
    /// figure is then null and no conversion is attempted.</summary>
    public bool IsReportingCurrencyEnabled { get; init; } = true;
    public decimal? ConvertedMarketValue { get; init; }
    public decimal? ConvertedInvested { get; init; }
    public decimal? ConvertedUnrealisedGainLoss { get; init; }
    public decimal? ConvertedTotalReturn { get; init; }
    public decimal? ConvertedTotalReturnNetOfTax { get; init; }

    /// <summary>True when some, but not all, contributing records converted successfully.</summary>
    public bool IsPartial { get; init; }

    /// <summary>True when no contributing record could be converted; every <c>Converted*</c> figure above is null.</summary>
    public bool IsReportingCurrencyUnavailable { get; init; }
}
