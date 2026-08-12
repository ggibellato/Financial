using Financial.CashFlow.Application.DTOs;
using FluentAssertions;
using System.Net;
using System.Net.Http.Json;

namespace Financial.Api.Tests;

public class CreditCardsEndpointsTests
{
    private static readonly Guid BaAmexId = Guid.Parse("8f3b1c1a-2e3a-4b1a-9a7f-500000000004");
    private static readonly Guid RetiredTestCardId = Guid.Parse("8f3b1c1a-2e3a-4b1a-9a7f-500000000006");

    [Fact]
    public async Task GetCreditCards_ReturnsOk_IncludesInactiveCards()
    {
        await using var factory = new ApiTestFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/v1/financial/credit-cards");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var creditCards = await response.Content.ReadFromJsonAsync<List<CreditCardDTO>>();
        creditCards.Should().HaveCount(6);
        creditCards.Should().ContainSingle(c => c.Id == BaAmexId && c.IsActive);
        creditCards.Should().ContainSingle(c => c.Id == RetiredTestCardId && !c.IsActive);
    }

    [Fact]
    public async Task UpdateCreditCard_ExistingId_ReturnsOkWithUpdatedFields()
    {
        await using var factory = new ApiTestFactory();
        using var client = factory.CreateClient();
        var request = new CreditCardUpdateDTO
        {
            NextInvoiceDueDate = new DateOnly(2026, 9, 5),
            IsActive = false
        };

        var response = await client.PutAsJsonAsync($"/api/v1/financial/credit-cards/{BaAmexId}", request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var creditCard = await response.Content.ReadFromJsonAsync<CreditCardDTO>();
        creditCard!.NextInvoiceDueDate.Should().Be(new DateOnly(2026, 9, 5));
        creditCard.IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task UpdateCreditCard_UnknownId_ReturnsNotFound()
    {
        await using var factory = new ApiTestFactory();
        using var client = factory.CreateClient();
        var request = new CreditCardUpdateDTO
        {
            NextInvoiceDueDate = null,
            IsActive = true
        };

        var response = await client.PutAsJsonAsync($"/api/v1/financial/credit-cards/{Guid.NewGuid()}", request);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task UpdateCreditCard_InvalidDueDateFormat_ReturnsBadRequestWithFieldLevelError()
    {
        await using var factory = new ApiTestFactory();
        using var client = factory.CreateClient();
        var payload = new StringContent(
            """{ "nextInvoiceDueDate": "not-a-date", "isActive": true }""",
            System.Text.Encoding.UTF8,
            "application/json");

        var response = await client.PutAsync($"/api/v1/financial/credit-cards/{BaAmexId}", payload);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("nextInvoiceDueDate");
    }

    [Fact]
    public async Task UpdateCreditCard_ThenGetCreditCards_ReflectsTheUpdateImmediately()
    {
        await using var factory = new ApiTestFactory();
        using var client = factory.CreateClient();
        await client.PutAsJsonAsync($"/api/v1/financial/credit-cards/{RetiredTestCardId}", new CreditCardUpdateDTO
        {
            NextInvoiceDueDate = new DateOnly(2026, 10, 1),
            IsActive = true
        });

        var response = await client.GetAsync("/api/v1/financial/credit-cards");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var creditCards = await response.Content.ReadFromJsonAsync<List<CreditCardDTO>>();
        creditCards.Should().ContainSingle(c =>
            c.Id == RetiredTestCardId && c.IsActive && c.NextInvoiceDueDate == new DateOnly(2026, 10, 1));
    }
}
