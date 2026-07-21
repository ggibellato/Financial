using System.Text.Json.Serialization;
using Financial.Domain.Entities;

namespace Financial.Application.DTOs;

/// <summary>
/// Complete details for an asset including operations and credits
/// </summary>
public class AssetDetailsDTO
{
    /// <summary>
    /// Asset name
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// Broker name
    /// </summary>
    public required string BrokerName { get; set; }

    /// <summary>
    /// Portfolio name
    /// </summary>
    public required string PortfolioName { get; set; }

    /// <summary>
    /// Ticker symbol
    /// </summary>
    public required string Ticker { get; set; }

    /// <summary>
    /// ISIN code
    /// </summary>
    public string ISIN { get; set; } = string.Empty;

    /// <summary>
    /// Exchange
    /// </summary>
    public string Exchange { get; set; } = string.Empty;

    /// <summary>
    /// Asset country of origin
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public CountryCode Country { get; set; } = CountryCode.Unknown;

    /// <summary>
    /// Local asset type code (per-country)
    /// </summary>
    public string LocalTypeCode { get; set; } = string.Empty;

    /// <summary>
    /// Global asset classification
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public GlobalAssetClass Class { get; set; } = GlobalAssetClass.Unknown;

    /// <summary>
    /// Current quantity held
    /// </summary>
    public decimal Quantity { get; set; }

    /// <summary>
    /// Average purchase price
    /// </summary>
    public decimal AveragePrice { get; set; }

    /// <summary>
    /// Weighted-average sell price across the asset's Sell transactions; null if never sold
    /// </summary>
    public decimal? AverageSellPrice { get; set; }

    /// <summary>
    /// Position type derived from quantity sign (Long/Flat/Short)
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public PositionType PositionType { get; set; }

    /// <summary>
    /// Total amount bought
    /// </summary>
    public decimal TotalBought { get; set; }

    /// <summary>
    /// Total amount sold
    /// </summary>
    public decimal TotalSold { get; set; }

    /// <summary>
    /// Total credits received (dividends/rent)
    /// </summary>
    public decimal TotalCredits { get; set; }

    /// <summary>
    /// Realized gain/loss from closed (sold) quantity plus credits, computed via
    /// weighted-average cost-basis replay of the asset's transaction history
    /// </summary>
    public decimal RealizedGainLoss { get; set; }

    /// <summary>
    /// List of all transactions (buy/sell)
    /// </summary>
    public List<TransactionDTO> Transactions { get; set; } = new();

    /// <summary>
    /// List of all credits (dividends/rent)
    /// </summary>
    public List<CreditDTO> Credits { get; set; } = new();

    /// <summary>
    /// Cash flows (transactions + credits) used to compute XIRR with credits
    /// </summary>
    public IReadOnlyList<AssetCashFlowDTO> CashFlowsWithCredits { get; set; } = [];

    /// <summary>
    /// Cash flows (transactions only) used to compute XIRR without credits
    /// </summary>
    public IReadOnlyList<AssetCashFlowDTO> CashFlowsWithoutCredits { get; set; } = [];
}

