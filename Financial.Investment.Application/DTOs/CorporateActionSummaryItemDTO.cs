using System.Text.Json.Serialization;
using Financial.Investment.Domain.Entities;

namespace Financial.Investment.Application.DTOs;

public class CorporateActionSummaryItemDTO
{
    public required string AssetName { get; set; }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public CorporateAction.CorporateActionType Type { get; set; }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public CorporateAction.CorporateActionRole? Role { get; set; }

    public DateTime EffectiveDate { get; set; }

    public string? LinkedAssetName { get; set; }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public CalculationStatus? CalculationStatus { get; set; }
}
