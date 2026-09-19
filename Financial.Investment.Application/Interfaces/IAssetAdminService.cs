using Financial.Investment.Application.DTOs;
using Financial.Investment.Application.Enums;

namespace Financial.Investment.Application.Interfaces;

/// <summary>
/// Asset identity lifecycle (create/edit), as distinct from moving or archiving one.
/// </summary>
/// <remarks>
/// Separate from <see cref="IAssetMoveService"/> deliberately, the same split <see cref="IPortfolioService"/>
/// makes against move/archive concerns — a consumer that only manages identity should not have to
/// know about relocation rules, and delete/archive stays owned by <see cref="IAssetMoveService"/>.
/// </remarks>
public interface IAssetAdminService
{
    /// <summary>Lists every asset across both Active and Historic brokers/portfolios.</summary>
    IReadOnlyList<AssetAdminDTO> GetAssets();

    /// <summary>
    /// Registers a new asset's identity under an Active broker's portfolio, with zero quantity.
    /// </summary>
    /// <exception cref="ArgumentException">A required field is missing, or the ISIN is not blank and not validly formatted.</exception>
    /// <exception cref="KeyNotFoundException">No Active broker, or no portfolio by that name under it, exists.</exception>
    /// <exception cref="Domain.Exceptions.InvestmentRuleViolationException">The name is already in use under that portfolio.</exception>
    Task<AssetAdminDTO> CreateAssetAsync(AssetAdminCreateDTO request);

    /// <summary>
    /// Updates an existing asset's identity fields, regardless of its transaction history.
    /// </summary>
    /// <param name="scope">
    /// Which broker record to resolve <paramref name="brokerName"/> against first — needed because
    /// the same real-world broker can have both an Active and a Historic record; without it, editing
    /// a Historic asset under a broker that is also Active would resolve to the wrong (Active) record
    /// and fail with "not found", even though the broker name matches.
    /// </param>
    /// <exception cref="ArgumentException">A required field is missing, or the ISIN is not blank and not validly formatted.</exception>
    /// <exception cref="KeyNotFoundException">No broker, portfolio, or asset by that name exists.</exception>
    /// <exception cref="Domain.Exceptions.InvestmentRuleViolationException">The new name is already in use under that portfolio.</exception>
    Task<AssetAdminDTO> UpdateAssetAsync(string brokerName, string portfolioName, string currentName, AssetAdminUpdateDTO request, InvestmentScope scope = InvestmentScope.Active);
}
