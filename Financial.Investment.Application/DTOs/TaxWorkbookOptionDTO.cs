using System.Text.Json.Serialization;
using Financial.Investment.Domain.Entities;

namespace Financial.Investment.Application.DTOs;

public class TaxWorkbookOptionDTO
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public required Jurisdiction Jurisdiction { get; set; }

    public required string TaxYear { get; set; }
}
