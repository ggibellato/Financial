using System;
using Financial.Investment.Domain.Entities;
using Financial.Shared.Abstractions.Currencies;

namespace Financial.Investment.Domain.Rules;

public static class TaxClassificationCalculator
{
    public static TaxClassification CalculateForDisposal(DisposalRecord record, Investments investments)
    {
        var jurisdiction = ForCurrency(record.Currency);
        var rule = investments.FindApplicableTaxRule(jurisdiction, EventCategory.CapitalGain, DateOnly.FromDateTime(record.Date));
        var status = rule is not null ? CalculationStatus.Final : CalculationStatus.Incomplete;

        return TaxClassification.CreateForDisposal(
            record.Id, jurisdiction, record.TaxYear, record.Proceeds, record.CostBasis, record.GainLoss, status, rule?.Id);
    }

    public static TaxClassification CalculateForCredit(Credit credit, Investments investments)
    {
        var jurisdiction = ForCurrency(credit.Currency);
        var taxYear = TaxYearCalculator.Calculate(credit.Date, credit.Currency.ToString());
        var category = ForCreditType(credit.Type);

        if (category is null)
        {
            return TaxClassification.CreateForCredit(
                credit.Id, jurisdiction, taxYear, EventCategory.Unrecognized,
                credit.Value, credit.Withheld, credit.NetAmount, CalculationStatus.RequiresReview, null);
        }

        var rule = investments.FindApplicableTaxRule(jurisdiction, category.Value, DateOnly.FromDateTime(credit.Date));
        var status = rule is not null ? CalculationStatus.Final : CalculationStatus.Incomplete;

        return TaxClassification.CreateForCredit(
            credit.Id, jurisdiction, taxYear, category.Value,
            credit.Value, credit.Withheld, credit.NetAmount, status, rule?.Id);
    }

    public static TaxClassification CalculateForCorporateAction(CorporateAction record, Currency currency, Investments investments)
    {
        var isMergerTarget = record.Type == CorporateAction.CorporateActionType.Merger && record.Role == CorporateAction.CorporateActionRole.Target;
        var isSpinOffNew = record.Type == CorporateAction.CorporateActionType.SpinOff && record.Role == CorporateAction.CorporateActionRole.New;

        if (!isMergerTarget && !isSpinOffNew)
        {
            throw new InvalidOperationException(
                $"CalculateForCorporateAction requires a {CorporateAction.CorporateActionType.Merger}/{CorporateAction.CorporateActionRole.Target} " +
                $"or {CorporateAction.CorporateActionType.SpinOff}/{CorporateAction.CorporateActionRole.New} record; " +
                $"corporate action {record.Id} is {record.Type}/{record.Role?.ToString() ?? "none"}.");
        }

        var jurisdiction = ForCurrency(currency);
        var taxYear = TaxYearCalculator.Calculate(record.EffectiveDate, currency.ToString());
        var rule = investments.FindApplicableTaxRule(jurisdiction, EventCategory.CorporateAction, DateOnly.FromDateTime(record.EffectiveDate));
        var status = rule is not null ? CalculationStatus.Final : CalculationStatus.RequiresReview;

        return TaxClassification.CreateForCorporateAction(
            record.Id, jurisdiction, taxYear, record.CarriedCostBasis!.Value, status, rule?.Id);
    }

    private static Jurisdiction ForCurrency(Currency currency) =>
        currency == Currency.BRL ? Jurisdiction.BR : Jurisdiction.UK;

    private static EventCategory? ForCreditType(Credit.CreditType type) => type switch
    {
        Credit.CreditType.Dividend => EventCategory.Dividend,
        Credit.CreditType.Coupon => EventCategory.Interest,
        Credit.CreditType.JCP => EventCategory.Interest,
        Credit.CreditType.SecuritiesLendingIncome => EventCategory.SecuritiesLendingIncome,
        _ => null
    };
}
