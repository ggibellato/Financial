using Financial.CashFlow.Domain.Entities;

namespace Financial.CashFlow.Infrastructure.Persistence;

/// <summary>
/// Lookup tables built fresh on every <see cref="CashFlowDataConverter"/> read, resolving a
/// reference property's Id against the seeded collection deserialized earlier in the same call.
/// </summary>
public sealed class ReferenceResolutionContext
{
    public Dictionary<Guid, Bank> Banks { get; } = new();
    public Dictionary<Guid, IncomeSource> IncomeSources { get; } = new();
    public Dictionary<Guid, InvestmentAccount> InvestmentAccounts { get; } = new();
}
