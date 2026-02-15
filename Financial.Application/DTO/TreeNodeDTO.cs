namespace Financial.Application.DTO;

/// <summary>
/// Generic hierarchical tree node for navigation
/// </summary>
public class TreeNodeDTO
{
    /// <summary>
    /// Type of node (Investments, Broker, Portfolio, Asset)
    /// </summary>
    public required string NodeType { get; set; }

    /// <summary>
    /// Display name for the node
    /// </summary>
    public required string DisplayName { get; set; }

    /// <summary>
    /// Child nodes
    /// </summary>
    public List<TreeNodeDTO> Children { get; set; } = new();

    /// <summary>
    /// Additional metadata specific to the node type
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = new();
}
