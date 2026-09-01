using Financial.CashFlow.Application.DTOs;
using FluentAssertions;
using System.Net;
using System.Net.Http.Json;

namespace Financial.Api.Tests;

public class MensaisEndpointsTests : ApiEndpointTests
{
    [Fact]
    public async Task CreateBill_ValidRequest_ReturnsOk()
    {
        var response = await Client.PostAsJsonAsync("/api/v1/financial/mensais", ValidBrasilBillRequest());

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
        var request = ValidBrasilBillRequest();
        request = new RecurringBillCreateDTO
        {
            DueDay = 32,
            Description = request.Description,
            Value = request.Value,
            Area = request.Area,
            Note = request.Note
        };

        var response = await Client.PostAsJsonAsync("/api/v1/financial/mensais", request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("Due day");
    }

    [Fact]
    public async Task CreateBill_NeverSetsNitOrMinimumWage_ThoseAreImportOnly()
    {
        var response = await Client.PostAsJsonAsync("/api/v1/financial/mensais", ValidBrasilBillRequest());

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var bill = await response.Content.ReadFromJsonAsync<RecurringBillDTO>();
        bill!.NitNumber.Should().BeNull();
        bill.MinimumWageValue.Should().BeNull();
    }

    [Fact]
    public async Task GetBills_ReturnsAllCreatedBills()
    {
        await Client.PostAsJsonAsync("/api/v1/financial/mensais", ValidBrasilBillRequest());

        var response = await Client.GetAsync("/api/v1/financial/mensais");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var bills = await response.Content.ReadFromJsonAsync<List<RecurringBillDTO>>();
        bills.Should().ContainSingle(b => b.Description == "INSS");
    }

    [Fact]
    public async Task DeleteBill_ExistingId_RemovesBill()
    {
        var created = await Client.PostAsJsonAsync("/api/v1/financial/mensais", ValidBrasilBillRequest());
        var bill = await created.Content.ReadFromJsonAsync<RecurringBillDTO>();

        var response = await Client.DeleteAsync($"/api/v1/financial/mensais/{bill!.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var bills = await (await Client.GetAsync("/api/v1/financial/mensais")).Content.ReadFromJsonAsync<List<RecurringBillDTO>>();
        bills.Should().BeEmpty();
    }

    [Fact]
    public async Task DeleteBill_UnknownId_ReturnsNotFound()
    {
        var response = await Client.DeleteAsync($"/api/v1/financial/mensais/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task UpdateBill_ExistingId_ReturnsOkAndUpdatesEveryField()
    {
        var created = await Client.PostAsJsonAsync("/api/v1/financial/mensais", ValidBrasilBillRequest());
        var bill = await created.Content.ReadFromJsonAsync<RecurringBillDTO>();

        var response = await Client.PutAsJsonAsync($"/api/v1/financial/mensais/{bill!.Id}", new RecurringBillUpdateDTO
        {
            DueDay = 15,
            Description = "INSS Renamed",
            Value = 900m,
            Area = "UK",
            Note = "Updated note",
            NitNumber = "12345678901",
            MinimumWageValue = 1621m,
            Status = "Paid",
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var updated = await response.Content.ReadFromJsonAsync<RecurringBillDTO>();
        updated!.DueDay.Should().Be(15);
        updated.Description.Should().Be("INSS Renamed");
        updated.Value.Should().Be(900m);
        updated.Area.Should().Be("UK");
        updated.Note.Should().Be("Updated note");
        updated.NitNumber.Should().Be("12345678901");
        updated.MinimumWageValue.Should().Be(1621m);
        updated.Status.Should().Be("Paid");
    }

    [Fact]
    public async Task UpdateBill_InvalidDueDay_ReturnsBadRequest()
    {
        var created = await Client.PostAsJsonAsync("/api/v1/financial/mensais", ValidBrasilBillRequest());
        var bill = await created.Content.ReadFromJsonAsync<RecurringBillDTO>();

        var response = await Client.PutAsJsonAsync($"/api/v1/financial/mensais/{bill!.Id}", ValidUpdateRequest(dueDay: 32));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task UpdateBill_InvalidArea_ReturnsBadRequest()
    {
        var created = await Client.PostAsJsonAsync("/api/v1/financial/mensais", ValidBrasilBillRequest());
        var bill = await created.Content.ReadFromJsonAsync<RecurringBillDTO>();

        var response = await Client.PutAsJsonAsync($"/api/v1/financial/mensais/{bill!.Id}", ValidUpdateRequest(area: "NotAnArea"));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task UpdateBill_UnknownId_ReturnsNotFound()
    {
        var response = await Client.PutAsJsonAsync($"/api/v1/financial/mensais/{Guid.NewGuid()}", ValidUpdateRequest());

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task UpdateBillStatus_ValidRequest_ReturnsOkWithUpdatedStatus()
    {
        var created = await Client.PostAsJsonAsync("/api/v1/financial/mensais", ValidBrasilBillRequest());
        var bill = await created.Content.ReadFromJsonAsync<RecurringBillDTO>();

        var response = await Client.PostAsJsonAsync(
            $"/api/v1/financial/mensais/{bill!.Id}/status", new RecurringBillStatusUpdateDTO { Status = "Paid" });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var updated = await response.Content.ReadFromJsonAsync<RecurringBillDTO>();
        updated!.Status.Should().Be("Paid");
    }

    [Fact]
    public async Task UpdateBillStatus_DoesNotChangeOtherFields()
    {
        var created = await Client.PostAsJsonAsync("/api/v1/financial/mensais", ValidBrasilBillRequest());
        var bill = await created.Content.ReadFromJsonAsync<RecurringBillDTO>();

        var response = await Client.PostAsJsonAsync(
            $"/api/v1/financial/mensais/{bill!.Id}/status", new RecurringBillStatusUpdateDTO { Status = "Paid" });

        var updated = await response.Content.ReadFromJsonAsync<RecurringBillDTO>();
        updated!.DueDay.Should().Be(bill.DueDay);
        updated.Description.Should().Be(bill.Description);
        updated.Value.Should().Be(bill.Value);
        updated.Area.Should().Be(bill.Area);
        updated.Note.Should().Be(bill.Note);
        updated.NitNumber.Should().Be(bill.NitNumber);
        updated.MinimumWageValue.Should().Be(bill.MinimumWageValue);
    }

    [Fact]
    public async Task UpdateBillStatus_UnknownId_ReturnsNotFound()
    {
        var response = await Client.PostAsJsonAsync(
            $"/api/v1/financial/mensais/{Guid.NewGuid()}/status", new RecurringBillStatusUpdateDTO { Status = "Paid" });

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task UpdateBillStatus_InvalidStatusValue_ReturnsBadRequestWithMessage()
    {
        var created = await Client.PostAsJsonAsync("/api/v1/financial/mensais", ValidBrasilBillRequest());
        var bill = await created.Content.ReadFromJsonAsync<RecurringBillDTO>();

        var response = await Client.PostAsJsonAsync(
            $"/api/v1/financial/mensais/{bill!.Id}/status", new RecurringBillStatusUpdateDTO { Status = "NotAStatus" });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("NotAStatus");
    }

    [Fact]
    public async Task ResetAllToUnset_SetsEveryBillStatusBackToUnset()
    {
        var created = await Client.PostAsJsonAsync("/api/v1/financial/mensais", ValidBrasilBillRequest());
        var bill = await created.Content.ReadFromJsonAsync<RecurringBillDTO>();
        await Client.PutAsJsonAsync($"/api/v1/financial/mensais/{bill!.Id}", ValidUpdateRequest());

        var response = await Client.PostAsync("/api/v1/financial/mensais/reset", null);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var bills = await response.Content.ReadFromJsonAsync<List<RecurringBillDTO>>();
        bills.Should().ContainSingle().Which.Status.Should().Be("Unset");
    }

    private static RecurringBillCreateDTO ValidBrasilBillRequest() => new()
    {
        DueDay = 10,
        Description = "INSS",
        Value = 850m,
        Area = "Brasil",
        Note = "Direct debit"
    };

    private static RecurringBillUpdateDTO ValidUpdateRequest(int dueDay = 10, string area = "Brasil") => new()
    {
        DueDay = dueDay,
        Description = "INSS",
        Value = 900m,
        Area = area,
        Note = string.Empty,
        Status = "Paid",
    };
}
