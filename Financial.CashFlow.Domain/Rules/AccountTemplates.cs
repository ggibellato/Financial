using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using Financial.CashFlow.Domain.Enums;

namespace Financial.CashFlow.Domain.Rules;

public static class AccountTemplates
{
    private static readonly FrozenDictionary<AccountTemplate, FrozenSet<Capability>> CapabilitySets =
        new Dictionary<AccountTemplate, FrozenSet<Capability>>
        {
            [AccountTemplate.Bank] = new[]
            {
                Capability.HasOpeningBalance,
                Capability.ReceivesIncome,
                Capability.HasCategories,
                Capability.CanSendTransfers,
                Capability.CanReceiveTransfers,
                Capability.CanReconcile,
                Capability.CanPayBills,
                Capability.SupportsRoundUp
            }.ToFrozenSet()
        }.ToFrozenDictionary();

    private static readonly FrozenSet<AccountTemplate> DefaultGroupTemplates = new[] { AccountTemplate.Bank }.ToFrozenSet();

    public static IReadOnlySet<Capability> CapabilitiesOf(AccountTemplate template) =>
        CapabilitySets.TryGetValue(template, out var capabilities)
            ? capabilities
            : throw new ArgumentException($"Account template '{template}' is not defined.");

    public static bool JoinsDefaultGroup(AccountTemplate template) => DefaultGroupTemplates.Contains(template);
}
