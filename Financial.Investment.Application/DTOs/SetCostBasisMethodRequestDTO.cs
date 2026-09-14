using System.Text.Json.Serialization;
using Financial.Investment.Domain.Entities;

namespace Financial.Investment.Application.DTOs;

public class SetCostBasisMethodRequestDTO
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public CostBasisMethod Method { get; set; }
}
