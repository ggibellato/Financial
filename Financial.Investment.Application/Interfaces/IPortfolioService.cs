using Financial.Investment.Application.DTOs;
using Financial.Investment.Application.Enums;

namespace Financial.Investment.Application.Interfaces;

/// <summary>
/// Portfolio lifecycle, as distinct from moving the assets inside one.
/// </summary>
/// <remarks>
/// Separate from <see cref="IAssetMoveService"/> deliberately. Deleting a portfolio is not moving
/// an asset, and a consumer that only tidies up should not have to know about archiving.
/// </remarks>
public interface IPortfolioService
{
    /// <summary>Lists every portfolio across both Active and Historic brokers.</summary>
    IReadOnlyList<PortfolioDTO> GetPortfolios();

    /// <summary>
    /// Registers a new portfolio under an Active broker.
    /// </summary>
    /// <exception cref="ArgumentException">A required field is missing.</exception>
    /// <exception cref="KeyNotFoundException">No Active broker by that name exists.</exception>
    /// <exception cref="Domain.Exceptions.InvestmentRuleViolationException">The name is already in use under that broker.</exception>
    Task<PortfolioDTO> CreatePortfolioAsync(PortfolioCreateDTO request);

    /// <summary>
    /// Renames an existing portfolio. The parent broker is fixed and not part of this operation.
    /// </summary>
    /// <exception cref="ArgumentException">A required field is missing.</exception>
    /// <exception cref="KeyNotFoundException">No broker or portfolio by that name exists.</exception>
    /// <exception cref="Domain.Exceptions.InvestmentRuleViolationException">The new name is already in use under that broker.</exception>
    Task<PortfolioDTO> UpdatePortfolioAsync(string brokerName, string currentName, PortfolioUpdateDTO request);

    /// <summary>
    /// Deletes a portfolio that holds no assets.
    /// </summary>
    /// <remarks>
    /// Never called as part of a move. Whether an emptied portfolio should go is the user's choice,
    /// and the repository's write exclusion is not reentrant, so one operation could not invoke the
    /// other even if it wanted to.
    /// </remarks>
    /// <exception cref="KeyNotFoundException">The broker or portfolio does not exist.</exception>
    /// <exception cref="ArgumentException">A required field is missing.</exception>
    /// <exception cref="Domain.Exceptions.InvestmentRuleViolationException">The portfolio still holds assets.</exception>
    Task DeleteEmptyPortfolioAsync(string brokerName, string portfolioName, InvestmentScope scope);
}
