using System.Text.Json.Serialization;
using Financial.Investment.Domain.Entities;

namespace Financial.Investment.Application.DTOs;

public class PortfolioDTO
{
    public required string Name { get; set; }

    public required string BrokerName { get; set; }

    public required string BrokerStatus { get; set; }

    public int AssetCount { get; set; }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public ArcaBucket? ArcaBucket { get; set; }
}
