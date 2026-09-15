using System;
using System.Collections.Generic;
using System.Linq;
using Financial.Investment.Domain.Exceptions;
using Financial.Shared.Abstractions.Currencies;

namespace Financial.Investment.Domain.Entities;

public class Investments
{
    private List<Broker> _activeBrokers = new List<Broker>();
    public IReadOnlyCollection<Broker> ActiveBrokers { get => _activeBrokers.AsReadOnly(); private set => SetActiveBrokers(value); }
    private void SetActiveBrokers(IReadOnlyCollection<Broker> data) => EntityGuard.ReplaceAll(_activeBrokers, data);

    private List<Broker> _historicBrokers = new List<Broker>();
    public IReadOnlyCollection<Broker> HistoricBrokers { get => _historicBrokers.AsReadOnly(); private set => SetHistoricBrokers(value); }
    private void SetHistoricBrokers(IReadOnlyCollection<Broker> data) => EntityGuard.ReplaceAll(_historicBrokers, data);

    /// <summary>
    /// Defaults to GBP - matching three of the four existing brokers - both for a freshly created
    /// aggregate and for a pre-existing data file with no "ReportingCurrency" key, since
    /// deserialization still runs this property's initializer via the real (private) constructor.
    /// </summary>
    public Currency ReportingCurrency { get; private set; } = Currency.GBP;

    /// <summary>
    /// Defaults to true so a pre-existing data file with no "ReportingCurrencyEnabled" key (every
    /// file written before this setting existed) keeps behaving exactly as it already did.
    /// </summary>
    public bool ReportingCurrencyEnabled { get; private set; } = true;

    private List<TaxRule> _taxRules = new List<TaxRule>();
    public IReadOnlyCollection<TaxRule> TaxRules { get => _taxRules.AsReadOnly(); private set => SetTaxRules(value); }
    private void SetTaxRules(IReadOnlyCollection<TaxRule> data) => EntityGuard.ReplaceAll(_taxRules, data);

    private Investments() { }

    public static Investments Create() => new();

    public void AddActiveBroker(Broker broker)
    {
        _activeBrokers.Add(broker);
    }

    public void AddHistoricBroker(Broker broker)
    {
        _historicBrokers.Add(broker);
    }

    public void SetReportingCurrency(Currency currency) => ReportingCurrency = currency;

    public void SetReportingCurrencyEnabled(bool enabled) => ReportingCurrencyEnabled = enabled;

    public Broker? FindActiveBroker(string name) => _activeBrokers.FirstOrDefault(broker => broker.Name == name);

    public Broker? FindHistoricBroker(string name) => _historicBrokers.FirstOrDefault(broker => broker.Name == name);

    /// <summary>
    /// Registers a brand-new Active broker.
    /// </summary>
    /// <exception cref="InvestmentRuleViolationException">
    /// A broker by this name already exists, Active or Historic.
    /// </exception>
    public Broker CreateActiveBroker(string name, string currency)
    {
        EnsureNameIsUnique(name, excluding: null);

        var broker = Broker.Create(name, currency);
        _activeBrokers.Add(broker);
        return broker;
    }

    /// <summary>
    /// Renames and/or re-currencies a broker, Active or Historic.
    /// </summary>
    /// <exception cref="KeyNotFoundException">No broker by <paramref name="currentName"/> exists.</exception>
    /// <exception cref="InvestmentRuleViolationException">
    /// <paramref name="newName"/> already belongs to a different broker.
    /// </exception>
    public Broker RenameBroker(string currentName, string newName, string newCurrency)
    {
        var broker = FindActiveBroker(currentName) ?? FindHistoricBroker(currentName)
            ?? throw new KeyNotFoundException($"Broker \"{currentName}\" was not found.");

        if (newName != currentName)
        {
            EnsureNameIsUnique(newName, excluding: broker);
        }

        broker.Update(newName, newCurrency);
        return broker;
    }

    /// <summary>
    /// Deletes an empty broker: an Active one moves to Historic (unless a Historic record of the same
    /// name already exists, in which case that existing record already is its history and nothing is
    /// duplicated); a Historic one is removed permanently.
    /// </summary>
    /// <exception cref="KeyNotFoundException">No broker by this name exists.</exception>
    /// <exception cref="InvestmentRuleViolationException">The broker still has portfolios.</exception>
    public void DeleteBroker(string name)
    {
        var activeBroker = FindActiveBroker(name);
        if (activeBroker is not null)
        {
            EnsureEmpty(activeBroker);
            _activeBrokers.Remove(activeBroker);
            if (FindHistoricBroker(name) is null)
            {
                _historicBrokers.Add(activeBroker);
            }
            return;
        }

        var historicBroker = FindHistoricBroker(name)
            ?? throw new KeyNotFoundException($"Broker \"{name}\" was not found.");

        EnsureEmpty(historicBroker);
        _historicBrokers.Remove(historicBroker);
    }

    private void EnsureNameIsUnique(string name, Broker? excluding)
    {
        var collision = FindActiveBroker(name) ?? FindHistoricBroker(name);
        if (collision is not null && !ReferenceEquals(collision, excluding))
        {
            throw new InvestmentRuleViolationException($"A broker named \"{name}\" already exists.");
        }
    }

    private static void EnsureEmpty(Broker broker)
    {
        if (broker.Portfolios.Count != 0)
        {
            throw new InvestmentRuleViolationException("Cannot delete a broker that still has portfolios.");
        }
    }

    /// <summary>
    /// Retires a fully closed asset from Active Investments into a Historic portfolio of the same
    /// broker, creating that portfolio - and the broker's Historic record itself - when they do not
    /// exist yet.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Lives on the root rather than on <see cref="Broker"/> because it spans both broker
    /// collections: the same real-world broker is two independent records here, and the Historic one
    /// may not exist at all. It is also the only direction offered - an asset never comes back out
    /// of Historic Investments, which is an archive of closed positions.
    /// </para>
    /// <para>
    /// The asset is relocated, not rebuilt, exactly as in <see cref="Broker.MoveAsset"/>: its
    /// transactions, credits and price history are the record of a position that is now closed, and
    /// archiving must not disturb them.
    /// </para>
    /// </remarks>
    /// <exception cref="ArgumentException">The destination name is blank or whitespace.</exception>
    /// <exception cref="KeyNotFoundException">The Active broker, portfolio, or asset does not exist.</exception>
    /// <exception cref="InvestmentRuleViolationException">
    /// The asset still holds a position, or the Historic destination already holds an asset of that
    /// name, or the new name duplicates an existing Historic portfolio.
    /// </exception>
    public void ArchiveAsset(string brokerName, string sourcePortfolioName, string assetName, string destinationPortfolioName)
    {
        var destinationName = Broker.NormalizeDestinationName(destinationPortfolioName);

        var activeBroker = FindActiveBroker(brokerName)
            ?? throw new KeyNotFoundException($"Broker \"{brokerName}\" was not found in Active Investments.");

        var source = activeBroker.FindPortfolio(sourcePortfolioName)
            ?? throw new KeyNotFoundException($"Portfolio \"{sourcePortfolioName}\" was not found under broker \"{brokerName}\".");

        var asset = source.FindAsset(assetName)
            ?? throw new KeyNotFoundException($"Asset \"{assetName}\" was not found in portfolio \"{sourcePortfolioName}\".");

        if (asset.Quantity != 0)
        {
            throw new InvestmentRuleViolationException(
                $"\"{assetName}\" still holds a position of {asset.Quantity}. Only a fully closed asset can be archived into Historic Investments.");
        }

        var destination = ResolveHistoricDestination(activeBroker, destinationName, assetName);

        source.RemoveAsset(assetName);
        destination.AddAsset(asset);
    }

    /// <summary>
    /// The two broker collections are not mirrors - a broker can be trading with nothing closed yet -
    /// so archiving its first closed asset is what brings its Historic record into being. That is not
    /// a new broker in the user's terms; it is the same one appearing in the historic view for the
    /// first time, so its name and currency are copied and nothing is asked.
    /// </summary>
    private Portfolio ResolveHistoricDestination(Broker activeBroker, string destinationName, string assetName)
    {
        var historicBroker = FindHistoricBroker(activeBroker.Name);
        if (historicBroker is not null)
        {
            // Resolve before anything is created: this can refuse, and a half-built Historic side
            // would survive in memory and be written by the next unrelated save.
            return historicBroker.ResolveDestination(destinationName, assetName);
        }

        historicBroker = Broker.Create(activeBroker.Name, activeBroker.Currency);
        AddHistoricBroker(historicBroker);

        // A broker created this instant holds nothing, so there is no name to clash with.
        return historicBroker.AddPortfolio(destinationName);
    }

    public TaxRule CreateTaxRule(
        Jurisdiction jurisdiction,
        EventCategory eventCategory,
        string label,
        string description,
        DateOnly effectiveFrom,
        DateOnly? effectiveTo)
    {
        EnsureNoOverlap(jurisdiction, eventCategory, effectiveFrom, effectiveTo, excluding: null);

        var rule = TaxRule.Create(jurisdiction, eventCategory, label, description, effectiveFrom, effectiveTo);
        _taxRules.Add(rule);
        return rule;
    }

    public TaxRule UpdateTaxRule(Guid id, string label, string description, DateOnly effectiveFrom, DateOnly? effectiveTo)
    {
        var rule = FindTaxRule(id) ?? throw new KeyNotFoundException($"Tax rule \"{id}\" was not found.");

        EnsureNoOverlap(rule.Jurisdiction, rule.EventCategory, effectiveFrom, effectiveTo, excluding: rule);
        rule.Update(label, description, effectiveFrom, effectiveTo);
        return rule;
    }

    public void DeleteTaxRule(Guid id)
    {
        var rule = FindTaxRule(id) ?? throw new KeyNotFoundException($"Tax rule \"{id}\" was not found.");

        var affectedTaxYears = FinalClassificationTaxYearsForRule(id);
        if (affectedTaxYears.Count > 0)
        {
            throw new InvestmentRuleViolationException(
                $"Cannot delete tax rule \"{rule.Label}\" while a final classification still depends on it, " +
                $"for tax year(s): {string.Join(", ", affectedTaxYears)}.");
        }

        _taxRules.Remove(rule);
    }

    public TaxRule? FindTaxRule(Guid id) => _taxRules.FirstOrDefault(rule => rule.Id == id);

    private IReadOnlyList<string> FinalClassificationTaxYearsForRule(Guid taxRuleId) =>
        ActiveBrokers.Concat(HistoricBrokers)
            .SelectMany(broker => broker.Portfolios)
            .SelectMany(portfolio => portfolio.Assets)
            .SelectMany(asset => asset.TaxClassifications)
            .Where(classification =>
                classification.Status == TaxClassificationStatus.Active &&
                classification.CalculationStatus == CalculationStatus.Final &&
                classification.TaxRuleId == taxRuleId)
            .Select(classification => classification.TaxYear)
            .Distinct()
            .OrderBy(taxYear => taxYear, StringComparer.Ordinal)
            .ToList();

    public TaxRule? FindApplicableTaxRule(Jurisdiction jurisdiction, EventCategory eventCategory, DateOnly date) =>
        _taxRules.FirstOrDefault(rule =>
            rule.Jurisdiction == jurisdiction && rule.EventCategory == eventCategory && rule.Applies(date));

    private void EnsureNoOverlap(
        Jurisdiction jurisdiction,
        EventCategory eventCategory,
        DateOnly effectiveFrom,
        DateOnly? effectiveTo,
        TaxRule? excluding)
    {
        var overlapping = _taxRules.FirstOrDefault(rule =>
            !ReferenceEquals(rule, excluding) &&
            rule.Jurisdiction == jurisdiction &&
            rule.EventCategory == eventCategory &&
            RangesOverlap(effectiveFrom, effectiveTo, rule.EffectiveFrom, rule.EffectiveTo));

        if (overlapping is not null)
        {
            throw new InvestmentRuleViolationException(
                $"This range overlaps existing rule \"{overlapping.Label}\" ({overlapping.EffectiveFrom:yyyy-MM-dd}–{(overlapping.EffectiveTo?.ToString("yyyy-MM-dd") ?? "present")}).");
        }
    }

    private static bool RangesOverlap(DateOnly aFrom, DateOnly? aTo, DateOnly bFrom, DateOnly? bTo)
    {
        var aEnd = aTo ?? DateOnly.MaxValue;
        var bEnd = bTo ?? DateOnly.MaxValue;
        return aFrom < bEnd && bFrom < aEnd;
    }
}
