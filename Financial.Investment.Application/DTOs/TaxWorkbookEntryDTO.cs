using System;
using System.Text.Json.Serialization;
using Financial.Investment.Domain.Entities;

namespace Financial.Investment.Application.DTOs;

public class TaxWorkbookEntryDTO
{
    public required Guid Id { get; set; }

    public required DateTime Date { get; set; }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public required EventCategory EventCategory { get; set; }

    public decimal? Proceeds { get; set; }

    public decimal? CostBasis { get; set; }

    public decimal? GainLoss { get; set; }

    public decimal? GrossAmount { get; set; }

    public decimal? WithheldAmount { get; set; }

    public decimal? NetAmount { get; set; }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public required CalculationStatus CalculationStatus { get; set; }

    public required Guid EvidenceReference { get; set; }

    public string? TaxRuleLabel { get; set; }
}
