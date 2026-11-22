using System.Text.Json.Serialization;
using Financial.Investment.Domain.Entities;

namespace Financial.Investment.Application.DTOs;

public class RealisedGainDTO
{
    public Guid Id { get; set; }

    public Guid TransactionId { get; set; }

    public DateTime Date { get; set; }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public CostBasisMethod Method { get; set; }

    public List<RealisedGainLotDTO> LotsConsumed { get; set; } = new();

    public decimal QuantitySold { get; set; }

    public decimal Proceeds { get; set; }

    public decimal CostBasis { get; set; }

    public decimal GainLoss { get; set; }

    public string Currency { get; set; } = string.Empty;

    public string TaxYear { get; set; } = string.Empty;

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public RealisedGainStatus Status { get; set; }

    public Guid? SupersededByRecordId { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
}
