using System.Text.Json.Serialization;
using Financial.Investment.Domain.Entities;

namespace Financial.Investment.Application.DTOs;

public class AssetDetailsDTO
{
    public required string Name { get; set; }

    public required string BrokerName { get; set; }

    public required string PortfolioName { get; set; }

    public required string Ticker { get; set; }

    public string ISIN { get; set; } = string.Empty;

    public string Exchange { get; set; } = string.Empty;

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public CountryCode Country { get; set; } = CountryCode.Unknown;

    public string LocalTypeCode { get; set; } = string.Empty;

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public GlobalAssetClass Class { get; set; } = GlobalAssetClass.Unknown;

    public decimal Quantity { get; set; }

    public decimal AveragePrice { get; set; }

    /// <summary>
    /// Weighted-average sell price across the asset's Sell transactions; null if never sold
    /// </summary>
    public decimal? AverageSellPrice { get; set; }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public PositionType PositionType { get; set; }

    public decimal TotalBought { get; set; }

    public decimal TotalSold { get; set; }

    public decimal TotalCredits { get; set; }

    /// <summary>
    /// Realized gain/loss from closed (sold) quantity plus credits, computed via
    /// weighted-average cost-basis replay of the asset's transaction history
    /// </summary>
    public decimal RealizedGainLoss { get; set; }

    public List<TransactionDTO> Transactions { get; set; } = new();

    public List<CreditDTO> Credits { get; set; } = new();

    /// <summary>
    /// Recorded price history, newest first (manual and automatically-fetched entries)
    /// </summary>
    public List<AssetPriceSnapshotDTO> PriceHistory { get; set; } = new();

    public IReadOnlyList<AssetCashFlowDTO> CashFlowsWithCredits { get; set; } = [];

    public IReadOnlyList<AssetCashFlowDTO> CashFlowsWithoutCredits { get; set; } = [];
}

