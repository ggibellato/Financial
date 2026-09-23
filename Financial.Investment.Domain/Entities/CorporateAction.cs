using System;

namespace Financial.Investment.Domain.Entities;

public class CorporateAction
{
    public enum CorporateActionType { Split, Merger }

    public enum MergerRole { Source, Target }

    public const int MaxNoteLength = 500;

    public Guid Id { get; private set; }
    public CorporateActionType Type { get; private set; }
    public DateTime EffectiveDate { get; private set; }
    public decimal? RatioFactor { get; private set; }
    public string? Note { get; private set; }
    public MergerRole? Role { get; private set; }
    public Guid? CorrelationId { get; private set; }
    public string? LinkedAssetName { get; private set; }
    public decimal? ExchangeRatio { get; private set; }
    public decimal? CashInLieu { get; private set; }
    public decimal? ConvertedQuantity { get; private set; }
    public decimal? CarriedCostBasis { get; private set; }

    private CorporateAction() { }

    private CorporateAction(
        Guid id,
        CorporateActionType type,
        DateTime effectiveDate,
        decimal? ratioFactor,
        string? note,
        MergerRole? role,
        Guid? correlationId,
        string? linkedAssetName,
        decimal? exchangeRatio,
        decimal? cashInLieu,
        decimal? convertedQuantity,
        decimal? carriedCostBasis)
    {
        ValidateNote(note);

        Id = id == Guid.Empty ? Guid.NewGuid() : id;
        Type = type;
        EffectiveDate = effectiveDate;
        RatioFactor = ratioFactor;
        Note = note;
        Role = role;
        CorrelationId = correlationId;
        LinkedAssetName = linkedAssetName;
        ExchangeRatio = exchangeRatio;
        CashInLieu = cashInLieu;
        ConvertedQuantity = convertedQuantity;
        CarriedCostBasis = carriedCostBasis;
    }

    public static CorporateAction CreateSplit(DateTime effectiveDate, decimal ratioFactor, string? note = null) =>
        CreateSplitWithId(Guid.NewGuid(), effectiveDate, ratioFactor, note);

    public static CorporateAction CreateSplitWithId(Guid id, DateTime effectiveDate, decimal ratioFactor, string? note = null)
    {
        ValidateRatioFactor(ratioFactor);

        return new(
            id, CorporateActionType.Split, effectiveDate, ratioFactor: ratioFactor, note: note,
            role: null, correlationId: null, linkedAssetName: null, exchangeRatio: null, cashInLieu: null,
            convertedQuantity: null, carriedCostBasis: null);
    }

    public static CorporateAction CreateMergerSource(
        DateTime effectiveDate,
        decimal exchangeRatio,
        decimal? cashInLieu,
        string? note,
        Guid correlationId,
        string linkedAssetName,
        decimal convertedQuantity,
        decimal carriedCostBasis) =>
        CreateMergerSourceWithId(Guid.NewGuid(), effectiveDate, exchangeRatio, cashInLieu, note, correlationId, linkedAssetName, convertedQuantity, carriedCostBasis);

    public static CorporateAction CreateMergerSourceWithId(
        Guid id,
        DateTime effectiveDate,
        decimal exchangeRatio,
        decimal? cashInLieu,
        string? note,
        Guid correlationId,
        string linkedAssetName,
        decimal convertedQuantity,
        decimal carriedCostBasis)
    {
        ValidateExchangeRatio(exchangeRatio);
        ValidateCashInLieu(cashInLieu);

        return new(
            id, CorporateActionType.Merger, effectiveDate, ratioFactor: null, note: note,
            role: MergerRole.Source, correlationId: correlationId, linkedAssetName: linkedAssetName,
            exchangeRatio: exchangeRatio, cashInLieu: cashInLieu, convertedQuantity: convertedQuantity, carriedCostBasis: carriedCostBasis);
    }

    public static CorporateAction CreateMergerTarget(
        DateTime effectiveDate,
        string? note,
        Guid correlationId,
        string linkedAssetName,
        decimal convertedQuantity,
        decimal carriedCostBasis) =>
        CreateMergerTargetWithId(Guid.NewGuid(), effectiveDate, note, correlationId, linkedAssetName, convertedQuantity, carriedCostBasis);

    public static CorporateAction CreateMergerTargetWithId(
        Guid id,
        DateTime effectiveDate,
        string? note,
        Guid correlationId,
        string linkedAssetName,
        decimal convertedQuantity,
        decimal carriedCostBasis) =>
        new(
            id, CorporateActionType.Merger, effectiveDate, ratioFactor: null, note: note,
            role: MergerRole.Target, correlationId: correlationId, linkedAssetName: linkedAssetName,
            exchangeRatio: null, cashInLieu: null, convertedQuantity: convertedQuantity, carriedCostBasis: carriedCostBasis);

    private static void ValidateRatioFactor(decimal ratioFactor)
    {
        if (ratioFactor <= 0 || ratioFactor == 1.0m)
        {
            throw new ArgumentException("Ratio factor must be greater than zero and not equal to 1.0.");
        }
    }

    private static void ValidateExchangeRatio(decimal exchangeRatio)
    {
        if (exchangeRatio <= 0)
        {
            throw new ArgumentException("Exchange ratio must be greater than zero.");
        }
    }

    private static void ValidateCashInLieu(decimal? cashInLieu)
    {
        if (cashInLieu is < 0)
        {
            throw new ArgumentException("Cash in lieu must be zero or greater.");
        }
    }

    private static void ValidateNote(string? note)
    {
        if (note is { Length: > MaxNoteLength })
        {
            throw new ArgumentException($"Note must be {MaxNoteLength} characters or fewer.");
        }
    }
}
