namespace Financial.Investment.Application.DTOs;

public sealed class HoldingsTotalsDTO
{
    public int HoldingCount { get; init; }
    public int UnvaluedHoldingCount { get; init; }
    public decimal Invested { get; init; }
    public decimal? MarketValue { get; init; }
    public decimal? InvestedShareOfBase { get; init; }
    public decimal? MarketShareOfBase { get; init; }
    public decimal? ProfitAmount { get; init; }
    public decimal? ProfitPercent { get; init; }
    public decimal? ProfitWithCreditsPercent { get; init; }
    public decimal TotalCredits { get; init; }
    public decimal? CreditsPercentOfInvested { get; init; }
    public decimal LastCreditAmount { get; init; }
    public decimal? LastCreditPercentOfMarketValue { get; init; }
}

public sealed class HoldingRowDTO
{
    public PortfolioAssetSummaryItemDTO Asset { get; init; } = new();
    public decimal? InvestedShareOfGroup { get; init; }
    public decimal? MarketShareOfGroup { get; init; }
    public decimal? InvestedShareOfBase { get; init; }
    public decimal? MarketShareOfBase { get; init; }
}

public sealed class HoldingsGroupDTO
{
    public string Name { get; init; } = string.Empty;
    public IReadOnlyList<HoldingRowDTO> Holdings { get; init; } = [];
    public HoldingsTotalsDTO Subtotal { get; init; } = new();
}

public sealed class BrokerHoldingsDTO
{
    public IReadOnlyList<string> Currencies { get; init; } = [];
    public IReadOnlyList<HoldingsGroupDTO> Groups { get; init; } = [];
    public HoldingsTotalsDTO Total { get; init; } = new();
}
