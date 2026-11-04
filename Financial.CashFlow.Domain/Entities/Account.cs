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
    public const decimal RequiredActiveSplitTotal = 100m;

    private IReadOnlySet<Capability>? _capabilitiesOverride;
    private IReadOnlyList<Pot> _pots = [];

    public Guid Id { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public AccountTemplate Template { get; private set; }
    public Currency Currency { get; private set; }
    public bool IsActive { get; private set; }
    public OpeningBalance? OpeningBalance { get; private set; }
    public RoundUpSetting? RoundUp { get; private set; }
    public IReadOnlyList<Pot>? Pots => Has(Capability.HasPots) ? _pots : null;
    public Category? Category { get; private set; }
    public bool RoundsUp => RoundUp?.Enabled == true;
    public string TemplateName => Template.ToString();
    public int TemplateOrder => AccountTemplates.Available.ToList().IndexOf(Template);
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

    public void AddPot(Pot pot)
    {
        ArgumentNullException.ThrowIfNull(pot);
        EnsureHasPots();

        if (_pots.Any(existing => string.Equals(existing.Name, pot.Name, StringComparison.OrdinalIgnoreCase)))
        {
            throw new ArgumentException(DuplicatePotNameMessage(pot.Name));
        }

        _pots = [.. _pots, pot];
    }

    public void ReplacePots(IReadOnlyList<PotChange> changes)
    {
        EnsureCanReplacePots(changes);

        foreach (var pot in _pots)
        {
            var change = changes.Single(candidate => candidate.Id == pot.Id);
            pot.Update(change.Name, change.SplitPercentage, change.IsActive);
        }

        _pots =
        [
            .. _pots,
            .. changes.Where(change => change.Id is null).Select(change => Pot.Create(change.Name, change.SplitPercentage, change.IsActive))
        ];
    }

    public void EnsureCanReplacePots(IReadOnlyList<PotChange> changes)
    {
        ArgumentNullException.ThrowIfNull(changes);
        EnsureHasPots();

        if (changes.Any(change => change.Id is Guid id && !OwnsPot(id)))
        {
            throw new ArgumentException($"That pot isn't in {Name}.");
        }

        if (_pots.Any(pot => changes.Count(change => change.Id == pot.Id) != 1))
        {
            throw new ArgumentException("Every pot must be in the list; pots can't be deleted.");
        }

        foreach (var change in changes)
        {
            Pot.Validate(change.Name, change.SplitPercentage);
        }

        if (FindDuplicatePotName(changes) is string duplicate)
        {
            throw new ArgumentException(DuplicatePotNameMessage(duplicate));
        }

        EnsureActiveSplitTotal(changes.Where(change => change.IsActive).Sum(change => change.SplitPercentage));
    }

    public static string DuplicatePotNameMessage(string name) => $"A pot named \"{name}\" already exists.";

    public static string? FindDuplicatePotName(IEnumerable<PotChange> changes) =>
        changes
            .Where(change => !string.IsNullOrWhiteSpace(change.Name))
            .GroupBy(change => change.Name.Trim(), StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault(sameName => sameName.Count() > 1)
            ?.Key;

    internal static void EnsureActiveSplitTotal(decimal activeSplitTotal)
    {
        if (activeSplitTotal != RequiredActiveSplitTotal)
        {
            throw new ArgumentException(
                FormattableString.Invariant($"Active pots must total {RequiredActiveSplitTotal:0}% (now {activeSplitTotal:0.####}%)."));
        }
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

    private void EnsureHasPots()
    {
        if (!Has(Capability.HasPots))
        {
            throw new ArgumentException($"{Name} has no pots.");
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
