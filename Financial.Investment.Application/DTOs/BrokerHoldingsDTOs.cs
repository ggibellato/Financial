using System.Text.Json.Serialization;
using Financial.Investment.Domain.Entities;

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
    public decimal? LastMonthStartValue { get; init; }
    public decimal? ThisMonthStartValue { get; init; }
    public decimal? MonthChangePercent { get; init; }
    public int MonthStartExcludedCount { get; init; }
}

public sealed class MonthStartValueDTO
{
    public decimal? Value { get; init; }
    public decimal? UnitPrice { get; init; }
    public DateOnly? PriceDate { get; init; }
    public string? Currency { get; init; }
}

public sealed class MonthStartColumnDTO
{
    public DateOnly Date { get; init; }
    public DateOnly EarliestPriceDate { get; init; }
    public DateOnly LatestPriceDate { get; init; }
}

public sealed class HoldingRowDTO
{
    public PortfolioAssetSummaryItemDTO Asset { get; init; } = new();
    public decimal? InvestedShareOfGroup { get; init; }
    public decimal? MarketShareOfGroup { get; init; }
    public decimal? InvestedShareOfBase { get; init; }
    public decimal? MarketShareOfBase { get; init; }
    public MonthStartValueDTO LastMonthStart { get; init; } = new();
    public MonthStartValueDTO ThisMonthStart { get; init; } = new();
    public decimal? MonthChangePercent { get; init; }
}

public sealed class HoldingsSegmentDTO
{
    public string? Name { get; init; }
    public IReadOnlyList<HoldingRowDTO> Holdings { get; init; } = [];
    public HoldingsTotalsDTO Subtotal { get; init; } = new();
    public decimal? InvestedShareOfGroup { get; init; }
    public decimal? MarketShareOfGroup { get; init; }
}

public enum ArcaSection
{
    Arca,
    ReserveFund,
    NotInArca
}

public sealed class HoldingsGroupDTO
{
    public string Name { get; init; } = string.Empty;
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public ArcaBucket? ArcaBucket { get; init; }
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public ArcaSection? ArcaSection { get; init; }
    public IReadOnlyList<HoldingRowDTO> Holdings { get; init; } = [];
    public IReadOnlyList<HoldingsSegmentDTO> Segments { get; init; } = [];
    public HoldingsTotalsDTO Subtotal { get; init; } = new();
}

public sealed class ArcaBucketTotalsDTO
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public ArcaBucket Bucket { get; init; }
    public HoldingsTotalsDTO Totals { get; init; } = new();
}

public sealed class ArcaAllocationDTO
{
    public IReadOnlyList<ArcaBucketTotalsDTO> Buckets { get; init; } = [];
    public HoldingsTotalsDTO ArcaTotal { get; init; } = new();
    public HoldingsTotalsDTO? ReserveFund { get; init; }
    public HoldingsTotalsDTO? NotInArca { get; init; }
    public HoldingsTotalsDTO ArcaAndReserveFundTotal { get; init; } = new();
}

public sealed class BrokerArcaDTO
{
    public IReadOnlyList<string> Currencies { get; init; } = [];
    public ArcaAllocationDTO? Arca { get; init; }
}

public sealed class BrokerHoldingsDTO
{
    public IReadOnlyList<string> Currencies { get; init; } = [];
    public MonthStartColumnDTO LastMonthStart { get; init; } = new();
    public MonthStartColumnDTO ThisMonthStart { get; init; } = new();
    public IReadOnlyList<HoldingsGroupDTO> Groups { get; init; } = [];
    public HoldingsTotalsDTO Total { get; init; } = new();
    public ArcaAllocationDTO? Arca { get; init; }
}
