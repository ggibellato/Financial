using System.Text.Json.Serialization;
using Financial.Investment.Domain.Entities;

namespace Financial.Investment.Application.DTOs;

public class BrokerDTO
{
    public required string Name { get; set; }

    public required string Currency { get; set; }

    public required string Status { get; set; }

    public int PortfolioCount { get; set; }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public CostBasisMethod CostBasisMethod { get; set; } = CostBasisMethod.AverageCost;
}
