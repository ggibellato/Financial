using Financial.CashFlow.Application.DTOs;
using FluentAssertions;
using System.Net;
using System.Net.Http.Json;

namespace Financial.Api.Tests;

public class MensaisEndpointsTests
{
    [Fact]
    public async Task CreateTemplate_ValidRequest_ReturnsOk()
    {
        await using var factory = new ApiTestFactory();
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/v1/financial/mensais/templates", ValidBrasilTemplateRequest());

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var template = await response.Content.ReadFromJsonAsync<RecurringBillTemplateDTO>();
        template.Should().NotBeNull();
        template!.Description.Should().Be("INSS");
        template.Area.Should().Be("Brasil");
        template.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task CreateTemplate_InvalidDueDay_ReturnsBadRequestWithMessage()
    {
        await using var factory = new ApiTestFactory();
        using var client = factory.CreateClient();
        var request = ValidBrasilTemplateRequest();
        request = new CreateRecurringBillTemplateDTO
        {
            DueDay = 32,
            Description = request.Description,
            Value = request.Value,
            Area = request.Area,
            Note = request.Note,
            NitNumber = request.NitNumber,
            MinimumWageValue = request.MinimumWageValue
        };

        var response = await client.PostAsJsonAsync("/api/v1/financial/mensais/templates", request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("Due day");
    }

    [Fact]
    public async Task CreateTemplate_UkTemplateWithoutNitOrMinimumWage_ReturnsOk()
    {
        await using var factory = new ApiTestFactory();
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/v1/financial/mensais/templates", new CreateRecurringBillTemplateDTO
        {
            DueDay = 15,
            Description = "Council Tax",
            Value = 120m,
            Area = "UK",
            Note = string.Empty,
            NitNumber = null,
            MinimumWageValue = null
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var template = await response.Content.ReadFromJsonAsync<RecurringBillTemplateDTO>();
        template!.NitNumber.Should().BeNull();
        template.MinimumWageValue.Should().BeNull();
    }

    [Fact]
    public async Task GetTemplates_ReturnsAllCreatedTemplates()
    {
        await using var factory = new ApiTestFactory();
        using var client = factory.CreateClient();
        await client.PostAsJsonAsync("/api/v1/financial/mensais/templates", ValidBrasilTemplateRequest());

        var response = await client.GetAsync("/api/v1/financial/mensais/templates");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var templates = await response.Content.ReadFromJsonAsync<List<RecurringBillTemplateDTO>>();
        templates.Should().ContainSingle(t => t.Description == "INSS");
    }

    [Fact]
    public async Task GetInstancesForMonth_FirstCall_GeneratesInstanceFromTemplate()
    {
        await using var factory = new ApiTestFactory();
        using var client = factory.CreateClient();
        var created = await client.PostAsJsonAsync("/api/v1/financial/mensais/templates", ValidBrasilTemplateRequest());
        var template = await created.Content.ReadFromJsonAsync<RecurringBillTemplateDTO>();

        var response = await client.GetAsync("/api/v1/financial/mensais/2026/7");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var instances = await response.Content.ReadFromJsonAsync<List<RecurringBillInstanceDTO>>();
        instances.Should().ContainSingle();
        instances![0].TemplateId.Should().Be(template!.Id);
        instances[0].Status.Should().Be("Unset");
        instances[0].Value.Should().Be(850m);
    }

    [Fact]
    public async Task GetInstancesForMonth_SecondCall_DoesNotDuplicateInstances()
    {
        await using var factory = new ApiTestFactory();
        using var client = factory.CreateClient();
        await client.PostAsJsonAsync("/api/v1/financial/mensais/templates", ValidBrasilTemplateRequest());
        await client.GetAsync("/api/v1/financial/mensais/2026/7");

        var response = await client.GetAsync("/api/v1/financial/mensais/2026/7");

        var instances = await response.Content.ReadFromJsonAsync<List<RecurringBillInstanceDTO>>();
        instances.Should().ContainSingle();
    }

    [Fact]
    public async Task UpdateInstance_ExistingId_ReturnsOkAndUpdatesFields()
    {
        await using var factory = new ApiTestFactory();
        using var client = factory.CreateClient();
        await client.PostAsJsonAsync("/api/v1/financial/mensais/templates", ValidBrasilTemplateRequest());
        var monthResponse = await client.GetAsync("/api/v1/financial/mensais/2026/7");
        var instances = await monthResponse.Content.ReadFromJsonAsync<List<RecurringBillInstanceDTO>>();
        var instance = instances!.Single();

        var response = await client.PutAsJsonAsync($"/api/v1/financial/mensais/instances/{instance.Id}", new UpdateRecurringBillInstanceDTO
        {
            Status = "Paid",
            Value = 900m
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var updated = await response.Content.ReadFromJsonAsync<RecurringBillInstanceDTO>();
        updated!.Status.Should().Be("Paid");
        updated.Value.Should().Be(900m);
    }

    [Fact]
    public async Task UpdateInstance_UnknownId_ReturnsNotFound()
    {
        await using var factory = new ApiTestFactory();
        using var client = factory.CreateClient();

        var response = await client.PutAsJsonAsync($"/api/v1/financial/mensais/instances/{Guid.NewGuid()}", new UpdateRecurringBillInstanceDTO
        {
            Status = "Paid",
            Value = 100m
        });

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    private static CreateRecurringBillTemplateDTO ValidBrasilTemplateRequest() => new()
    {
        DueDay = 10,
        Description = "INSS",
        Value = 850m,
        Area = "Brasil",
        Note = "Direct debit",
        NitNumber = "12345678901",
        MinimumWageValue = 1412m
    };
}
