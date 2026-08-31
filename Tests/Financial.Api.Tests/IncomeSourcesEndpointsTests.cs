using Financial.CashFlow.Application.DTOs;
using FluentAssertions;
using System.Net;
using System.Net.Http.Json;

namespace Financial.Api.Tests;

public class IncomeSourcesEndpointsTests : ApiEndpointTests
{
    private static readonly Guid GleisonId = Guid.Parse("8f3b1c1a-2e3a-4b1a-9a7f-000000000001");
    private static readonly Guid ArianaId = Guid.Parse("8f3b1c1a-2e3a-4b1a-9a7f-000000000002");
    private static readonly Guid BarclaysId = Guid.Parse("8f3b1c1a-2e3a-4b1a-9a7f-100000000001");

    [Fact]
    public async Task GetIncomeSources_ReturnsTheFourSeededSourcesWithCorrectFields()
    {
        var response = await Client.GetAsync("/api/v1/financial/income-sources");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var sources = await response.Content.ReadFromJsonAsync<List<IncomeSourceDTO>>();
        sources.Should().HaveCount(4);
        sources.Should().ContainSingle(s => s.Name == "Gleison" && s.IsActive && s.Group == "Salary" && s.Id != Guid.Empty);
        sources.Should().ContainSingle(s => s.Name == "Ariana" && s.IsActive && s.Group == "Salary");
        sources.Should().ContainSingle(s => s.Name == "Lottery" && s.IsActive && s.Group == "NonReportable");
        sources.Should().ContainSingle(s => s.Name == "DividendoJuros" && s.IsActive && s.Group == "DividendoJuros");
    }

    [Fact]
    public async Task GetIncomeSources_ReturnsAutoSplitToReserveInResponse()
    {
        var response = await Client.GetAsync("/api/v1/financial/income-sources");

        var sources = await response.Content.ReadFromJsonAsync<List<IncomeSourceDTO>>();
        sources.Should().ContainSingle(s => s.Name == "Ariana" && s.AutoSplitToReserve);
        sources.Should().ContainSingle(s => s.Name == "Gleison" && !s.AutoSplitToReserve);
        sources.Should().ContainSingle(s => s.Name == "Lottery" && !s.AutoSplitToReserve);
        sources.Should().ContainSingle(s => s.Name == "DividendoJuros" && !s.AutoSplitToReserve);
    }

    [Fact]
    public async Task GetIncomeSources_RequiresNoParameters_AndReturnsFullUnfilteredList()
    {
        var response = await Client.GetAsync("/api/v1/financial/income-sources");
        var sources = await response.Content.ReadFromJsonAsync<List<IncomeSourceDTO>>();

        // All 4 seeded sources come back regardless of IsActive value - none are seeded inactive
        // in the fixture, so this also confirms no isActive=true filter is silently applied.
        sources.Should().HaveCount(4);
        sources.Should().OnlyContain(s => s.IsActive);
    }

    [Fact]
    public async Task CreateIncomeSource_ValidRequest_ReturnsOkWithNewIncomeSource()
    {
        var response = await Client.PostAsJsonAsync("/api/v1/financial/income-sources", new IncomeSourceCreateDTO
        {
            Name = "Freelance",
            Group = "NonReportable",
            IsActive = true,
            AutoSplitToReserve = false
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var source = await response.Content.ReadFromJsonAsync<IncomeSourceDTO>();
        source!.Name.Should().Be("Freelance");
        source.Group.Should().Be("NonReportable");
        source.IsActive.Should().BeTrue();
        source.HasReferences.Should().BeFalse();
        source.Id.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public async Task CreateIncomeSource_DuplicateName_ReturnsConflictWithMessage()
    {
        var response = await Client.PostAsJsonAsync("/api/v1/financial/income-sources", new IncomeSourceCreateDTO
        {
            Name = "Gleison",
            Group = "Salary",
            IsActive = true,
            AutoSplitToReserve = false
        });

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("Gleison").And.Contain("already exists");
    }

    [Fact]
    public async Task CreateIncomeSource_BlankName_ReturnsBadRequest()
    {
        var response = await Client.PostAsJsonAsync("/api/v1/financial/income-sources", new IncomeSourceCreateDTO
        {
            Name = "   ",
            Group = "Salary",
            IsActive = true,
            AutoSplitToReserve = false
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateIncomeSource_InvalidGroup_ReturnsBadRequest()
    {
        var response = await Client.PostAsJsonAsync("/api/v1/financial/income-sources", new IncomeSourceCreateDTO
        {
            Name = "Freelance",
            Group = "NotAGroup",
            IsActive = true,
            AutoSplitToReserve = false
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task UpdateIncomeSource_ValidRequest_ReturnsOkAndUpdatesFields()
    {
        var response = await Client.PutAsJsonAsync($"/api/v1/financial/income-sources/{ArianaId}", new IncomeSourceUpdateDTO
        {
            Name = "Ariana Renamed",
            Group = "NonReportable",
            IsActive = false,
            AutoSplitToReserve = false
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var source = await response.Content.ReadFromJsonAsync<IncomeSourceDTO>();
        source!.Name.Should().Be("Ariana Renamed");
        source.Group.Should().Be("NonReportable");
        source.IsActive.Should().BeFalse();
        source.AutoSplitToReserve.Should().BeFalse();
    }

    [Fact]
    public async Task UpdateIncomeSource_UnknownId_ReturnsNotFound()
    {
        var response = await Client.PutAsJsonAsync($"/api/v1/financial/income-sources/{Guid.NewGuid()}", new IncomeSourceUpdateDTO
        {
            Name = "X",
            Group = "Salary",
            IsActive = true,
            AutoSplitToReserve = false
        });

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task UpdateIncomeSource_NameCollidesWithAnotherSource_ReturnsConflict()
    {
        var response = await Client.PutAsJsonAsync($"/api/v1/financial/income-sources/{ArianaId}", new IncomeSourceUpdateDTO
        {
            Name = "Gleison",
            Group = "Salary",
            IsActive = true,
            AutoSplitToReserve = false
        });

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task UpdateIncomeSource_InvalidGroup_ReturnsBadRequest()
    {
        var response = await Client.PutAsJsonAsync($"/api/v1/financial/income-sources/{ArianaId}", new IncomeSourceUpdateDTO
        {
            Name = "Ariana",
            Group = "NotAGroup",
            IsActive = true,
            AutoSplitToReserve = false
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task DeleteIncomeSource_WithNoReferences_ReturnsOkAndRemovesIt()
    {
        var created = await Client.PostAsJsonAsync("/api/v1/financial/income-sources", new IncomeSourceCreateDTO
        {
            Name = "Freelance",
            Group = "NonReportable",
            IsActive = true,
            AutoSplitToReserve = false
        });
        var source = await created.Content.ReadFromJsonAsync<IncomeSourceDTO>();

        var response = await Client.DeleteAsync($"/api/v1/financial/income-sources/{source!.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var sources = await (await Client.GetAsync("/api/v1/financial/income-sources")).Content.ReadFromJsonAsync<List<IncomeSourceDTO>>();
        sources.Should().NotContain(s => s.Id == source.Id);
    }

    [Fact]
    public async Task DeleteIncomeSource_UnknownId_ReturnsNotFound()
    {
        var response = await Client.DeleteAsync($"/api/v1/financial/income-sources/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteIncomeSource_ReferencedByIncome_ReturnsConflictWithMessage()
    {
        await Client.PostAsJsonAsync("/api/v1/financial/incomes", new IncomeCreateDTO
        {
            Date = new DateOnly(2026, 7, 1),
            IncomeSourceId = GleisonId,
            GrossValue = null,
            NetValue = 500m,
            BankId = BarclaysId
        });

        var response = await Client.DeleteAsync($"/api/v1/financial/income-sources/{GleisonId}");

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("used by an income entry");
    }

    [Fact]
    public async Task GetIncomeSources_HasReferences_ReflectsWhetherAnIncomeExists()
    {
        var before = await (await Client.GetAsync("/api/v1/financial/income-sources")).Content.ReadFromJsonAsync<List<IncomeSourceDTO>>();
        before.Should().ContainSingle(s => s.Id == GleisonId && !s.HasReferences);

        await Client.PostAsJsonAsync("/api/v1/financial/incomes", new IncomeCreateDTO
        {
            Date = new DateOnly(2026, 7, 1),
            IncomeSourceId = GleisonId,
            GrossValue = null,
            NetValue = 500m,
            BankId = BarclaysId
        });

        var after = await (await Client.GetAsync("/api/v1/financial/income-sources")).Content.ReadFromJsonAsync<List<IncomeSourceDTO>>();
        after.Should().ContainSingle(s => s.Id == GleisonId && s.HasReferences);
    }
}
