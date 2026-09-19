using Financial.Investment.Application.DTOs;
using Financial.Investment.Application.Enums;
using Financial.Investment.Domain.Entities;

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
    /// <param name="scope">
    /// Which record to resolve <paramref name="currentName"/> against first — a real-world broker
    /// can have both an Active and a Historic record under the same name (e.g. once one closed
    /// position has been archived while others are still trading), so without it, editing the
    /// Historic record would resolve to the Active one instead and fail with "not found".
    /// </param>
    /// <exception cref="ArgumentException">Name or currency is missing.</exception>
    /// <exception cref="KeyNotFoundException">No broker by <paramref name="currentName"/> exists.</exception>
    /// <exception cref="Domain.Exceptions.InvestmentRuleViolationException">The new name is already in use.</exception>
    Task<BrokerDTO> UpdateBrokerAsync(string currentName, BrokerUpdateDTO request, InvestmentScope scope = InvestmentScope.Active);

    /// <summary>
    /// Deletes an empty broker: an Active one archives to Historic, a Historic one is removed
    /// permanently.
    /// </summary>
    /// <exception cref="KeyNotFoundException">No broker by this name exists.</exception>
    /// <exception cref="Domain.Exceptions.InvestmentRuleViolationException">The broker still has portfolios.</exception>
    Task DeleteBrokerAsync(string name);

    /// <param name="scope">
    /// Which record to resolve <paramref name="brokerName"/> against first — see
    /// <see cref="UpdateBrokerAsync"/> for why this is needed.
    /// </param>
    /// <exception cref="KeyNotFoundException">No broker by this name exists.</exception>
    Task<BrokerDTO> SetCostBasisMethodAsync(string brokerName, CostBasisMethod method, InvestmentScope scope = InvestmentScope.Active);
}
