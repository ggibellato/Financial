using System;

namespace Financial.Investment.Domain.Entities;

public class CorporateAction
{
    public enum CorporateActionType { Split }

    public const int MaxNoteLength = 500;

    public Guid Id { get; private set; }
    public CorporateActionType Type { get; private set; }
    public DateTime EffectiveDate { get; private set; }
    public decimal RatioFactor { get; private set; }
    public string? Note { get; private set; }

    private CorporateAction() { }

    private CorporateAction(Guid id, CorporateActionType type, DateTime effectiveDate, decimal ratioFactor, string? note)
    {
        ValidateRatioFactor(ratioFactor);
        ValidateNote(note);

        Id = id == Guid.Empty ? Guid.NewGuid() : id;
        Type = type;
        EffectiveDate = effectiveDate;
        RatioFactor = ratioFactor;
        Note = note;
    }

    public static CorporateAction CreateSplit(DateTime effectiveDate, decimal ratioFactor, string? note = null) =>
        new(Guid.NewGuid(), CorporateActionType.Split, effectiveDate, ratioFactor, note);

    public static CorporateAction CreateSplitWithId(Guid id, DateTime effectiveDate, decimal ratioFactor, string? note = null) =>
        new(id, CorporateActionType.Split, effectiveDate, ratioFactor, note);

    private static void ValidateRatioFactor(decimal ratioFactor)
    {
        if (ratioFactor <= 0 || ratioFactor == 1.0m)
        {
            throw new ArgumentException("Ratio factor must be greater than zero and not equal to 1.0.");
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
