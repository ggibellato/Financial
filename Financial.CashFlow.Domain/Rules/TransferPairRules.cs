using System;
using System.Collections.Generic;
using System.Linq;
using Financial.CashFlow.Domain.Entities;
using Financial.CashFlow.Domain.Enums;

namespace Financial.CashFlow.Domain.Rules;

public sealed class TransferPairRules
{
    private readonly IReadOnlyList<(Capability Source, Capability Destination)> _pairs;

    public TransferPairRules(IEnumerable<(Capability Source, Capability Destination)> pairs)
    {
        ArgumentNullException.ThrowIfNull(pairs);
        _pairs = pairs.ToArray();
    }

    public static TransferPairRules Default { get; } =
        new([(Capability.CanSendTransfers, Capability.CanReceiveTransfers)]);

    public bool Allows(Account source, Account destination) =>
        _pairs.Any(pair => source.Has(pair.Source) && destination.Has(pair.Destination));

    public IReadOnlyList<Account> DestinationsFor(Account source, IEnumerable<Account> accounts) =>
        Counterparts(source, accounts, candidate => Allows(source, candidate));

    public IReadOnlyList<Account> SourcesFor(Account destination, IEnumerable<Account> accounts) =>
        Counterparts(destination, accounts, candidate => Allows(candidate, destination));

    private static IReadOnlyList<Account> Counterparts(Account account, IEnumerable<Account> accounts, Func<Account, bool> isAllowed) =>
        accounts
            .Where(candidate => candidate.IsActive && candidate.Id != account.Id && isAllowed(candidate))
            .OrderBy(candidate => candidate.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();
}
