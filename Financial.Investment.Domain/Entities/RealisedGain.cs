using System;
using System.Collections.Generic;
using System.Linq;
using Financial.Shared.Abstractions.Currencies;

namespace Financial.Investment.Domain.Entities;

public sealed class RealisedGain
{
    public Guid Id { get; private set; }
    public Guid TransactionId { get; private set; }
    public DateTime Date { get; private set; }
    public CostBasisMethod Method { get; private set; }

    private List<RealisedGainLot> _lotsConsumed = new();
    public IReadOnlyList<RealisedGainLot> LotsConsumed
    {
        get => _lotsConsumed.AsReadOnly();
        private set => _lotsConsumed = new List<RealisedGainLot>(value);
    }

    public decimal QuantitySold { get; private set; }
    public decimal Proceeds { get; private set; }

    public decimal CostBasis => _lotsConsumed.Sum(lot => lot.Quantity * lot.UnitCost);

    public decimal GainLoss => Proceeds - CostBasis;

    public Currency Currency { get; private set; }
    public string TaxYear { get; private set; } = string.Empty;
    public RealisedGainStatus Status { get; private set; } = RealisedGainStatus.Active;
    public Guid? SupersededByRecordId { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    private RealisedGain() { }

    private RealisedGain(
        Guid id,
        Guid transactionId,
        DateTime date,
        CostBasisMethod method,
        IReadOnlyList<RealisedGainLot> lotsConsumed,
        decimal quantitySold,
        decimal proceeds,
        Currency currency,
        string taxYear,
        DateTimeOffset createdAt)
        : this()
    {
        if (lotsConsumed is null || lotsConsumed.Count == 0)
        {
            throw new ArgumentException("A realised gain requires at least one consumed lot.", nameof(lotsConsumed));
        }

        Id = id;
        TransactionId = transactionId;
        Date = date;
        Method = method;
        LotsConsumed = lotsConsumed;
        QuantitySold = quantitySold;
        Proceeds = proceeds;
        Currency = currency;
        TaxYear = taxYear;
        CreatedAt = createdAt;
    }

    public static RealisedGain Create(
        Guid transactionId,
        DateTime date,
        CostBasisMethod method,
        IReadOnlyList<RealisedGainLot> lotsConsumed,
        decimal quantitySold,
        decimal proceeds,
        Currency currency,
        string taxYear,
        DateTimeOffset createdAt) =>
        new(Guid.NewGuid(), transactionId, date, method, lotsConsumed, quantitySold, proceeds, currency, taxYear, createdAt);

    public void Supersede(Guid? supersededByRecordId)
    {
        if (Status == RealisedGainStatus.Superseded)
        {
            throw new InvalidOperationException("This realised gain has already been superseded.");
        }

        Status = RealisedGainStatus.Superseded;
        SupersededByRecordId = supersededByRecordId;
    }

    public static RealisedGain CreateWithId(
        Guid id,
        Guid transactionId,
        DateTime date,
        CostBasisMethod method,
        IReadOnlyList<RealisedGainLot> lotsConsumed,
        decimal quantitySold,
        decimal proceeds,
        Currency currency,
        string taxYear,
        DateTimeOffset createdAt) =>
        new(id, transactionId, date, method, lotsConsumed, quantitySold, proceeds, currency, taxYear, createdAt);
}
