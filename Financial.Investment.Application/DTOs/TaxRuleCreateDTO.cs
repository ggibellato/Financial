using System;
using System.Text.Json.Serialization;
using Financial.Investment.Domain.Entities;

namespace Financial.Investment.Application.DTOs;

public class TaxRuleCreateDTO
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public required Jurisdiction Jurisdiction { get; set; }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public required EventCategory EventCategory { get; set; }

    public required string Label { get; set; }

    public string Description { get; set; } = string.Empty;

    public required DateOnly EffectiveFrom { get; set; }

    public DateOnly? EffectiveTo { get; set; }
}
