using Financial.CashFlow.Application.DTOs;
using FluentAssertions;
using System.Net;
using System.Net.Http.Json;

namespace Financial.Api.Tests;

[Trait("Category", "Integration")]
public class ExpenseEndpointsTests : ApiEndpointTests
{
    private static readonly Guid BarclaysId = Guid.Parse("8f3b1c1a-2e3a-4b1a-9a7f-100000000001");
    private static readonly Guid Trading212Id = Guid.Parse("8f3b1c1a-2e3a-4b1a-9a7f-100000000002");
    private static readonly Guid ChaseId = Guid.Parse("8f3b1c1a-2e3a-4b1a-9a7f-100000000003");
    private static readonly Guid CasaId = Guid.Parse("8f3b1c1a-2e3a-4b1a-9a7f-600000000003");
    private static readonly Guid ExtrasId = Guid.Parse("8f3b1c1a-2e3a-4b1a-9a7f-600000000005");
    private static readonly Guid MercadoId = Guid.Parse("8f3b1c1a-2e3a-4b1a-9a7f-600000000008");
    private static readonly Guid DizimoId = Guid.Parse("8f3b1c1a-2e3a-4b1a-9a7f-600000000012");

    [Fact]
    public async Task AddExpense_ValidRequest_ReturnsOk()
    {
        var request = new ExpenseCreateDTO
        {
            Date = new DateOnly(2026, 7, 15),
            Description = "Weekly groceries",
            Value = 54.32m,
            CategoryId = MercadoId,
            PaymentSourceBankId = BarclaysId,
            CreditCardId = null
        };

        var response = await Client.PostAsJsonAsync("/api/v1/financial/expenses", request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var expense = await response.Content.ReadFromJsonAsync<ExpenseDTO>();
        expense.Should().NotBeNull();
        expense!.Description.Should().Be("Weekly groceries");
        expense.CategoryName.Should().Be("Mercado");
        expense.PaymentStatus.Should().Be("ImmediatePayment");
    }

    [Fact]
    public async Task AddExpense_OmittingCountsAsTithe_ReturnsOkWithFlagTrue()
    {
        var request = new ExpenseCreateDTO
        {
            Date = new DateOnly(2026, 7, 15),
            Description = "Tithe payment",
            Value = 200m,
            CategoryId = DizimoId,
            PaymentSourceBankId = BarclaysId,
            CreditCardId = null
        };

        var response = await Client.PostAsJsonAsync("/api/v1/financial/expenses", request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var expense = await response.Content.ReadFromJsonAsync<ExpenseDTO>();
        expense!.CountsAsTithe.Should().BeTrue();
    }

    [Fact]
    public async Task UpdateExpense_TogglingCountsAsTitheToFalse_ReturnsOkWithFlagFalse()
    {
        var created = await Client.PostAsJsonAsync("/api/v1/financial/expenses", new ExpenseCreateDTO
        {
            Date = new DateOnly(2026, 7, 15),
            Description = "Tithe payment",
            Value = 200m,
            CategoryId = DizimoId,
            PaymentSourceBankId = BarclaysId,
            CreditCardId = null
        });
        var createdExpense = await created.Content.ReadFromJsonAsync<ExpenseDTO>();

        var response = await Client.PutAsJsonAsync($"/api/v1/financial/expenses/{createdExpense!.Id}", new ExpenseUpdateDTO
        {
            Date = createdExpense.Date,
            Description = createdExpense.Description,
            Value = createdExpense.Value,
            CategoryId = createdExpense.CategoryId,
            PaymentSourceBankId = createdExpense.PaymentSourceBankId,
            CreditCardId = createdExpense.CreditCardId,
            CountsAsTithe = false
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var updated = await response.Content.ReadFromJsonAsync<ExpenseDTO>();
        updated!.CountsAsTithe.Should().BeFalse();
    }

    [Fact]
    public async Task AddExpense_NeitherPaymentSourceNorCreditCardId_ReturnsBadRequest()
    {
        var request = new ExpenseCreateDTO
        {
            Date = new DateOnly(2026, 7, 15),
            Description = "No payment shape",
            Value = 30m,
            CategoryId = ExtrasId,
            PaymentSourceBankId = null,
            CreditCardId = null
        };

        var response = await Client.PostAsJsonAsync("/api/v1/financial/expenses", request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("payment source or a card tag");
    }

    [Fact]
    public async Task UpdateExpense_ExistingId_ReturnsOkAndUpdatesFields()
    {
        var created = await Client.PostAsJsonAsync("/api/v1/financial/expenses", new ExpenseCreateDTO
        {
            Date = new DateOnly(2026, 7, 1),
            Description = "Original",
            Value = 10m,
            CategoryId = CasaId,
            PaymentSourceBankId = ChaseId,
            CreditCardId = null
        });
        var createdExpense = await created.Content.ReadFromJsonAsync<ExpenseDTO>();

        var response = await Client.PutAsJsonAsync($"/api/v1/financial/expenses/{createdExpense!.Id}", new ExpenseUpdateDTO
        {
            Date = new DateOnly(2026, 8, 1),
            Description = "Updated",
            Value = 20m,
            CategoryId = MercadoId,
            PaymentSourceBankId = BarclaysId,
            CreditCardId = null
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var updated = await response.Content.ReadFromJsonAsync<ExpenseDTO>();
        updated!.Description.Should().Be("Updated");
        updated.CategoryName.Should().Be("Mercado");
    }

    [Fact]
    public async Task UpdateExpense_UnknownId_ReturnsNotFound()
    {
        var response = await Client.PutAsJsonAsync($"/api/v1/financial/expenses/{Guid.NewGuid()}", new ExpenseUpdateDTO
        {
            Date = new DateOnly(2026, 7, 1),
            Description = "Ghost",
            Value = 10m,
            CategoryId = CasaId,
            PaymentSourceBankId = ChaseId,
            CreditCardId = null
        });

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteExpense_ExistingId_ReturnsOkAndRemovesExpense()
    {
        var created = await Client.PostAsJsonAsync("/api/v1/financial/expenses", new ExpenseCreateDTO
        {
            Date = new DateOnly(2026, 7, 5),
            Description = "To delete",
            Value = 10m,
            CategoryId = CasaId,
            PaymentSourceBankId = ChaseId,
            CreditCardId = null
        });
        var createdExpense = await created.Content.ReadFromJsonAsync<ExpenseDTO>();

        var response = await Client.DeleteAsync($"/api/v1/financial/expenses/{createdExpense!.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var rows = await Client.GetFromJsonAsync<List<AccountTransactionDTO>>($"/api/v1/financial/accounts/{ChaseId}/transactions/2026/7");
        rows.Should().NotContain(row => row.Id == createdExpense.Id);
    }

    [Fact]
    public async Task DeleteExpense_UnknownId_ReturnsNotFound()
    {
        var response = await Client.DeleteAsync($"/api/v1/financial/expenses/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetCategoryTotalsByMonth_ReturnsSummedTotalsPerCategory()
    {
        await Client.PostAsJsonAsync("/api/v1/financial/expenses", new ExpenseCreateDTO
        {
            Date = new DateOnly(2026, 7, 1),
            Description = "Groceries 1",
            Value = 10m,
            CategoryId = MercadoId,
            PaymentSourceBankId = BarclaysId,
            CreditCardId = null
        });
        await Client.PostAsJsonAsync("/api/v1/financial/expenses", new ExpenseCreateDTO
        {
            Date = new DateOnly(2026, 7, 2),
            Description = "Groceries 2",
            Value = 5m,
            CategoryId = MercadoId,
            PaymentSourceBankId = BarclaysId,
            CreditCardId = null
        });

        var response = await Client.GetAsync("/api/v1/financial/expenses/month/2026/7/category-totals");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var totals = await response.Content.ReadFromJsonAsync<List<CategoryTotalDTO>>();
        totals.Should().ContainSingle(t => t.Category == "Mercado" && t.TotalValue == 15m);
    }

    [Fact]
    public async Task GetAccountTransactions_EligibleExpenseWithNoSavedRoundUp_IncludesSuggestion()
    {
        await Client.PostAsJsonAsync("/api/v1/financial/expenses", new ExpenseCreateDTO
        {
            Date = new DateOnly(2026, 7, 15),
            Description = "TfL",
            Value = 9.40m,
            CategoryId = ExtrasId,
            PaymentSourceBankId = Trading212Id,
            CreditCardId = null
        });

        var response = await Client.GetAsync($"/api/v1/financial/accounts/{Trading212Id}/transactions/2026/7");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var rows = await response.Content.ReadFromJsonAsync<List<AccountTransactionDTO>>();
        rows.Should().ContainSingle(row => row.Description == "TfL" && row.Expense!.SuggestedRoundUpAmount == 0.60m);
    }
}
