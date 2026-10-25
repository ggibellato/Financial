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

    public Guid Id { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public AccountTemplate Template { get; private set; }
    public Currency Currency { get; private set; }
    public bool IsActive { get; private set; }
    public OpeningBalance? OpeningBalance { get; private set; }
    public RoundUpSetting? RoundUp { get; private set; }
    public DateOnly? NextInvoiceDueDate { get; private set; }
    public bool RoundsUp => RoundUp?.Enabled == true;
    public string TemplateName => Template.ToString();
    public bool JoinsDefaultGroup => AccountTemplates.JoinsDefaultGroup(Template);

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

    public void SetNextInvoiceDueDate(DateOnly? dueDate)
    {
        CardAccountRule.EnsureIsCard(this);
        NextInvoiceDueDate = dueDate;
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
        NextInvoiceDueDate = Has(Capability.HasStatements) ? NextInvoiceDueDate : null;
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
