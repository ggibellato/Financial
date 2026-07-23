using Financial.CashFlow.Application.DTOs;
using FluentAssertions;
using System.Net;
using System.Net.Http.Json;

namespace Financial.Api.Tests;

public class MensaisEndpointsTests
{
    [Fact]
    public async Task CreateBill_ValidRequest_ReturnsOk()
    {
        await using var factory = new ApiTestFactory();
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/v1/financial/mensais", ValidBrasilBillRequest());

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var bill = await response.Content.ReadFromJsonAsync<RecurringBillDTO>();
        bill.Should().NotBeNull();
        bill!.Description.Should().Be("INSS");
        bill.Area.Should().Be("Brasil");
        bill.Status.Should().Be("Unset");
    }

    [Fact]
    public async Task CreateBill_InvalidDueDay_ReturnsBadRequestWithMessage()
    {
        await using var factory = new ApiTestFactory();
        using var client = factory.CreateClient();
        var request = ValidBrasilBillRequest();
        request = new CreateRecurringBillDTO
        {
            DueDay = 32,
            Description = request.Description,
            Value = request.Value,
            Area = request.Area,
            Note = request.Note
        };

        var response = await client.PostAsJsonAsync("/api/v1/financial/mensais", request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("Due day");
    }

    [Fact]
    public async Task CreateBill_NeverSetsNitOrMinimumWage_ThoseAreImportOnly()
    {
        await using var factory = new ApiTestFactory();
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/v1/financial/mensais", ValidBrasilBillRequest());

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var bill = await response.Content.ReadFromJsonAsync<RecurringBillDTO>();
        bill!.NitNumber.Should().BeNull();
        bill.MinimumWageValue.Should().BeNull();
    }

    [Fact]
    public async Task GetBills_ReturnsAllCreatedBills()
    {
        await using var factory = new ApiTestFactory();
        using var client = factory.CreateClient();
        await client.PostAsJsonAsync("/api/v1/financial/mensais", ValidBrasilBillRequest());

        var response = await client.GetAsync("/api/v1/financial/mensais");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var bills = await response.Content.ReadFromJsonAsync<List<RecurringBillDTO>>();
        bills.Should().ContainSingle(b => b.Description == "INSS");
    }

    [Fact]
    public async Task DeleteBill_ExistingId_RemovesBill()
    {
        await using var factory = new ApiTestFactory();
        using var client = factory.CreateClient();
        var created = await client.PostAsJsonAsync("/api/v1/financial/mensais", ValidBrasilBillRequest());
        var bill = await created.Content.ReadFromJsonAsync<RecurringBillDTO>();

        var response = await client.DeleteAsync($"/api/v1/financial/mensais/{bill!.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var bills = await (await client.GetAsync("/api/v1/financial/mensais")).Content.ReadFromJsonAsync<List<RecurringBillDTO>>();
        bills.Should().BeEmpty();
    }

    [Fact]
    public async Task DeleteBill_UnknownId_ReturnsNotFound()
    {
        await using var factory = new ApiTestFactory();
        using var client = factory.CreateClient();

        var response = await client.DeleteAsync($"/api/v1/financial/mensais/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task UpdateBill_ExistingId_ReturnsOkAndUpdatesFields()
    {
        await using var factory = new ApiTestFactory();
        using var client = factory.CreateClient();
        var created = await client.PostAsJsonAsync("/api/v1/financial/mensais", ValidBrasilBillRequest());
        var bill = await created.Content.ReadFromJsonAsync<RecurringBillDTO>();

        var response = await client.PutAsJsonAsync($"/api/v1/financial/mensais/{bill!.Id}", new UpdateRecurringBillDTO
        {
            Status = "Paid",
            Value = 900m
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var updated = await response.Content.ReadFromJsonAsync<RecurringBillDTO>();
        updated!.Status.Should().Be("Paid");
        updated.Value.Should().Be(900m);
    }

    [Fact]
    public async Task UpdateBill_UnknownId_ReturnsNotFound()
    {
        await using var factory = new ApiTestFactory();
        using var client = factory.CreateClient();

        var response = await client.PutAsJsonAsync($"/api/v1/financial/mensais/{Guid.NewGuid()}", new UpdateRecurringBillDTO
        {
            Status = "Paid",
            Value = 100m
        });

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task ResetAllToUnset_SetsEveryBillStatusBackToUnset()
    {
        await using var factory = new ApiTestFactory();
        using var client = factory.CreateClient();
        var created = await client.PostAsJsonAsync("/api/v1/financial/mensais", ValidBrasilBillRequest());
        var bill = await created.Content.ReadFromJsonAsync<RecurringBillDTO>();
        await client.PutAsJsonAsync($"/api/v1/financial/mensais/{bill!.Id}", new UpdateRecurringBillDTO { Status = "Paid", Value = 900m });

        var response = await client.PostAsync("/api/v1/financial/mensais/reset", null);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var bills = await response.Content.ReadFromJsonAsync<List<RecurringBillDTO>>();
        bills.Should().ContainSingle().Which.Status.Should().Be("Unset");
    }

    private static CreateRecurringBillDTO ValidBrasilBillRequest() => new()
    {
        DueDay = 10,
        Description = "INSS",
        Value = 850m,
        Area = "Brasil",
        Note = "Direct debit"
    };
}
