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
    public CountryCode Country { get; set; } = CountryCode.Unknown;

    /// <summary>
    /// Local asset type code (per-country)
    /// </summary>
    public string LocalTypeCode { get; set; } = string.Empty;

    /// <summary>
    /// Global asset classification
    /// </summary>
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
    /// Whether the asset is active
    /// </summary>
    public bool IsActive { get; set; }

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
    /// List of all operations (buy/sell)
    /// </summary>
    public List<OperationDTO> Operations { get; set; } = new();

    /// <summary>
    /// List of all credits (dividends/rent)
    /// </summary>
    public List<CreditDTO> Credits { get; set; } = new();
}

