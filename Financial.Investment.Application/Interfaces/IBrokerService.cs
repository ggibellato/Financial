using Financial.Investment.Application.DTOs;

namespace Financial.Investment.Application.Interfaces;

/// <summary>
/// Broker lifecycle: registering, renaming, and retiring a broker record, as distinct from the
/// read-only navigation tree (<see cref="INavigationService"/>) or portfolio/asset lifecycle
/// (<see cref="IPortfolioService"/>, <see cref="IAssetMoveService"/>).
/// </summary>
public interface IBrokerService
{
    /// <summary>Lists every broker, Active and Historic.</summary>
    IReadOnlyList<BrokerDTO> GetBrokers();

    /// <summary>
    /// Registers a new Active broker.
    /// </summary>
    /// <exception cref="ArgumentException">Name or currency is missing.</exception>
    /// <exception cref="Domain.Exceptions.InvestmentRuleViolationException">The name is already in use.</exception>
    Task<BrokerDTO> CreateBrokerAsync(BrokerCreateDTO request);

    /// <summary>
    /// Renames and/or re-currencies an existing broker, Active or Historic.
    /// </summary>
    /// <exception cref="ArgumentException">Name or currency is missing.</exception>
    /// <exception cref="KeyNotFoundException">No broker by <paramref name="currentName"/> exists.</exception>
    /// <exception cref="Domain.Exceptions.InvestmentRuleViolationException">The new name is already in use.</exception>
    Task<BrokerDTO> UpdateBrokerAsync(string currentName, BrokerUpdateDTO request);

    /// <summary>
    /// Deletes an empty broker: an Active one archives to Historic, a Historic one is removed
    /// permanently.
    /// </summary>
    /// <exception cref="KeyNotFoundException">No broker by this name exists.</exception>
    /// <exception cref="Domain.Exceptions.InvestmentRuleViolationException">The broker still has portfolios.</exception>
    Task DeleteBrokerAsync(string name);
}
