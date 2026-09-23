using System.Text.Json.Serialization;
using Financial.Investment.Domain.Entities;

namespace Financial.Investment.Application.DTOs;

public class CorporateActionMergerCreateDTO
{
    public required string BrokerName { get; set; }
    public required string PortfolioName { get; set; }
    public required string SourceAssetName { get; set; }
    public DateTime EffectiveDate { get; set; }
    public decimal ExchangeRatio { get; set; }
    public decimal? CashInLieuAmount { get; set; }
    public string? Note { get; set; }
    public required string TargetAssetName { get; set; }
    public bool CreateTargetAssetInline { get; set; }

    public string? TargetISIN { get; set; }
    public string? TargetExchange { get; set; }
    public string? TargetTicker { get; set; }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public CountryCode? TargetCountry { get; set; }

    public string? TargetLocalTypeCode { get; set; }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public GlobalAssetClass? TargetClass { get; set; }
}
