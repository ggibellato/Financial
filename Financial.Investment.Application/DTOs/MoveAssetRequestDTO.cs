namespace Financial.Investment.Application.DTOs;

/// <summary>
/// Identifies an asset and where it should go. Deliberately carries no "create the portfolio"
/// flag: <see cref="DestinationPortfolioName"/> alone decides the outcome - an existing name moves
/// the asset into it, an unused one creates it. A flag could contradict the graph and would add a
/// failure mode with no meaning to the user.
/// </summary>
public class MoveAssetRequestDTO
{
    /// <summary>The broker holding the asset. It holds both ends of the move.</summary>
    public required string BrokerName { get; set; }

    /// <summary>
    /// Investment scope both ends sit in: "active" or "historic". One field rather than two,
    /// because a move stays within a scope - crossing from Active into Historic is archiving, a
    /// separate operation with its own rule about the position being closed first. Naming a single
    /// scope makes the crossing unrequestable rather than requestable and refused.
    /// </summary>
    public required string Scope { get; set; }

    /// <summary>Portfolio the asset is in now.</summary>
    public required string SourcePortfolioName { get; set; }

    /// <summary>Asset to move.</summary>
    public required string AssetName { get; set; }

    /// <summary>Portfolio to move the asset into, existing or to be created.</summary>
    public required string DestinationPortfolioName { get; set; }
}
