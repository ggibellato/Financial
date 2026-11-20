using Financial.Investment.Application.DTOs;
using Financial.Investment.Application.Services;
using Financial.Investment.Domain.Entities;
using Financial.Investment.Domain.Exceptions;
using Financial.TestUtilities;
using FluentAssertions;
using FluentAssertions.Execution;

namespace Financial.Investment.Application.Tests.Services;

[Trait("Category", "Unit")]
public class TaxRuleServiceTests
{
    private readonly StubInvestmentRepository _repository = new() { Investments = Domain.Entities.Investments.Create() };
    private readonly RecordingTelemetryTracer _tracer = new();
    private readonly RecordingLogger<TaxRuleService> _logger = new();

    [Fact]
    public async Task CreateTaxRuleAsync_ValidRequest_AddsRuleAndPersistsOnce()
    {
        var request = new TaxRuleCreateDTO
        {
            Jurisdiction = Jurisdiction.BR,
            EventCategory = EventCategory.Dividend,
            Label = "BR dividend withholding",
            Description = "Effective 2026.",
            EffectiveFrom = new DateOnly(2026, 1, 1),
            EffectiveTo = null
        };

        var result = await CreateService().CreateTaxRuleAsync(request);

        using (new AssertionScope())
        {
            result.Jurisdiction.Should().Be(Jurisdiction.BR);
            result.EventCategory.Should().Be(EventCategory.Dividend);
            result.Label.Should().Be("BR dividend withholding");
            result.EffectiveFrom.Should().Be(new DateOnly(2026, 1, 1));
            _repository.Investments!.TaxRules.Should().ContainSingle();
            _repository.WriteCallCount.Should().Be(1);
            _tracer.ShouldHaveSucceededSpan("Investment.TaxRuleService.CreateTaxRule");
        }
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task CreateTaxRuleAsync_BlankLabel_ThrowsAndWritesNothing(string label)
    {
        var request = new TaxRuleCreateDTO
        {
            Jurisdiction = Jurisdiction.BR,
            EventCategory = EventCategory.Dividend,
            Label = label,
            EffectiveFrom = new DateOnly(2026, 1, 1)
        };

        var act = async () => await CreateService().CreateTaxRuleAsync(request);

        await act.Should().ThrowAsync<ArgumentException>();
        _repository.WriteCallCount.Should().Be(0);
    }

    [Fact]
    public async Task CreateTaxRuleAsync_OverlappingRange_ThrowsAndWritesNothing()
    {
        _repository.Investments!.CreateTaxRule(
            Jurisdiction.BR, EventCategory.Dividend, "Existing", "desc", new DateOnly(2026, 1, 1), null);
        var request = new TaxRuleCreateDTO
        {
            Jurisdiction = Jurisdiction.BR,
            EventCategory = EventCategory.Dividend,
            Label = "New",
            EffectiveFrom = new DateOnly(2026, 6, 1)
        };

        var act = async () => await CreateService().CreateTaxRuleAsync(request);

        await act.Should().ThrowAsync<InvestmentRuleViolationException>();
        _repository.WriteCallCount.Should().Be(0);
        _tracer.ShouldHaveFailedSpan<InvestmentRuleViolationException>("Investment.TaxRuleService.CreateTaxRule");
    }

    [Fact]
    public async Task UpdateTaxRuleAsync_ValidRequest_ChangesRuleAndPersistsOnce()
    {
        var rule = _repository.Investments!.CreateTaxRule(
            Jurisdiction.BR, EventCategory.Dividend, "Original", "desc", new DateOnly(2026, 1, 1), null);
        var request = new TaxRuleUpdateDTO
        {
            Label = "Updated",
            Description = "New desc",
            EffectiveFrom = new DateOnly(2026, 6, 1),
            EffectiveTo = new DateOnly(2027, 1, 1)
        };

        var result = await CreateService().UpdateTaxRuleAsync(rule.Id, request);

        using (new AssertionScope())
        {
            result.Label.Should().Be("Updated");
            result.EffectiveFrom.Should().Be(new DateOnly(2026, 6, 1));
            _repository.WriteCallCount.Should().Be(1);
            _tracer.ShouldHaveSucceededSpan("Investment.TaxRuleService.UpdateTaxRule");
        }
    }

    [Fact]
    public async Task UpdateTaxRuleAsync_UnknownId_ThrowsNotFoundAndWritesNothing()
    {
        var request = new TaxRuleUpdateDTO { Label = "Label", EffectiveFrom = new DateOnly(2026, 1, 1) };

        var act = async () => await CreateService().UpdateTaxRuleAsync(Guid.NewGuid(), request);

        await act.Should().ThrowAsync<KeyNotFoundException>();
        _repository.WriteCallCount.Should().Be(0);
        _tracer.ShouldHaveFailedSpan<KeyNotFoundException>("Investment.TaxRuleService.UpdateTaxRule");
    }

    [Fact]
    public async Task DeleteTaxRuleAsync_ExistingRule_RemovesItAndPersistsOnce()
    {
        var rule = _repository.Investments!.CreateTaxRule(
            Jurisdiction.BR, EventCategory.Dividend, "Original", "desc", new DateOnly(2026, 1, 1), null);

        await CreateService().DeleteTaxRuleAsync(rule.Id);

        using (new AssertionScope())
        {
            _repository.Investments!.TaxRules.Should().BeEmpty();
            _repository.WriteCallCount.Should().Be(1);
            _tracer.ShouldHaveSucceededSpan("Investment.TaxRuleService.DeleteTaxRule");
        }
    }

    [Fact]
    public async Task DeleteTaxRuleAsync_UnknownId_ThrowsNotFoundAndWritesNothing()
    {
        var act = async () => await CreateService().DeleteTaxRuleAsync(Guid.NewGuid());

        await act.Should().ThrowAsync<KeyNotFoundException>();
        _repository.WriteCallCount.Should().Be(0);
        _tracer.ShouldHaveFailedSpan<KeyNotFoundException>("Investment.TaxRuleService.DeleteTaxRule");
    }

    [Fact]
    public void GetTaxRules_MapsEveryFieldToDto()
    {
        _repository.Investments!.CreateTaxRule(
            Jurisdiction.UK, EventCategory.Interest, "UK interest", "desc",
            new DateOnly(2026, 1, 1), new DateOnly(2027, 1, 1));

        var result = CreateService().GetTaxRules();

        using (new AssertionScope())
        {
            var dto = result.Should().ContainSingle().Subject;
            dto.Jurisdiction.Should().Be(Jurisdiction.UK);
            dto.EventCategory.Should().Be(EventCategory.Interest);
            dto.Label.Should().Be("UK interest");
            dto.Description.Should().Be("desc");
            dto.EffectiveFrom.Should().Be(new DateOnly(2026, 1, 1));
            dto.EffectiveTo.Should().Be(new DateOnly(2027, 1, 1));
            _tracer.ShouldHaveSucceededSpan("Investment.TaxRuleService.GetTaxRules");
        }
    }

    [Fact]
    public void GetTaxRules_WhenRepositoryThrows_RecordsFailedSpanAndRethrows()
    {
        _repository.ThrowOnGetInvestments = new InvalidOperationException("simulated failure");

        var act = () => CreateService().GetTaxRules();

        act.Should().Throw<InvalidOperationException>();
        _tracer.ShouldHaveFailedSpan<InvalidOperationException>("Investment.TaxRuleService.GetTaxRules");
    }

    private TaxRuleService CreateService() => new(_repository, _tracer, _logger);
}
