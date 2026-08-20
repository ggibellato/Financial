using Financial.Investment.Application.Enums;
using Financial.Investment.Domain.Entities;

namespace Financial.Investment.Application.Interfaces;

public interface IInvestmentRepository
{
    IEnumerable<Asset> GetAssetsByBroker(string name, InvestmentScope scope = InvestmentScope.Active);
    IEnumerable<Asset> GetAssetsByBrokerPortfolio(string broker, string portfolio, InvestmentScope scope = InvestmentScope.Active);
    IEnumerable<Broker> GetBrokerList(InvestmentScope scope = InvestmentScope.Active);
    Asset? GetAsset(string brokerName, string portfolioName, string assetName, InvestmentScope scope = InvestmentScope.Active);

    /// <summary>
    /// Runs <paramref name="applyChanges"/> with exclusive access to the in-memory graph, then
    /// persists the whole document when it reports a change.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Saving re-serializes the entire object graph, so a mutation running on another thread part
    /// way through that walk breaks it. Handing the mutation in is what puts both under the same
    /// exclusion; mutating an entity first and saving afterwards does not, which is why there is
    /// no plain save method to call instead.
    /// </para>
    /// <para>
    /// The delegate must be synchronous, and must not call back into this method: the exclusion
    /// is not reentrant, so a nested call deadlocks the process rather than throwing.
    /// </para>
    /// </remarks>
    /// <param name="applyChanges">
    /// Applies the change and returns true when something changed and must be persisted. Returning
    /// false leaves the document untouched, which is also how an in-memory-only correction such as
    /// a compensating rollback runs under the same exclusion without writing.
    /// </param>
    /// <returns>What <paramref name="applyChanges"/> reported: true when the document was written.</returns>
    Task<bool> ApplyAndSaveAsync(Func<bool> applyChanges);
}
