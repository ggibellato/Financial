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
