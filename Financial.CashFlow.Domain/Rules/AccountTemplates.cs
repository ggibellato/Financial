using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Financial.CashFlow.Domain.Enums;
using Financial.Shared.Abstractions.Currencies;

namespace Financial.CashFlow.Domain.Rules;

public static class AccountTemplates
{
    private sealed record Definition(
        AccountTemplate Template, FrozenSet<Capability> Capabilities, ReadOnlyCollection<Currency> Currencies, bool JoinsDefaultGroup);

    private static readonly Definition[] Definitions =
    [
        new(
            AccountTemplate.Bank,
            new[]
            {
                Capability.HasOpeningBalance,
                Capability.ReceivesIncome,
                Capability.HasCategories,
                Capability.CanSendTransfers,
                Capability.CanReceiveTransfers,
                Capability.CanReconcile,
                Capability.CanPayBills,
                Capability.SupportsRoundUp
            }.ToFrozenSet(),
            Array.AsReadOnly([Currency.GBP]),
            JoinsDefaultGroup: true)
    ];

    private static readonly FrozenDictionary<AccountTemplate, Definition> ByTemplate =
        Definitions.ToFrozenDictionary(definition => definition.Template);

    public static IReadOnlyList<AccountTemplate> Available { get; } = Array.AsReadOnly(Definitions.Select(definition => definition.Template).ToArray());

    public static IReadOnlySet<Capability> CapabilitiesOf(AccountTemplate template) => DefinitionOf(template).Capabilities;

    public static IReadOnlyList<Currency> AllowedCurrencies(AccountTemplate template) => DefinitionOf(template).Currencies;

    public static bool JoinsDefaultGroup(AccountTemplate template) =>
        ByTemplate.TryGetValue(template, out var definition) && definition.JoinsDefaultGroup;

    private static Definition DefinitionOf(AccountTemplate template) =>
        ByTemplate.TryGetValue(template, out var definition)
            ? definition
            : throw new ArgumentException($"Account template '{template}' is not defined.");
}
