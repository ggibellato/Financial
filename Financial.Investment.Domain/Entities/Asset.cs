using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Financial.Investment.Domain.Exceptions;
using Financial.Investment.Domain.Rules;

namespace Financial.Investment.Domain.Entities;

public class Asset
{
    public string Name { get; private set; } = string.Empty;

    public string ISIN { get; private set; } = string.Empty;

    public string Exchange { get; private set; } = string.Empty;

    public string Ticker { get; private set; } = string.Empty;

    public CountryCode Country { get; private set; } = CountryCode.Unknown;

    public string LocalTypeCode { get; private set; } = string.Empty;

    public GlobalAssetClass Class { get; private set; } = GlobalAssetClass.Unknown;

    public ValuationMethod ValuationMethod { get; private set; } = ValuationMethod.Unspecified;

    public IncomePolicy IncomePolicy { get; private set; } = IncomePolicy.Unknown;

    public Transactions Transactions { get; private set; } = new();

    public decimal AveragePrice => Transactions.AveragePrice;

    public decimal Quantity => Transactions.Quantity;

    public decimal? AverageSellPrice => Transactions.AverageSellPrice;

    public decimal RealizedGainLoss =>
        DisposalRecords.Where(d => d.Status == DisposalRecordStatus.Active).Sum(d => d.GainLoss) + Credits.Sum(c => c.Value);

    public PositionType PositionType => Quantity switch
    {
        > 0 => PositionType.Long,
        < 0 => PositionType.Short,
        _ => PositionType.Flat
    };

    private List<DisposalRecord> _disposalRecords = new List<DisposalRecord>();
    public IReadOnlyCollection<DisposalRecord> DisposalRecords
    {
        get => _disposalRecords.AsReadOnly();
        private set => EntityGuard.ReplaceAll(_disposalRecords, value);
    }

    private List<CorporateAction> _corporateActions = new List<CorporateAction>();
    public IReadOnlyCollection<CorporateAction> CorporateActions
    {
        get => _corporateActions.AsReadOnly();
        // Relinks Transactions' own corporate-action awareness on every set, not just the mutation
        // methods below - otherwise a JSON reload (Transactions deserializes as a sibling property,
        // never told about them) would replay position/quantity as if no split had ever happened.
        private set
        {
            EntityGuard.ReplaceAll(_corporateActions, value);
            SyncCorporateActionsWithTransactions();
        }
    }

    private void SyncCorporateActionsWithTransactions() => Transactions.SetCorporateActions(_corporateActions.ToList());

    private List<Credit> _credits = new List<Credit>();
    public IReadOnlyCollection<Credit> Credits { get => _credits.AsReadOnly(); private set => SetCredits(value); }
    private void SetCredits(IReadOnlyCollection<Credit> data)
    {
        _credits.Clear();
        foreach (var credit in data)
        {
            AddCredit(credit);
        }
    }

    private List<TaxClassification> _taxClassifications = new List<TaxClassification>();
    public IReadOnlyCollection<TaxClassification> TaxClassifications
    {
        get => _taxClassifications.AsReadOnly();
        private set => EntityGuard.ReplaceAll(_taxClassifications, value);
    }

    private List<AssetPriceSnapshot> _priceSnapshots = new List<AssetPriceSnapshot>();
    public IReadOnlyCollection<AssetPriceSnapshot> PriceSnapshots
    {
        get => _priceSnapshots.AsReadOnly();
        private set => SetPriceSnapshots(value);
    }
    private void SetPriceSnapshots(IReadOnlyCollection<AssetPriceSnapshot> data)
    {
        var replacement = new List<AssetPriceSnapshot>(data.Count);
        foreach (var entry in data)
        {
            UpsertInto(replacement, entry);
        }

        _priceSnapshots = replacement;
    }

    private Asset() { }

    private Asset(string name, string isin, string exchange, string ticker, CountryCode country, string localTypeCode, GlobalAssetClass assetClass) : this()
    {
        Name = name;
        ISIN = isin;
        Exchange = exchange;
        Ticker = ticker;
        Country = country;
        LocalTypeCode = NormalizeLocalTypeCode(localTypeCode);
        Class = assetClass;
    }

    public static Asset Create(string name, string isin, string exchange, string ticker) =>
        new(name, isin, exchange, ticker, CountryCode.Unknown, string.Empty, GlobalAssetClass.Unknown);

    public static Asset Create(string name, string isin, string exchange, string ticker, CountryCode country, string localTypeCode)
    {
        var normalizedLocalTypeCode = NormalizeLocalTypeCode(localTypeCode);
        var assetClass = GlobalAssetClassMapping.Resolve(country, normalizedLocalTypeCode);
        return new Asset(name, isin, exchange, ticker, country, normalizedLocalTypeCode, assetClass);
    }

    public static Asset Create(
        string name,
        string isin,
        string exchange,
        string ticker,
        CountryCode country,
        string localTypeCode,
        GlobalAssetClass assetClass)
    {
        var normalizedLocalTypeCode = NormalizeLocalTypeCode(localTypeCode);
        return new Asset(name, isin, exchange, ticker, country, normalizedLocalTypeCode, assetClass);
    }

    /// <summary>
    /// Updates this asset's identity fields. Callable regardless of transaction history, since
    /// identity (what the asset is) is independent of position (how much of it is held).
    /// </summary>
    /// <remarks>Uniqueness within the parent portfolio is the caller's responsibility (<see cref="Portfolio.UpdateAssetIdentity"/>),
    /// the same division of ownership as <see cref="Broker.RenamePortfolio"/> uses for portfolio names.</remarks>
    public void UpdateIdentity(
        string name,
        string isin,
        string exchange,
        string ticker,
        CountryCode country,
        string localTypeCode,
        GlobalAssetClass assetClass)
    {
        Name = name;
        ISIN = isin;
        Exchange = exchange;
        Ticker = ticker;
        Country = country;
        LocalTypeCode = NormalizeLocalTypeCode(localTypeCode);
        Class = assetClass;
    }

    public void AddTransaction(Transaction transaction) => Transactions.Add(transaction);

    public void AddTransactions(IEnumerable<Transaction> transactions) => Transactions.AddRange(transactions);

    public bool UpdateTransaction(Transaction updatedTransaction) => Transactions.Update(updatedTransaction);

    public bool RemoveTransaction(Guid transactionId) => Transactions.RemoveById(transactionId);

    public void RecordTransaction(
        Transaction transaction,
        CostBasisMethod method = CostBasisMethod.AverageCost,
        IReadOnlyList<SpecificLotAllocation>? allocation = null,
        Investments? investments = null)
    {
        EnsureNoUncoveredSale([.. Transactions, transaction], transaction.Id, _corporateActions);

        if (transaction.Date <= LatestActiveDisposalDate())
        {
            AddTransaction(transaction);
            try
            {
                DisposalRecordRegenerator.RegenerateAsset(this, method, transaction.Date, transaction.Id, allocation, investments);
            }
            catch
            {
                RemoveTransaction(transaction.Id);
                throw;
            }
            return;
        }

        var effect = TransactionTypeEffects.For(transaction.Type);
        var isDisposing = effect.Quantity == QuantityEffect.Decrease && effect.Cash != CashEffect.None;
        if (isDisposing)
        {
            var precedingCorporateActions = _corporateActions.Where(ca => ca.EffectiveDate <= transaction.Date);
            var disposalRecord = DisposalRecordCalculator.Calculate(
                transaction, Transactions, method, transaction.Currency.ToString(), allocation, precedingCorporateActions);
            _disposalRecords.Add(disposalRecord);
            if (investments is not null)
            {
                AppendTaxClassification(TaxClassificationCalculator.CalculateForDisposal(disposalRecord, investments));
            }
        }

        AddTransaction(transaction);

        if (isDisposing)
        {
            RefreshRealizedCapitalGain();
        }
    }

    private DateTime? LatestActiveDisposalDate()
    {
        var activeDates = _disposalRecords.Where(r => r.Status == DisposalRecordStatus.Active).Select(r => r.Date).ToList();
        return activeDates.Count == 0 ? null : activeDates.Max();
    }

    internal void AppendBackfilledDisposalRecord(DisposalRecord record)
    {
        _disposalRecords.Add(record);
        RefreshRealizedCapitalGain();
    }

    internal void AppendTaxClassification(TaxClassification classification) => _taxClassifications.Add(classification);

    internal TaxClassification? FindTaxClassificationBySource(SourceType sourceType, Guid sourceId) =>
        _taxClassifications.FirstOrDefault(c => c.SourceType == sourceType && c.SourceId == sourceId && c.Status == TaxClassificationStatus.Active);

    internal void SupersedeTaxClassificationBySource(SourceType sourceType, Guid sourceId, Guid? supersededByClassificationId) =>
        FindTaxClassificationBySource(sourceType, sourceId)?.Supersede(supersededByClassificationId);

    internal bool RemoveTaxClassificationBySource(SourceType sourceType, Guid sourceId)
    {
        var classification = FindTaxClassificationBySource(sourceType, sourceId);
        if (classification is null)
        {
            return false;
        }

        return _taxClassifications.Remove(classification);
    }

    internal void RefreshRealizedCapitalGain() =>
        Transactions.SetRealizedCapitalGain(_disposalRecords.Where(r => r.Status == DisposalRecordStatus.Active).Sum(r => r.GainLoss));

    public bool ReviseTransaction(Transaction updatedTransaction, CostBasisMethod method = CostBasisMethod.AverageCost, Investments? investments = null)
    {
        var previous = Transactions.FirstOrDefault(t => t.Id == updatedTransaction.Id);
        if (previous is null)
        {
            return false;
        }

        var candidate = Transactions.Select(t => t.Id == updatedTransaction.Id ? updatedTransaction : t);
        EnsureNoUncoveredSale(candidate, updatedTransaction.Id, _corporateActions);

        if (!UpdateTransaction(updatedTransaction))
        {
            return false;
        }

        var anchor = previous.Date <= updatedTransaction.Date ? previous.Date : updatedTransaction.Date;
        if (anchor <= LatestActiveDisposalDate())
        {
            try
            {
                DisposalRecordRegenerator.RegenerateAsset(this, method, anchor, investments: investments);
            }
            catch
            {
                UpdateTransaction(previous);
                throw;
            }
        }

        return true;
    }

    public bool RetractTransaction(Guid transactionId, CostBasisMethod method = CostBasisMethod.AverageCost, Investments? investments = null)
    {
        var removed = Transactions.FirstOrDefault(t => t.Id == transactionId);
        if (removed is null)
        {
            return false;
        }

        var candidate = Transactions.Where(t => t.Id != transactionId);
        EnsureNoUncoveredSale(candidate, subjectTransactionId: null, _corporateActions);

        if (!RemoveTransaction(transactionId))
        {
            return false;
        }

        if (removed.Date <= LatestActiveDisposalDate())
        {
            try
            {
                DisposalRecordRegenerator.RegenerateAsset(this, method, removed.Date, investments: investments);
            }
            catch
            {
                AddTransaction(removed);
                throw;
            }
        }

        return true;
    }

    private static void EnsureNoUncoveredSale(IEnumerable<Transaction> candidate, Guid? subjectTransactionId, IEnumerable<CorporateAction> corporateActions)
    {
        var violation = SaleCoverageRule.FindFirstUncoveredSale(candidate, corporateActions);
        if (violation is null)
        {
            return;
        }

        var heldQuantity = violation.QuantityHeld.ToString(CultureInfo.InvariantCulture);
        var message = violation.OffendingSale.Id == subjectTransactionId
            ? $"Cannot sell {violation.OffendingSale.Quantity.ToString(CultureInfo.InvariantCulture)} units on {violation.OffendingSale.Date:yyyy-MM-dd} — only {heldQuantity} were held on that date."
            : $"This change would leave the sale of {violation.OffendingSale.Quantity.ToString(CultureInfo.InvariantCulture)} units on {violation.OffendingSale.Date:yyyy-MM-dd} short by {violation.Shortfall.ToString(CultureInfo.InvariantCulture)} units — only {heldQuantity} would be held on that date.";

        throw new InvestmentRuleViolationException(message);
    }

    public void RecordCorporateAction(CorporateAction corporateAction, CostBasisMethod method = CostBasisMethod.AverageCost, Investments? investments = null)
    {
        if (corporateAction == null)
        {
            throw new ArgumentNullException(nameof(corporateAction));
        }

        EnsureNonZeroPositionAt(corporateAction, _corporateActions);

        _corporateActions.Add(corporateAction);
        SyncCorporateActionsWithTransactions();

        try
        {
            DisposalRecordRegenerator.RegenerateAsset(this, method, corporateAction.EffectiveDate, investments: investments);
        }
        catch
        {
            _corporateActions.Remove(corporateAction);
            SyncCorporateActionsWithTransactions();
            throw;
        }
    }

    public bool ReviseCorporateAction(CorporateAction updatedCorporateAction, CostBasisMethod method = CostBasisMethod.AverageCost, Investments? investments = null)
    {
        if (updatedCorporateAction == null)
        {
            throw new ArgumentNullException(nameof(updatedCorporateAction));
        }

        var index = _corporateActions.FindIndex(ca => ca.Id == updatedCorporateAction.Id);
        if (index < 0)
        {
            return false;
        }

        var previous = _corporateActions[index];
        var otherCorporateActions = _corporateActions.Where(ca => ca.Id != updatedCorporateAction.Id);
        EnsureNonZeroPositionAt(updatedCorporateAction, otherCorporateActions);

        _corporateActions[index] = updatedCorporateAction;
        SyncCorporateActionsWithTransactions();

        var anchor = previous.EffectiveDate <= updatedCorporateAction.EffectiveDate ? previous.EffectiveDate : updatedCorporateAction.EffectiveDate;

        try
        {
            DisposalRecordRegenerator.RegenerateAsset(this, method, anchor, investments: investments);
        }
        catch
        {
            _corporateActions[index] = previous;
            SyncCorporateActionsWithTransactions();
            throw;
        }

        return true;
    }

    public bool RetractCorporateAction(Guid corporateActionId, CostBasisMethod method = CostBasisMethod.AverageCost, Investments? investments = null)
    {
        var index = _corporateActions.FindIndex(ca => ca.Id == corporateActionId);
        if (index < 0)
        {
            return false;
        }

        var removed = _corporateActions[index];
        _corporateActions.RemoveAt(index);
        SyncCorporateActionsWithTransactions();

        try
        {
            DisposalRecordRegenerator.RegenerateAsset(this, method, removed.EffectiveDate, investments: investments);
        }
        catch (Exception ex)
        {
            _corporateActions.Insert(index, removed);
            SyncCorporateActionsWithTransactions();

            if (ex is InvestmentRuleViolationException)
            {
                throw new InvestmentRuleViolationException("Cannot delete: a later disposal depends on lots created by this split.");
            }

            throw;
        }

        return true;
    }

    private void EnsureNonZeroPositionAt(CorporateAction corporateAction, IEnumerable<CorporateAction> otherCorporateActions)
    {
        var quantity = 0m;
        var candidateActions = otherCorporateActions.Append(corporateAction);

        foreach (var step in CorporateActionReplay.Merge(Transactions, candidateActions))
        {
            if (step is CorporateActionReplayStep(var stepAction) && ReferenceEquals(stepAction, corporateAction))
            {
                break;
            }

            quantity = step switch
            {
                TransactionReplayStep(var transaction) => CorporateActionReplay.ApplyTransactionToQuantity(quantity, transaction),
                CorporateActionReplayStep(var priorAction) => CorporateActionReplay.RescalePosition(quantity, 0m, priorAction.RatioFactor!.Value).Quantity,
                _ => quantity
            };
        }

        // Also covers a split dated before the holding's first transaction - replaying up to
        // that date yields the same zero quantity.
        if (quantity == 0)
        {
            throw new InvestmentRuleViolationException("This holding has no open position to split.");
        }
    }

    public void AddCredit(Credit credit, Investments? investments = null)
    {
        if (credit == null)
        {
            throw new ArgumentNullException(nameof(credit));
        }

        _credits.Add(credit);

        if (investments is not null)
        {
            AppendTaxClassification(TaxClassificationCalculator.CalculateForCredit(credit, investments));
        }
    }

    public bool UpdateCredit(Credit updatedCredit, Investments? investments = null)
    {
        if (updatedCredit == null)
        {
            throw new ArgumentNullException(nameof(updatedCredit));
        }

        EntityGuard.EnsureNotEmptyId(updatedCredit.Id, "Credit Id is required for update.", nameof(updatedCredit));

        var index = _credits.FindIndex(credit => credit.Id == updatedCredit.Id);
        if (index < 0)
        {
            return false;
        }

        _credits[index] = updatedCredit;

        RemoveTaxClassificationBySource(SourceType.Credit, updatedCredit.Id);
        if (investments is not null)
        {
            AppendTaxClassification(TaxClassificationCalculator.CalculateForCredit(updatedCredit, investments));
        }

        return true;
    }

    public bool RemoveCredit(Guid creditId)
    {
        EntityGuard.EnsureNotEmptyId(creditId, "Credit Id is required for delete.", nameof(creditId));

        var index = _credits.FindIndex(credit => credit.Id == creditId);
        if (index < 0)
        {
            return false;
        }

        _credits.RemoveAt(index);
        RemoveTaxClassificationBySource(SourceType.Credit, creditId);
        return true;
    }
    public void AddCredits(IEnumerable<Credit> credits)
    {
        foreach (var credit in credits)
        {
            AddCredit(credit);
        }
    }

    public void SetValuationMethod(ValuationMethod valuationMethod) => ValuationMethod = valuationMethod;

    public void SetIncomePolicy(IncomePolicy incomePolicy) => IncomePolicy = incomePolicy;

    /// <summary>
    /// Simple path: maps <paramref name="isManual"/> onto <see cref="PriceSource.Manual"/>/
    /// <see cref="PriceSource.Unknown"/>, and defaults the rest of a snapshot's provenance (no
    /// currency known, retrieved now, no source reference). This is the overload every existing
    /// caller (mostly test fixtures with no interest in provenance) keeps using unchanged; a caller
    /// that actually knows a snapshot's full provenance uses the overload below instead.
    /// </summary>
    public void SetPrice(DateOnly date, decimal price, bool isManual) =>
        SetPrice(date, price, isManual ? PriceSource.Manual : PriceSource.Unknown, currency: string.Empty, sourceReference: null, DateTimeOffset.UtcNow);

    public void SetPrice(DateOnly date, decimal price, PriceSource source, string currency, string? sourceReference, DateTimeOffset retrievedAt)
    {
        var entry = AssetPriceSnapshot.Create(date, price, ValuationMethod, source, currency, sourceReference, retrievedAt);
        UpsertPriceEntry(entry);
    }

    public AssetPriceSnapshot? GetPriceForDate(DateOnly date) =>
        _priceSnapshots.FirstOrDefault(entry => entry.Date == date);

    public AssetPriceSnapshot? GetMostRecentPrice() =>
        _priceSnapshots.OrderByDescending(entry => entry.Date).FirstOrDefault();

    public AssetPriceSnapshot? GetPriceAsOf(DateOnly date) =>
        _priceSnapshots.Where(entry => entry.Date <= date).MaxBy(entry => entry.Date);

    public bool RemovePrice(DateOnly date)
    {
        var current = _priceSnapshots;
        var index = current.FindIndex(entry => entry.Date == date);
        if (index < 0 || !current[index].IsManual)
        {
            return false;
        }

        var updated = new List<AssetPriceSnapshot>(current);
        updated.RemoveAt(index);
        _priceSnapshots = updated;
        return true;
    }

    /// <summary>
    /// Puts <paramref name="date"/> back to <paramref name="previous"/>, removing the entry
    /// entirely when there was none. This is the compensating half of <see cref="SetPrice"/>, for
    /// the case where the write could not be persisted: leaving the entry behind is what let a
    /// lost write read back as a recorded one.
    /// <para>
    /// Unlike <see cref="RemovePrice"/> this does not require the entry to be manual. That rule
    /// exists to stop the delete path discarding a hand-entered price, and it still does - the
    /// entry being undone here is the automatic one this same operation just wrote, and the caller
    /// supplies what it displaced rather than choosing a row to drop.
    /// </para>
    /// </summary>
    public void RestorePrice(DateOnly date, AssetPriceSnapshot? previous)
    {
        if (previous is not null)
        {
            UpsertPriceEntry(previous);
            return;
        }

        var current = _priceSnapshots;
        var index = current.FindIndex(entry => entry.Date == date);
        if (index < 0)
        {
            return;
        }

        var updated = new List<AssetPriceSnapshot>(current);
        updated.RemoveAt(index);
        _priceSnapshots = updated;
    }

    /// <summary>
    /// Price history is replaced wholesale rather than edited in place. Recording a fetched price
    /// and reading the history happen concurrently — a portfolio grid prices every row at once
    /// while the asset page loads details for one of them — and an in-place Add or index
    /// assignment breaks an enumeration that is already running, which is what produced
    /// "Collection was modified" during a save. A published list is never touched again, so a
    /// reader keeps a stable view for as long as it needs one, and AssetPriceSnapshot is
    /// immutable so the entries can be shared between the old list and the new one.
    /// </summary>
    private void UpsertPriceEntry(AssetPriceSnapshot entry)
    {
        var updated = new List<AssetPriceSnapshot>(_priceSnapshots);
        UpsertInto(updated, entry);
        _priceSnapshots = updated;
    }

    private static void UpsertInto(List<AssetPriceSnapshot> entries, AssetPriceSnapshot entry)
    {
        var index = entries.FindIndex(existing => existing.Date == entry.Date);
        if (index >= 0)
        {
            entries[index] = entry;
        }
        else
        {
            entries.Add(entry);
        }
    }

    private static string NormalizeLocalTypeCode(string localTypeCode)
    {
        return string.IsNullOrWhiteSpace(localTypeCode) ? string.Empty : localTypeCode.Trim();
    }
}
