using System;
using System.Collections.Generic;
using System.Linq;
using Financial.Shared.Abstractions.Currencies;

namespace Financial.Investment.Domain.Entities;

/// <summary>
/// An immutable, auditable record of what a Sell/Redemption actually disposed of, at what basis,
/// computed under the asset's broker's configured CostBasisMethod at the moment it was created.
/// Nothing here mutates after construction: a later correction or method change (a future feature)
/// marks this record Superseded and links to its replacement rather than rewriting it in place.
/// </summary>
public sealed class DisposalRecord
{
    public Guid Id { get; private set; }
    public Guid TransactionId { get; private set; }
    public DateTime Date { get; private set; }
    public CostBasisMethod Method { get; private set; }

    private List<DisposalLotConsumption> _lotsConsumed = new();
    public IReadOnlyList<DisposalLotConsumption> LotsConsumed { get => _lotsConsumed.AsReadOnly(); private set => _lotsConsumed = new List<DisposalLotConsumption>(value); }

    public decimal QuantityDisposed { get; private set; }
    public decimal Proceeds { get; private set; }

    /// <summary>Sum of each consumed lot's quantity x unit cost — never stored separately from
    /// LotsConsumed, so the two can never drift apart.</summary>
    public decimal CostBasis => _lotsConsumed.Sum(lot => lot.Quantity * lot.UnitCost);

    public decimal GainLoss => Proceeds - CostBasis;

    public Currency Currency { get; private set; }
    public string TaxYear { get; private set; } = string.Empty;
    public DisposalRecordStatus Status { get; private set; } = DisposalRecordStatus.Active;
    public Guid? SupersededByRecordId { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    private DisposalRecord() { }

    private DisposalRecord(
        Guid id,
        Guid transactionId,
        DateTime date,
        CostBasisMethod method,
        IReadOnlyList<DisposalLotConsumption> lotsConsumed,
        decimal quantityDisposed,
        decimal proceeds,
        Currency currency,
        string taxYear,
        DateTimeOffset createdAt)
        : this()
    {
        if (lotsConsumed is null || lotsConsumed.Count == 0)
        {
            throw new ArgumentException("A disposal record requires at least one consumed lot.", nameof(lotsConsumed));
        }

        Id = id;
        TransactionId = transactionId;
        Date = date;
        Method = method;
        LotsConsumed = lotsConsumed;
        QuantityDisposed = quantityDisposed;
        Proceeds = proceeds;
        Currency = currency;
        TaxYear = taxYear;
        CreatedAt = createdAt;
    }

    public static DisposalRecord Create(
        Guid transactionId,
        DateTime date,
        CostBasisMethod method,
        IReadOnlyList<DisposalLotConsumption> lotsConsumed,
        decimal quantityDisposed,
        decimal proceeds,
        Currency currency,
        string taxYear) =>
        new(Guid.NewGuid(), transactionId, date, method, lotsConsumed, quantityDisposed, proceeds, currency, taxYear, DateTimeOffset.UtcNow);

    public static DisposalRecord CreateWithId(
        Guid id,
        Guid transactionId,
        DateTime date,
        CostBasisMethod method,
        IReadOnlyList<DisposalLotConsumption> lotsConsumed,
        decimal quantityDisposed,
        decimal proceeds,
        Currency currency,
        string taxYear,
        DateTimeOffset createdAt) =>
        new(id, transactionId, date, method, lotsConsumed, quantityDisposed, proceeds, currency, taxYear, createdAt);
}
