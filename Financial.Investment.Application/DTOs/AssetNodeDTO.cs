using System.Text.Json.Serialization;
using Financial.Investment.Domain.Entities;

namespace Financial.Investment.Application.DTOs;

public class AssetNodeDTO
{
    public required string Name { get; set; }

    public required string Ticker { get; set; }

    public string Exchange { get; set; } = string.Empty;

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public CountryCode Country { get; set; } = CountryCode.Unknown;

    public string LocalTypeCode { get; set; } = string.Empty;

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public GlobalAssetClass Class { get; set; } = GlobalAssetClass.Unknown;

    public string ISIN { get; set; } = string.Empty;

    public decimal Quantity { get; set; }

    public decimal AveragePrice { get; set; }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public PositionType PositionType { get; set; }

    public int TransactionCount { get; set; }

    public int CreditCount { get; set; }
}

