using System;
using System.Collections.Generic;
using Financial.CashFlow.Domain.Enums;
using Financial.CashFlow.Domain.Rules;
using Financial.CashFlow.Domain.ValueObjects;
using Financial.Shared.Abstractions.Currencies;

namespace Financial.CashFlow.Domain.Entities;

public class Account
{
    private IReadOnlySet<Capability>? _capabilitiesOverride;

    public Guid Id { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public AccountTemplate Template { get; private set; }
    public Currency Currency { get; private set; }
    public bool IsActive { get; private set; }
    public OpeningBalance? OpeningBalance { get; private set; }
    public RoundUpSetting? RoundUp { get; private set; }
    public bool RoundsUp => RoundUp?.Enabled == true;

    private Account() { }

    public static Account Create(string name, AccountTemplate template, Currency currency)
        => Build(name, template, currency, capabilitiesOverride: null);

    internal static Account CreateWithCapabilities(string name, Currency currency, IReadOnlySet<Capability> capabilities, bool isActive = true)
    {
        ArgumentNullException.ThrowIfNull(capabilities);
        var account = Build(name, AccountTemplate.Bank, currency, capabilities);
        account.IsActive = isActive;
        return account;
    }

    public bool Has(Capability capability) =>
        (_capabilitiesOverride ?? AccountTemplates.CapabilitiesOf(Template)).Contains(capability);

    public void Update(string name)
    {
        EnsureNameIsPresent(name);
        Name = name;
    }

    public void SetOpeningBalance(decimal amount, DateOnly startDate)
    {
        if (!Has(Capability.HasOpeningBalance))
        {
            throw new ArgumentException($"{Name} has no opening balance.");
        }

        OpeningBalance = OpeningBalance.Create(amount, startDate);
    }

    public void SetRoundUp(bool enabled)
    {
        if (!Has(Capability.SupportsRoundUp))
        {
            throw new ArgumentException($"{Name} does not support round-up.");
        }

        RoundUp = new RoundUpSetting(enabled);
    }

    private static Account Build(string name, AccountTemplate template, Currency currency, IReadOnlySet<Capability>? capabilitiesOverride)
    {
        EnsureNameIsPresent(name);

        var account = new Account
        {
            Id = Guid.NewGuid(),
            Name = name,
            Template = template,
            Currency = currency,
            IsActive = true,
            _capabilitiesOverride = capabilitiesOverride
        };

        if (account.Has(Capability.HasOpeningBalance))
        {
            account.OpeningBalance = OpeningBalance.Create(0m, default);
        }

        if (account.Has(Capability.SupportsRoundUp))
        {
            account.RoundUp = new RoundUpSetting(false);
        }

        return account;
    }

    private static void EnsureNameIsPresent(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Account name is required.");
        }
    }
}
