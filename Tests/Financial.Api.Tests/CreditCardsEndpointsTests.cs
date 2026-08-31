using Financial.CashFlow.Application.DTOs;
using FluentAssertions;
using System.Net;
using System.Net.Http.Json;

namespace Financial.Api.Tests;

public class CreditCardsEndpointsTests : ApiEndpointTests
{
    private static readonly Guid BaAmexId = Guid.Parse("8f3b1c1a-2e3a-4b1a-9a7f-500000000004");
    private static readonly Guid RetiredTestCardId = Guid.Parse("8f3b1c1a-2e3a-4b1a-9a7f-500000000006");
    private static readonly Guid MercadoId = Guid.Parse("8f3b1c1a-2e3a-4b1a-9a7f-600000000008");

    [Fact]
    public async Task GetCreditCards_ReturnsOk_IncludesInactiveCards()
    {
        var response = await Client.GetAsync("/api/v1/financial/credit-cards");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var creditCards = await response.Content.ReadFromJsonAsync<List<CreditCardDTO>>();
        creditCards.Should().HaveCount(6);
        creditCards.Should().ContainSingle(c => c.Id == BaAmexId && c.IsActive);
        creditCards.Should().ContainSingle(c => c.Id == RetiredTestCardId && !c.IsActive);
    }

    [Fact]
    public async Task CreateCreditCard_ValidRequest_ReturnsOkWithNewCard()
    {
        var response = await Client.PostAsJsonAsync("/api/v1/financial/credit-cards", new CreditCardCreateDTO { Name = "Nubank", IsActive = true });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var creditCard = await response.Content.ReadFromJsonAsync<CreditCardDTO>();
        creditCard!.Name.Should().Be("Nubank");
        creditCard.IsActive.Should().BeTrue();
        creditCard.NextInvoiceDueDate.Should().BeNull();
        creditCard.HasReferences.Should().BeFalse();
        creditCard.Id.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public async Task CreateCreditCard_DuplicateName_ReturnsConflictWithMessage()
    {
        var response = await Client.PostAsJsonAsync("/api/v1/financial/credit-cards", new CreditCardCreateDTO { Name = "BaAmex", IsActive = true });

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("BaAmex").And.Contain("already exists");
    }

    [Fact]
    public async Task CreateCreditCard_BlankName_ReturnsBadRequest()
    {
        var response = await Client.PostAsJsonAsync("/api/v1/financial/credit-cards", new CreditCardCreateDTO { Name = "   ", IsActive = true });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task UpdateCreditCard_ExistingId_ReturnsOkWithUpdatedFields()
    {
        var request = new CreditCardUpdateDTO
        {
            Name = "BaAmex Renamed",
            NextInvoiceDueDate = new DateOnly(2026, 9, 5),
            IsActive = false
        };

        var response = await Client.PutAsJsonAsync($"/api/v1/financial/credit-cards/{BaAmexId}", request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var creditCard = await response.Content.ReadFromJsonAsync<CreditCardDTO>();
        creditCard!.Name.Should().Be("BaAmex Renamed");
        creditCard.NextInvoiceDueDate.Should().Be(new DateOnly(2026, 9, 5));
        creditCard.IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task UpdateCreditCard_UnknownId_ReturnsNotFound()
    {
        var request = new CreditCardUpdateDTO
        {
            Name = "X",
            NextInvoiceDueDate = null,
            IsActive = true
        };

        var response = await Client.PutAsJsonAsync($"/api/v1/financial/credit-cards/{Guid.NewGuid()}", request);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task UpdateCreditCard_NameCollidesWithAnotherCard_ReturnsConflict()
    {
        var response = await Client.PutAsJsonAsync($"/api/v1/financial/credit-cards/{RetiredTestCardId}", new CreditCardUpdateDTO
        {
            Name = "BaAmex",
            NextInvoiceDueDate = null,
            IsActive = true
        });

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task UpdateCreditCard_InvalidDueDateFormat_ReturnsBadRequestWithFieldLevelError()
    {
        var payload = new StringContent(
            """{ "name": "BaAmex", "nextInvoiceDueDate": "not-a-date", "isActive": true }""",
            System.Text.Encoding.UTF8,
            "application/json");

        var response = await Client.PutAsync($"/api/v1/financial/credit-cards/{BaAmexId}", payload);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("nextInvoiceDueDate");
    }

    [Fact]
    public async Task UpdateCreditCard_ThenGetCreditCards_ReflectsTheUpdateImmediately()
    {
        await Client.PutAsJsonAsync($"/api/v1/financial/credit-cards/{RetiredTestCardId}", new CreditCardUpdateDTO
        {
            Name = "RetiredTestCard",
            NextInvoiceDueDate = new DateOnly(2026, 10, 1),
            IsActive = true
        });

        var response = await Client.GetAsync("/api/v1/financial/credit-cards");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var creditCards = await response.Content.ReadFromJsonAsync<List<CreditCardDTO>>();
        creditCards.Should().ContainSingle(c =>
            c.Id == RetiredTestCardId && c.IsActive && c.NextInvoiceDueDate == new DateOnly(2026, 10, 1));
    }

    [Fact]
    public async Task DeleteCreditCard_WithNoReferences_ReturnsOkAndRemovesIt()
    {
        var created = await Client.PostAsJsonAsync("/api/v1/financial/credit-cards", new CreditCardCreateDTO { Name = "Nubank", IsActive = true });
        var creditCard = await created.Content.ReadFromJsonAsync<CreditCardDTO>();

        var response = await Client.DeleteAsync($"/api/v1/financial/credit-cards/{creditCard!.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var creditCards = await (await Client.GetAsync("/api/v1/financial/credit-cards")).Content.ReadFromJsonAsync<List<CreditCardDTO>>();
        creditCards.Should().NotContain(c => c.Id == creditCard.Id);
    }

    [Fact]
    public async Task DeleteCreditCard_UnknownId_ReturnsNotFound()
    {
        var response = await Client.DeleteAsync($"/api/v1/financial/credit-cards/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteCreditCard_ReferencedByExpense_ReturnsConflictWithMessage()
    {
        await Client.PostAsJsonAsync("/api/v1/financial/expenses", new ExpenseCreateDTO
        {
            Date = new DateOnly(2026, 7, 5),
            Description = "Groceries",
            Value = 50m,
            CategoryId = MercadoId,
            PaymentSourceBankId = null,
            CreditCardId = BaAmexId
        });

        var response = await Client.DeleteAsync($"/api/v1/financial/credit-cards/{BaAmexId}");

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("statement or expense");
    }

    [Fact]
    public async Task GetCreditCards_HasReferences_ReflectsWhetherAnExpenseExists()
    {
        var before = await (await Client.GetAsync("/api/v1/financial/credit-cards")).Content.ReadFromJsonAsync<List<CreditCardDTO>>();
        before.Should().ContainSingle(c => c.Id == BaAmexId && !c.HasReferences);

        await Client.PostAsJsonAsync("/api/v1/financial/expenses", new ExpenseCreateDTO
        {
            Date = new DateOnly(2026, 7, 5),
            Description = "Groceries",
            Value = 50m,
            CategoryId = MercadoId,
            PaymentSourceBankId = null,
            CreditCardId = BaAmexId
        });

        var after = await (await Client.GetAsync("/api/v1/financial/credit-cards")).Content.ReadFromJsonAsync<List<CreditCardDTO>>();
        after.Should().ContainSingle(c => c.Id == BaAmexId && c.HasReferences);
    }
}
