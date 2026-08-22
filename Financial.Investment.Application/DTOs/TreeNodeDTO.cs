using System.Text.Json.Serialization;

namespace Financial.Investment.Application.DTOs;

public class TreeNodeDTO
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public required TreeNodeType NodeType { get; set; }

    public required string DisplayName { get; set; }

    public List<TreeNodeDTO> Children { get; set; } = new();

    public Dictionary<string, object> Metadata { get; set; } = new();
}

