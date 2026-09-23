using System;

namespace Financial.Investment.Domain.Entities;

public sealed class TaxClassification
{
    public Guid Id { get; private set; }
    public SourceType SourceType { get; private set; }
    public Guid SourceId { get; private set; }
    public Jurisdiction Jurisdiction { get; private set; }
    public string TaxYear { get; private set; } = string.Empty;
    public EventCategory EventCategory { get; private set; }
    public decimal? Proceeds { get; private set; }
    public decimal? CostBasis { get; private set; }
    public decimal? GainLoss { get; private set; }
    public decimal? GrossAmount { get; private set; }
    public decimal? WithheldAmount { get; private set; }
    public decimal? NetAmount { get; private set; }
    public CalculationStatus CalculationStatus { get; private set; }
    public Guid? TaxRuleId { get; private set; }
    public TaxClassificationStatus Status { get; private set; } = TaxClassificationStatus.Active;
    public Guid? SupersededByClassificationId { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    private TaxClassification() { }

    private TaxClassification(
        Guid id,
        SourceType sourceType,
        Guid sourceId,
        Jurisdiction jurisdiction,
        string taxYear,
        EventCategory eventCategory,
        decimal? proceeds,
        decimal? costBasis,
        decimal? gainLoss,
        decimal? grossAmount,
        decimal? withheldAmount,
        decimal? netAmount,
        CalculationStatus calculationStatus,
        Guid? taxRuleId,
        DateTimeOffset createdAt)
        : this()
    {
        Id = id;
        SourceType = sourceType;
        SourceId = sourceId;
        Jurisdiction = jurisdiction;
        TaxYear = taxYear;
        EventCategory = eventCategory;
        Proceeds = proceeds;
        CostBasis = costBasis;
        GainLoss = gainLoss;
        GrossAmount = grossAmount;
        WithheldAmount = withheldAmount;
        NetAmount = netAmount;
        CalculationStatus = calculationStatus;
        TaxRuleId = taxRuleId;
        CreatedAt = createdAt;
    }

    public static TaxClassification CreateForDisposal(
        Guid disposalRecordId,
        Jurisdiction jurisdiction,
        string taxYear,
        decimal proceeds,
        decimal costBasis,
        decimal gainLoss,
        CalculationStatus calculationStatus,
        Guid? taxRuleId) =>
        new(
            Guid.NewGuid(), SourceType.Disposal, disposalRecordId, jurisdiction, taxYear, EventCategory.CapitalGain,
            proceeds, costBasis, gainLoss, null, null, null, calculationStatus, taxRuleId, DateTimeOffset.UtcNow);

    public static TaxClassification CreateForCredit(
        Guid creditId,
        Jurisdiction jurisdiction,
        string taxYear,
        EventCategory eventCategory,
        decimal grossAmount,
        decimal withheldAmount,
        decimal netAmount,
        CalculationStatus calculationStatus,
        Guid? taxRuleId) =>
        new(
            Guid.NewGuid(), SourceType.Credit, creditId, jurisdiction, taxYear, eventCategory,
            null, null, null, grossAmount, withheldAmount, netAmount, calculationStatus, taxRuleId, DateTimeOffset.UtcNow);

    public static TaxClassification CreateForCorporateAction(
        Guid corporateActionId,
        Jurisdiction jurisdiction,
        string taxYear,
        decimal costBasis,
        CalculationStatus calculationStatus,
        Guid? taxRuleId) =>
        new(
            Guid.NewGuid(), SourceType.CorporateAction, corporateActionId, jurisdiction, taxYear, EventCategory.CorporateAction,
            null, costBasis, null, null, null, null, calculationStatus, taxRuleId, DateTimeOffset.UtcNow);

    public void Supersede(Guid? supersededByClassificationId)
    {
        if (Status == TaxClassificationStatus.Superseded)
        {
            throw new InvalidOperationException("This tax classification has already been superseded.");
        }

        Status = TaxClassificationStatus.Superseded;
        SupersededByClassificationId = supersededByClassificationId;
    }
}
