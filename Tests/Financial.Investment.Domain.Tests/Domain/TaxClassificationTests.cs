using Financial.Investment.Domain.Entities;
using FluentAssertions;
using FluentAssertions.Execution;

namespace Financial.Investment.Domain.Tests;

public class TaxClassificationTests
{
    [Fact]
    public void CreateForDisposal_SetsProperties()
    {
        var disposalId = Guid.NewGuid();
        var ruleId = Guid.NewGuid();

        var classification = TaxClassification.CreateForDisposal(
            disposalId, Jurisdiction.BR, "2026", 1000m, 600m, 400m, CalculationStatus.Final, ruleId);

        using (new AssertionScope())
        {
            classification.Id.Should().NotBeEmpty();
            classification.SourceType.Should().Be(SourceType.Disposal);
            classification.SourceId.Should().Be(disposalId);
            classification.Jurisdiction.Should().Be(Jurisdiction.BR);
            classification.TaxYear.Should().Be("2026");
            classification.EventCategory.Should().Be(EventCategory.CapitalGain);
            classification.Proceeds.Should().Be(1000m);
            classification.CostBasis.Should().Be(600m);
            classification.GainLoss.Should().Be(400m);
            classification.GrossAmount.Should().BeNull();
            classification.WithheldAmount.Should().BeNull();
            classification.NetAmount.Should().BeNull();
            classification.CalculationStatus.Should().Be(CalculationStatus.Final);
            classification.TaxRuleId.Should().Be(ruleId);
            classification.Status.Should().Be(TaxClassificationStatus.Active);
            classification.SupersededByClassificationId.Should().BeNull();
        }
    }

    [Fact]
    public void CreateForCredit_SetsProperties()
    {
        var creditId = Guid.NewGuid();

        var classification = TaxClassification.CreateForCredit(
            creditId, Jurisdiction.UK, "2025/26", EventCategory.Interest, 100m, 20m, 80m, CalculationStatus.Incomplete, null);

        using (new AssertionScope())
        {
            classification.SourceType.Should().Be(SourceType.Credit);
            classification.SourceId.Should().Be(creditId);
            classification.EventCategory.Should().Be(EventCategory.Interest);
            classification.GrossAmount.Should().Be(100m);
            classification.WithheldAmount.Should().Be(20m);
            classification.NetAmount.Should().Be(80m);
            classification.Proceeds.Should().BeNull();
            classification.CostBasis.Should().BeNull();
            classification.GainLoss.Should().BeNull();
            classification.CalculationStatus.Should().Be(CalculationStatus.Incomplete);
            classification.TaxRuleId.Should().BeNull();
        }
    }

    [Fact]
    public void Supersede_SetsStatusAndReplacementId()
    {
        var classification = TaxClassification.CreateForCredit(
            Guid.NewGuid(), Jurisdiction.BR, "2026", EventCategory.Dividend, 100m, 0m, 100m, CalculationStatus.Final, Guid.NewGuid());
        var replacementId = Guid.NewGuid();

        classification.Supersede(replacementId);

        using (new AssertionScope())
        {
            classification.Status.Should().Be(TaxClassificationStatus.Superseded);
            classification.SupersededByClassificationId.Should().Be(replacementId);
        }
    }

    [Fact]
    public void Supersede_AlreadySuperseded_Throws()
    {
        var classification = TaxClassification.CreateForCredit(
            Guid.NewGuid(), Jurisdiction.BR, "2026", EventCategory.Dividend, 100m, 0m, 100m, CalculationStatus.Final, null);
        classification.Supersede(null);

        Action act = () => classification.Supersede(Guid.NewGuid());

        act.Should().Throw<InvalidOperationException>();
    }
}
