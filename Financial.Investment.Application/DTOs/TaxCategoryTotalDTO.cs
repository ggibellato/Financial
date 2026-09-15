using System.Text.Json.Serialization;
using Financial.Investment.Domain.Entities;

namespace Financial.Investment.Application.DTOs;

public class TaxCategoryTotalDTO
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public required EventCategory EventCategory { get; set; }

    public decimal? TotalProceeds { get; set; }

    public decimal? TotalCostBasis { get; set; }

    public decimal? TotalGainLoss { get; set; }

    public decimal? TotalGrossAmount { get; set; }

    public decimal? TotalWithheldAmount { get; set; }

    public decimal? TotalNetAmount { get; set; }
}
