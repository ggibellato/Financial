using System.Text.Json.Serialization;
using Financial.Investment.Domain.Entities;

namespace Financial.Investment.Application.DTOs;

public class TaxWorkbookDTO
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public required Jurisdiction Jurisdiction { get; set; }

    public required string TaxYear { get; set; }

    public required IReadOnlyList<TaxWorkbookEntryDTO> Entries { get; set; }

    public required IReadOnlyList<TaxCategoryTotalDTO> CategoryTotals { get; set; }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public CalculationStatus? CalculationStatus { get; set; }
}
