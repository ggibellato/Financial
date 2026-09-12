using System.Text.Json.Serialization;
using Financial.Investment.Domain.Entities;

namespace Financial.Investment.Application.DTOs;

public class SetAssetPriceDTO
{
    public required string BrokerName { get; set; }
    public required string PortfolioName { get; set; }
    public required string AssetName { get; set; }
    public DateOnly Date { get; set; }
    public decimal Price { get; set; }
    public string? Currency { get; set; }
    public string? SourceReference { get; set; }
}

public class DeleteAssetPriceDTO
{
    public required string BrokerName { get; set; }
    public required string PortfolioName { get; set; }
    public required string AssetName { get; set; }
    public DateOnly Date { get; set; }
}

public class AssetPriceSnapshotDTO
{
    public DateOnly Date { get; set; }
    public decimal Price { get; set; }
    public bool IsManual { get; set; }
    public string Currency { get; set; } = string.Empty;

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public PriceSource Source { get; set; }

    public string? SourceReference { get; set; }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public ValuationMethod ValuationMethod { get; set; }

    public DateTimeOffset RetrievedAt { get; set; }
}
