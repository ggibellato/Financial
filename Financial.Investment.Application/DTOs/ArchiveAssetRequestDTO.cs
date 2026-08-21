namespace Financial.Investment.Application.DTOs;

/// <summary>
/// Identifies a fully closed asset and the Historic portfolio it should be retired into.
/// </summary>
/// <remarks>
/// Carries no scope fields. Archiving always runs from Active Investments into Historic
/// Investments, so naming the scopes would only create combinations to reject - and it would make
/// the reverse direction expressible, which it must never be.
/// </remarks>
public class ArchiveAssetRequestDTO
{
    /// <summary>The broker holding the asset. Its Historic record is created if it has none yet.</summary>
    public required string BrokerName { get; set; }

    /// <summary>Active portfolio the asset is in now.</summary>
    public required string SourcePortfolioName { get; set; }

    /// <summary>Asset to archive. Its quantity must be exactly zero.</summary>
    public required string AssetName { get; set; }

    /// <summary>Historic portfolio to archive into, existing or to be created.</summary>
    public required string DestinationPortfolioName { get; set; }
}
