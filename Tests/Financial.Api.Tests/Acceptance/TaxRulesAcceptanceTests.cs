using System.Net;
using System.Net.Http.Json;
using Financial.Investment.Application.DTOs;
using Financial.Investment.Domain.Entities;
using FluentAssertions;

namespace Financial.Api.Tests.Acceptance;

public class TaxRulesAcceptanceTests : ApiEndpointTests
{
    private const string BaseRoute = "/api/v1/financial/tax-rules";

    [Fact]
    [Trait("AC", "P51-F01-tax-rules-01")]
    public async Task CreatedTaxRule_IsReadBackWithEveryField()
    {
        var request = new TaxRuleCreateDTO
        {
            Jurisdiction = Jurisdiction.BR,
            EventCategory = EventCategory.Dividend,
            Label = "BR dividend withholding — 2026 change",
            Description = "Effective 2026-01-01.",
            EffectiveFrom = new DateOnly(2026, 1, 1),
            EffectiveTo = null
        };

        var createResponse = await Client.PostAsJsonAsync(BaseRoute, request);
        createResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var created = await createResponse.Content.ReadFromJsonAsync<TaxRuleDTO>();

        var rules = await Client.GetFromJsonAsync<List<TaxRuleDTO>>(BaseRoute);

        var found = rules.Should().ContainSingle(r => r.Id == created!.Id).Subject;
        found.Jurisdiction.Should().Be(Jurisdiction.BR);
        found.EventCategory.Should().Be(EventCategory.Dividend);
        found.Label.Should().Be("BR dividend withholding — 2026 change");
        found.Description.Should().Be("Effective 2026-01-01.");
        found.EffectiveFrom.Should().Be(new DateOnly(2026, 1, 1));
        found.EffectiveTo.Should().BeNull();
    }

    [Fact]
    [Trait("AC", "P51-F01-tax-rules-02")]
    public async Task OverlappingRangeForSameJurisdictionAndCategory_IsRejected()
    {
        await Client.PostAsJsonAsync(BaseRoute, new TaxRuleCreateDTO
        {
            Jurisdiction = Jurisdiction.BR,
            EventCategory = EventCategory.Dividend,
            Label = "First rule",
            EffectiveFrom = new DateOnly(2026, 1, 1),
            EffectiveTo = new DateOnly(2027, 1, 1)
        });

        var response = await Client.PostAsJsonAsync(BaseRoute, new TaxRuleCreateDTO
        {
            Jurisdiction = Jurisdiction.BR,
            EventCategory = EventCategory.Dividend,
            Label = "Second rule",
            EffectiveFrom = new DateOnly(2026, 6, 1),
            EffectiveTo = null
        });

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    [Trait("AC", "P51-F01-tax-rules-03")]
    public async Task EffectiveFromOnOrAfterEffectiveTo_IsRejected()
    {
        var response = await Client.PostAsJsonAsync(BaseRoute, new TaxRuleCreateDTO
        {
            Jurisdiction = Jurisdiction.BR,
            EventCategory = EventCategory.Dividend,
            Label = "Invalid range",
            EffectiveFrom = new DateOnly(2026, 6, 1),
            EffectiveTo = new DateOnly(2026, 1, 1)
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    [Trait("AC", "P51-F01-tax-rules-02")]
    public async Task UpdatingARule_IntoAnOverlappingRangeForSameJurisdictionAndCategory_IsRejected()
    {
        await Client.PostAsJsonAsync(BaseRoute, new TaxRuleCreateDTO
        {
            Jurisdiction = Jurisdiction.BR,
            EventCategory = EventCategory.Dividend,
            Label = "First rule",
            EffectiveFrom = new DateOnly(2026, 1, 1),
            EffectiveTo = new DateOnly(2027, 1, 1)
        });
        await Client.PostAsJsonAsync(BaseRoute, new TaxRuleCreateDTO
        {
            Jurisdiction = Jurisdiction.BR,
            EventCategory = EventCategory.Dividend,
            Label = "Second rule",
            EffectiveFrom = new DateOnly(2027, 1, 1),
            EffectiveTo = null
        });
        var second = await Client.GetFromJsonAsync<List<TaxRuleDTO>>(BaseRoute);
        var secondId = second!.Single(r => r.Label == "Second rule").Id;

        var response = await Client.PutAsJsonAsync($"{BaseRoute}/{secondId}", new TaxRuleUpdateDTO
        {
            Label = "Second rule",
            EffectiveFrom = new DateOnly(2026, 6, 1),
            EffectiveTo = null
        });

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    [Trait("AC", "P51-F01-tax-rules-06")]
    public async Task TwoRulesForTheSameJurisdictionAndCategory_CanNeverBothCoverOneDate()
    {
        await Client.PostAsJsonAsync(BaseRoute, new TaxRuleCreateDTO
        {
            Jurisdiction = Jurisdiction.UK,
            EventCategory = EventCategory.Interest,
            Label = "First",
            EffectiveFrom = new DateOnly(2026, 1, 1),
            EffectiveTo = new DateOnly(2027, 1, 1)
        });

        var adjacent = await Client.PostAsJsonAsync(BaseRoute, new TaxRuleCreateDTO
        {
            Jurisdiction = Jurisdiction.UK,
            EventCategory = EventCategory.Interest,
            Label = "Adjacent, non-overlapping",
            EffectiveFrom = new DateOnly(2027, 1, 1),
            EffectiveTo = null
        });
        adjacent.StatusCode.Should().Be(HttpStatusCode.OK);

        var overlapping = await Client.PostAsJsonAsync(BaseRoute, new TaxRuleCreateDTO
        {
            Jurisdiction = Jurisdiction.UK,
            EventCategory = EventCategory.Interest,
            Label = "Overlapping",
            EffectiveFrom = new DateOnly(2026, 6, 1),
            EffectiveTo = null
        });

        overlapping.StatusCode.Should().Be(HttpStatusCode.Conflict);
        var rules = await Client.GetFromJsonAsync<List<TaxRuleDTO>>(BaseRoute);
        rules.Should().HaveCount(2);
    }

    [Fact]
    public async Task UpdateTaxRule_UnknownId_ReturnsNotFound()
    {
        var response = await Client.PutAsJsonAsync($"{BaseRoute}/{Guid.NewGuid()}", new TaxRuleUpdateDTO
        {
            Label = "Label",
            EffectiveFrom = new DateOnly(2026, 1, 1)
        });

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteTaxRule_ThenDeleteAgain_ReturnsNoContentThenNotFound()
    {
        var created = await Client.PostAsJsonAsync(BaseRoute, new TaxRuleCreateDTO
        {
            Jurisdiction = Jurisdiction.BR,
            EventCategory = EventCategory.CapitalGain,
            Label = "To delete",
            EffectiveFrom = new DateOnly(2026, 1, 1)
        });
        var rule = await created.Content.ReadFromJsonAsync<TaxRuleDTO>();

        var firstDelete = await Client.DeleteAsync($"{BaseRoute}/{rule!.Id}");
        var secondDelete = await Client.DeleteAsync($"{BaseRoute}/{rule.Id}");

        firstDelete.StatusCode.Should().Be(HttpStatusCode.NoContent);
        secondDelete.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
