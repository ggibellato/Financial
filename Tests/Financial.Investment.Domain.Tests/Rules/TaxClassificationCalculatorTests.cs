using Financial.Investment.Domain.Entities;
using Financial.Investment.Domain.Rules;
using Financial.Shared.Abstractions.Currencies;
using FluentAssertions;
using FluentAssertions.Execution;

namespace Financial.Investment.Domain.Tests.Rules;

public class TaxClassificationCalculatorTests
{
    private static DisposalRecord CreateDisposalRecord(Currency currency = Currency.BRL, DateTime? date = null) =>
        DisposalRecord.Create(
            Guid.NewGuid(),
            date ?? new DateTime(2026, 6, 1),
            CostBasisMethod.AverageCost,
            [new DisposalLotConsumption(null, 10m, 60m)],
            10m,
            1000m,
            currency,
            "2026");

    [Fact]
    public void CalculateForDisposal_DerivesJurisdictionFromCurrency_Brl()
    {
        var record = CreateDisposalRecord(Currency.BRL);
        var investments = Investments.Create();

        var classification = TaxClassificationCalculator.CalculateForDisposal(record, investments);

        classification.Jurisdiction.Should().Be(Jurisdiction.BR);
    }

    [Theory]
    [InlineData(Currency.GBP)]
    [InlineData(Currency.USD)]
    public void CalculateForDisposal_DerivesJurisdictionFromCurrency_NonBrlIsUk(Currency currency)
    {
        var record = CreateDisposalRecord(currency);
        var investments = Investments.Create();

        var classification = TaxClassificationCalculator.CalculateForDisposal(record, investments);

        classification.Jurisdiction.Should().Be(Jurisdiction.UK);
    }

    [Fact]
    public void CalculateForDisposal_NoApplicableRule_IsIncomplete()
    {
        var record = CreateDisposalRecord();
        var investments = Investments.Create();

        var classification = TaxClassificationCalculator.CalculateForDisposal(record, investments);

        using (new AssertionScope())
        {
            classification.CalculationStatus.Should().Be(CalculationStatus.Incomplete);
            classification.TaxRuleId.Should().BeNull();
        }
    }

    [Fact]
    public void CalculateForDisposal_ApplicableRule_IsFinalAndReferencesTheRule()
    {
        var record = CreateDisposalRecord(date: new DateTime(2026, 6, 1));
        var investments = Investments.Create();
        var rule = investments.CreateTaxRule(
            Jurisdiction.BR, EventCategory.CapitalGain, "BR capital gains", "desc", new DateOnly(2026, 1, 1), null);

        var classification = TaxClassificationCalculator.CalculateForDisposal(record, investments);

        using (new AssertionScope())
        {
            classification.CalculationStatus.Should().Be(CalculationStatus.Final);
            classification.TaxRuleId.Should().Be(rule.Id);
        }
    }

    [Fact]
    public void CalculateForDisposal_CopiesFieldsFromTheRecordUnchanged()
    {
        var record = CreateDisposalRecord();
        var investments = Investments.Create();

        var classification = TaxClassificationCalculator.CalculateForDisposal(record, investments);

        using (new AssertionScope())
        {
            classification.SourceType.Should().Be(SourceType.Disposal);
            classification.SourceId.Should().Be(record.Id);
            classification.TaxYear.Should().Be(record.TaxYear);
            classification.Proceeds.Should().Be(record.Proceeds);
            classification.CostBasis.Should().Be(record.CostBasis);
            classification.GainLoss.Should().Be(record.GainLoss);
            classification.EventCategory.Should().Be(EventCategory.CapitalGain);
        }
    }

    [Theory]
    [InlineData(Credit.CreditType.Dividend, EventCategory.Dividend)]
    [InlineData(Credit.CreditType.Coupon, EventCategory.Interest)]
    [InlineData(Credit.CreditType.JCP, EventCategory.Interest)]
    [InlineData(Credit.CreditType.SecuritiesLendingIncome, EventCategory.SecuritiesLendingIncome)]
    public void CalculateForCredit_MapsEveryKnownCreditTypeToItsCategory(Credit.CreditType type, EventCategory expected)
    {
        var credit = Credit.Create(new DateTime(2026, 6, 1), type, 100m, 10m, Currency.BRL);
        var investments = Investments.Create();

        var classification = TaxClassificationCalculator.CalculateForCredit(credit, investments);

        classification.EventCategory.Should().Be(expected);
    }

    [Fact]
    public void CalculateForCredit_CopiesAmountsFromTheCredit()
    {
        var credit = Credit.Create(new DateTime(2026, 6, 1), Credit.CreditType.Dividend, 100m, 10m, Currency.BRL);
        var investments = Investments.Create();

        var classification = TaxClassificationCalculator.CalculateForCredit(credit, investments);

        using (new AssertionScope())
        {
            classification.GrossAmount.Should().Be(100m);
            classification.WithheldAmount.Should().Be(10m);
            classification.NetAmount.Should().Be(90m);
        }
    }

    [Fact]
    public void CalculateForCredit_NoApplicableRule_IsIncomplete()
    {
        var credit = Credit.Create(new DateTime(2026, 6, 1), Credit.CreditType.Dividend, 100m, 0m, Currency.BRL);
        var investments = Investments.Create();

        var classification = TaxClassificationCalculator.CalculateForCredit(credit, investments);

        classification.CalculationStatus.Should().Be(CalculationStatus.Incomplete);
    }

    [Fact]
    public void CalculateForCredit_ApplicableRule_IsFinalAndReferencesTheRule()
    {
        var credit = Credit.Create(new DateTime(2026, 6, 1), Credit.CreditType.Dividend, 100m, 0m, Currency.BRL);
        var investments = Investments.Create();
        var rule = investments.CreateTaxRule(
            Jurisdiction.BR, EventCategory.Dividend, "BR dividend", "desc", new DateOnly(2026, 1, 1), null);

        var classification = TaxClassificationCalculator.CalculateForCredit(credit, investments);

        using (new AssertionScope())
        {
            classification.CalculationStatus.Should().Be(CalculationStatus.Final);
            classification.TaxRuleId.Should().Be(rule.Id);
        }
    }

    [Fact]
    public void CalculateForCredit_TaxYear_BrlUsesPlainCalendarYear()
    {
        var credit = Credit.Create(new DateTime(2026, 3, 1), Credit.CreditType.Dividend, 100m, 0m, Currency.BRL);
        var investments = Investments.Create();

        var classification = TaxClassificationCalculator.CalculateForCredit(credit, investments);

        classification.TaxYear.Should().Be("2026");
    }

    [Fact]
    public void CalculateForCredit_NonBrlUsesUkTaxYear()
    {
        var credit = Credit.Create(new DateTime(2026, 3, 1), Credit.CreditType.Dividend, 100m, 0m, Currency.GBP);
        var investments = Investments.Create();

        var classification = TaxClassificationCalculator.CalculateForCredit(credit, investments);

        classification.TaxYear.Should().Be("2025/26");
    }

    private static CorporateAction CreateMergerTargetRecord(DateTime? effectiveDate = null) =>
        CorporateAction.CreateMergerTarget(
            effectiveDate ?? new DateTime(2026, 6, 1), null, Guid.NewGuid(), "XCORP", 40m, 1000m);

    [Fact]
    public void CalculateForCorporateAction_DerivesJurisdictionFromCurrency_Brl()
    {
        var target = CreateMergerTargetRecord();
        var investments = Investments.Create();

        var classification = TaxClassificationCalculator.CalculateForCorporateAction(target, Currency.BRL, investments);

        classification.Jurisdiction.Should().Be(Jurisdiction.BR);
    }

    [Theory]
    [InlineData(Currency.GBP)]
    [InlineData(Currency.USD)]
    public void CalculateForCorporateAction_DerivesJurisdictionFromCurrency_NonBrlIsUk(Currency currency)
    {
        var target = CreateMergerTargetRecord();
        var investments = Investments.Create();

        var classification = TaxClassificationCalculator.CalculateForCorporateAction(target, currency, investments);

        classification.Jurisdiction.Should().Be(Jurisdiction.UK);
    }

    [Fact]
    public void CalculateForCorporateAction_NoApplicableRule_RequiresReview()
    {
        var target = CreateMergerTargetRecord();
        var investments = Investments.Create();

        var classification = TaxClassificationCalculator.CalculateForCorporateAction(target, Currency.BRL, investments);

        using (new AssertionScope())
        {
            classification.CalculationStatus.Should().Be(CalculationStatus.RequiresReview);
            classification.TaxRuleId.Should().BeNull();
        }
    }

    [Fact]
    public void CalculateForCorporateAction_ApplicableRule_IsFinalAndReferencesTheRule()
    {
        var target = CreateMergerTargetRecord(new DateTime(2026, 6, 1));
        var investments = Investments.Create();
        var rule = investments.CreateTaxRule(
            Jurisdiction.BR, EventCategory.CorporateAction, "BR corporate actions", "desc", new DateOnly(2026, 1, 1), null);

        var classification = TaxClassificationCalculator.CalculateForCorporateAction(target, Currency.BRL, investments);

        using (new AssertionScope())
        {
            classification.CalculationStatus.Should().Be(CalculationStatus.Final);
            classification.TaxRuleId.Should().Be(rule.Id);
        }
    }

    [Fact]
    public void CalculateForCorporateAction_CopiesFieldsFromTheTargetRecord()
    {
        var target = CreateMergerTargetRecord();
        var investments = Investments.Create();

        var classification = TaxClassificationCalculator.CalculateForCorporateAction(target, Currency.BRL, investments);

        using (new AssertionScope())
        {
            classification.SourceType.Should().Be(SourceType.CorporateAction);
            classification.SourceId.Should().Be(target.Id);
            classification.EventCategory.Should().Be(EventCategory.CorporateAction);
            classification.CostBasis.Should().Be(target.CarriedCostBasis);
        }
    }

    [Fact]
    public void CalculateForCorporateAction_TaxYear_BrlUsesPlainCalendarYear()
    {
        var target = CreateMergerTargetRecord(new DateTime(2026, 3, 1));
        var investments = Investments.Create();

        var classification = TaxClassificationCalculator.CalculateForCorporateAction(target, Currency.BRL, investments);

        classification.TaxYear.Should().Be("2026");
    }

    [Fact]
    public void CalculateForCorporateAction_NonBrlUsesUkTaxYear()
    {
        var target = CreateMergerTargetRecord(new DateTime(2026, 3, 1));
        var investments = Investments.Create();

        var classification = TaxClassificationCalculator.CalculateForCorporateAction(target, Currency.GBP, investments);

        classification.TaxYear.Should().Be("2025/26");
    }
}
