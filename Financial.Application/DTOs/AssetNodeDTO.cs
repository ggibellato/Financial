using System.Text.Json.Serialization;
using Financial.Domain.Entities;

namespace Financial.Application.DTOs;

/// <summary>
/// Represents an asset node in the navigation tree
/// </summary>
public class AssetNodeDTO
{
    /// <summary>
    /// Asset name/ticker (e.g., "BCIA11", "VUSA")
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// Ticker symbol
    /// </summary>
    public required string Ticker { get; set; }

    /// <summary>
    /// Exchange where the asset is traded (e.g., "BVMF", "LSE")
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
    /// ISIN code (if available)
    /// </summary>
    public string ISIN { get; set; } = string.Empty;

    /// <summary>
    /// Current quantity held
    /// </summary>
    public decimal Quantity { get; set; }

    /// <summary>
    /// Average purchase price
    /// </summary>
    public decimal AveragePrice { get; set; }

    /// <summary>
    /// Position type derived from quantity sign (Long/Flat/Short)
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public PositionType PositionType { get; set; }

    /// <summary>
    /// Number of transactions (buy/sell)
    /// </summary>
    public int TransactionCount { get; set; }

    /// <summary>
    /// Number of credit entries (dividends/rent)
    /// </summary>
    public int CreditCount { get; set; }
}

