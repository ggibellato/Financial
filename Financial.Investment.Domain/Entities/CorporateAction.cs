using System;

namespace Financial.Investment.Domain.Entities;

public class CorporateAction
{
    public enum CorporateActionType { Split, Merger, SpinOff }

    public enum CorporateActionRole { Source, Target, Parent, New }

    public const int MaxNoteLength = 500;

    public Guid Id { get; private set; }
    public CorporateActionType Type { get; private set; }
    public DateTime EffectiveDate { get; private set; }
    public decimal? RatioFactor { get; private set; }
    public string? Note { get; private set; }
    public CorporateActionRole? Role { get; private set; }
    public Guid? CorrelationId { get; private set; }
    public string? LinkedAssetName { get; private set; }
    public decimal? ExchangeRatio { get; private set; }
    public decimal? CashInLieu { get; private set; }
    public decimal? ConvertedQuantity { get; private set; }
    public decimal? CarriedCostBasis { get; private set; }
    public decimal? AllocationPercentage { get; private set; }

    public bool IsReceivingRole =>
        (Type == CorporateActionType.Merger && Role == CorporateActionRole.Target)
        || (Type == CorporateActionType.SpinOff && Role == CorporateActionRole.New);

    private CorporateAction() { }

    private CorporateAction(
        Guid id,
        CorporateActionType type,
        DateTime effectiveDate,
        decimal? ratioFactor,
        string? note,
        CorporateActionRole? role,
        Guid? correlationId,
        string? linkedAssetName,
        decimal? exchangeRatio,
        decimal? cashInLieu,
        decimal? convertedQuantity,
        decimal? carriedCostBasis,
        decimal? allocationPercentage)
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
        AllocationPercentage = allocationPercentage;
    }

    public static CorporateAction CreateSplit(DateTime effectiveDate, decimal ratioFactor, string? note = null) =>
        CreateSplitWithId(Guid.NewGuid(), effectiveDate, ratioFactor, note);

    public static CorporateAction CreateSplitWithId(Guid id, DateTime effectiveDate, decimal ratioFactor, string? note = null)
    {
        ValidateRatioFactor(ratioFactor);

        return new(
            id, CorporateActionType.Split, effectiveDate, ratioFactor: ratioFactor, note: note,
            role: null, correlationId: null, linkedAssetName: null, exchangeRatio: null, cashInLieu: null,
            convertedQuantity: null, carriedCostBasis: null, allocationPercentage: null);
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
            role: CorporateActionRole.Source, correlationId: correlationId, linkedAssetName: linkedAssetName,
            exchangeRatio: exchangeRatio, cashInLieu: cashInLieu, convertedQuantity: convertedQuantity, carriedCostBasis: carriedCostBasis,
            allocationPercentage: null);
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
            role: CorporateActionRole.Target, correlationId: correlationId, linkedAssetName: linkedAssetName,
            exchangeRatio: null, cashInLieu: null, convertedQuantity: convertedQuantity, carriedCostBasis: carriedCostBasis,
            allocationPercentage: null);

    public static CorporateAction CreateSpinOffParent(
        DateTime effectiveDate,
        decimal allocationPercentage,
        string? note,
        Guid correlationId,
        string linkedAssetName,
        decimal quantityReceived,
        decimal carriedCostBasis) =>
        CreateSpinOffParentWithId(Guid.NewGuid(), effectiveDate, allocationPercentage, note, correlationId, linkedAssetName, quantityReceived, carriedCostBasis);

    public static CorporateAction CreateSpinOffParentWithId(
        Guid id,
        DateTime effectiveDate,
        decimal allocationPercentage,
        string? note,
        Guid correlationId,
        string linkedAssetName,
        decimal quantityReceived,
        decimal carriedCostBasis)
    {
        ValidateAllocationPercentage(allocationPercentage);
        ValidateQuantityReceived(quantityReceived);

        return new(
            id, CorporateActionType.SpinOff, effectiveDate, ratioFactor: null, note: note,
            role: CorporateActionRole.Parent, correlationId: correlationId, linkedAssetName: linkedAssetName,
            exchangeRatio: null, cashInLieu: null, convertedQuantity: quantityReceived, carriedCostBasis: carriedCostBasis,
            allocationPercentage: allocationPercentage);
    }

    public static CorporateAction CreateSpinOffNew(
        DateTime effectiveDate,
        string? note,
        Guid correlationId,
        string linkedAssetName,
        decimal quantityReceived,
        decimal carriedCostBasis) =>
        CreateSpinOffNewWithId(Guid.NewGuid(), effectiveDate, note, correlationId, linkedAssetName, quantityReceived, carriedCostBasis);

    public static CorporateAction CreateSpinOffNewWithId(
        Guid id,
        DateTime effectiveDate,
        string? note,
        Guid correlationId,
        string linkedAssetName,
        decimal quantityReceived,
        decimal carriedCostBasis) =>
        new(
            id, CorporateActionType.SpinOff, effectiveDate, ratioFactor: null, note: note,
            role: CorporateActionRole.New, correlationId: correlationId, linkedAssetName: linkedAssetName,
            exchangeRatio: null, cashInLieu: null, convertedQuantity: quantityReceived, carriedCostBasis: carriedCostBasis,
            allocationPercentage: null);

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

    private static void ValidateAllocationPercentage(decimal allocationPercentage)
    {
        if (allocationPercentage is < 0 or > 100)
        {
            throw new ArgumentException("Allocation percentage must be between 0 and 100 inclusive.");
        }
    }

    private static void ValidateQuantityReceived(decimal quantityReceived)
    {
        if (quantityReceived <= 0)
        {
            throw new ArgumentException("Quantity received must be greater than zero.");
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
