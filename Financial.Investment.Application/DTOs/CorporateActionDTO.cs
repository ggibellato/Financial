using System.Text.Json.Serialization;
using Financial.Investment.Domain.Entities;

namespace Financial.Investment.Application.DTOs;

public class CorporateActionDTO
{
    public Guid Id { get; set; }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public CorporateAction.CorporateActionType Type { get; set; }

    public DateTime EffectiveDate { get; set; }

    public decimal? RatioFactor { get; set; }

    public string? Note { get; set; }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public CorporateAction.CorporateActionRole? Role { get; set; }

    public Guid? CorrelationId { get; set; }

    public string? LinkedAssetName { get; set; }

    public decimal? ExchangeRatio { get; set; }

    public decimal? CashInLieu { get; set; }

    public decimal? ConvertedQuantity { get; set; }

    public decimal? CarriedCostBasis { get; set; }

    public decimal? AllocationPercentage { get; set; }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public CalculationStatus? CalculationStatus { get; set; }
}
