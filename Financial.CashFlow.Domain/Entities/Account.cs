using System;
using System.Collections.Generic;
using System.Linq;
using Financial.CashFlow.Domain.Enums;
using Financial.CashFlow.Domain.Rules;
using Financial.CashFlow.Domain.ValueObjects;
using Financial.Shared.Abstractions.Currencies;

namespace Financial.CashFlow.Domain.Entities;

public class Account
{
    public const int MaxNameLength = 60;

    private IReadOnlySet<Capability>? _capabilitiesOverride;
    private IReadOnlyList<ReserveBucket> _pots = [];

    public Guid Id { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public AccountTemplate Template { get; private set; }
    public Currency Currency { get; private set; }
    public bool IsActive { get; private set; }
    public OpeningBalance? OpeningBalance { get; private set; }
    public RoundUpSetting? RoundUp { get; private set; }
    public IReadOnlyList<ReserveBucket>? Pots => Has(Capability.HasPots) ? _pots : null;
    public Category? Category { get; private set; }
    public bool RoundsUp => RoundUp?.Enabled == true;
    public string TemplateName => Template.ToString();
    public bool JoinsDefaultGroup => AccountTemplates.JoinsDefaultGroup(Template);
    public bool CanPay => Has(Capability.HasCategories) && !Has(Capability.HasStatements);
    public bool BalanceIsOwed => Has(Capability.HasStatements);

    private Account() { }

    public static Account Create(string name, AccountTemplate template, Currency currency)
    {
        EnsureCurrencyIsAllowed(template, currency);
        return Build(name, template, currency, capabilitiesOverride: null);
    }

    internal static Account CreateWithCapabilities(
        string name, Currency currency, IReadOnlySet<Capability> capabilities, bool isActive = true, AccountTemplate template = AccountTemplate.Bank)
    {
        ArgumentNullException.ThrowIfNull(capabilities);
        var account = Build(name, template, currency, capabilities);
        account.IsActive = isActive;
        return account;
    }

    public bool Has(Capability capability) =>
        (_capabilitiesOverride ?? AccountTemplates.CapabilitiesOf(Template)).Contains(capability);

    public void Update(string name)
    {
        Name = ValidName(name);
    }

    public void SetActive(bool isActive)
    {
        IsActive = isActive;
    }

    public void ChangeTemplate(AccountTemplate template, Currency currency)
    {
        EnsureCurrencyIsAllowed(template, currency);
        Template = template;
        Currency = currency;
        _capabilitiesOverride = null;
        ApplyCapabilityDefaults();
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

    public void AddPot(ReserveBucket pot)
    {
        ArgumentNullException.ThrowIfNull(pot);

        if (!Has(Capability.HasPots))
        {
            throw new ArgumentException($"{Name} has no pots.");
        }

        if (_pots.Any(existing => string.Equals(existing.Name, pot.Name, StringComparison.OrdinalIgnoreCase)))
        {
            throw new ArgumentException($"A reserve bucket named \"{pot.Name}\" already exists.");
        }

        _pots = [.. _pots, pot];
    }

    public void SetCategory(Category category)
    {
        ArgumentNullException.ThrowIfNull(category);

        if (!Has(Capability.HasPots))
        {
            throw new ArgumentException($"{Name} has no category setting.");
        }

        Category = category;
    }

    public bool OwnsPot(Guid potId) => _pots.Any(pot => pot.Id == potId);

    public void EnsureCanPay()
    {
        if (Has(Capability.HasStatements))
        {
            throw new ArgumentException($"{Name} is a card account and can't pay.");
        }

        if (!Has(Capability.HasCategories))
        {
            throw new ArgumentException($"Account '{Name}' cannot be a payment source.");
        }
    }

    private static Account Build(string name, AccountTemplate template, Currency currency, IReadOnlySet<Capability>? capabilitiesOverride)
    {
        var account = new Account
        {
            Id = Guid.NewGuid(),
            Name = ValidName(name),
            Template = template,
            Currency = currency,
            IsActive = true,
            _capabilitiesOverride = capabilitiesOverride
        };

        account.ApplyCapabilityDefaults();
        return account;
    }

    private void ApplyCapabilityDefaults()
    {
        OpeningBalance = Has(Capability.HasOpeningBalance) ? OpeningBalance ?? OpeningBalance.Create(0m, default) : null;
        RoundUp = Has(Capability.SupportsRoundUp) ? RoundUp ?? new RoundUpSetting(false) : null;
        if (!Has(Capability.HasPots))
        {
            _pots = [];
            Category = null;
        }
    }

    private static string ValidName(string name)
    {
        var trimmed = name?.Trim();
        if (string.IsNullOrEmpty(trimmed))
        {
            throw new ArgumentException("Account name is required.");
        }

        if (trimmed.Length > MaxNameLength)
        {
            throw new ArgumentException($"Account name must be {MaxNameLength} characters or fewer.");
        }

        return trimmed;
    }

    private static void EnsureCurrencyIsAllowed(AccountTemplate template, Currency currency)
    {
        var allowed = AccountTemplates.AllowedCurrencies(template);
        if (!allowed.Contains(currency))
        {
            throw new ArgumentException($"{template} accounts must be {string.Join(" or ", allowed)}.");
        }
    }
}
