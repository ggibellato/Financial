using System.Text.Json.Serialization;
using Financial.Investment.Domain.Entities;

namespace Financial.Investment.Application.DTOs;

public class CorporateActionSpinOffCreateDTO
{
    public required string BrokerName { get; set; }
    public required string PortfolioName { get; set; }
    public required string ParentAssetName { get; set; }
    public DateTime EffectiveDate { get; set; }
    public decimal QuantityReceived { get; set; }
    public decimal AllocationPercentage { get; set; }
    public string? Note { get; set; }
    public required string NewAssetName { get; set; }
    public bool CreateNewAssetInline { get; set; }

    public string? NewISIN { get; set; }
    public string? NewExchange { get; set; }
    public string? NewTicker { get; set; }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public CountryCode? NewCountry { get; set; }

    public string? NewLocalTypeCode { get; set; }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public GlobalAssetClass? NewClass { get; set; }
}
