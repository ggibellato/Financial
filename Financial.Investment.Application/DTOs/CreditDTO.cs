namespace Financial.Investment.Application.DTOs;

public class CreditDTO
{
    public Guid Id { get; set; }

    public DateTime Date { get; set; }

    public required string Type { get; set; }

    public decimal Value { get; set; }

    public decimal Withheld { get; set; }

    public decimal IntermediationFee { get; set; }

    public decimal NetAmount { get; set; }

    public string Currency { get; set; } = string.Empty;

    public FxRateSnapshotDTO? FxRateSnapshot { get; set; }

    public decimal? SharesForDividend { get; set; }

    public decimal? AverageCostPerShare { get; set; }

    public decimal? InvestedAmount { get; set; }

    public decimal? PriceOnDate { get; set; }

    public decimal? MarketValueOnDate { get; set; }

    public decimal? YieldOnInvested { get; set; }

    public decimal? YieldOnMarket { get; set; }
}

