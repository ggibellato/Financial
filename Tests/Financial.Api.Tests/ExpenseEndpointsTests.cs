using Financial.CashFlow.Application.DTOs;
using FluentAssertions;
using System.Net;
using System.Net.Http.Json;

namespace Financial.Api.Tests;

public class ExpenseEndpointsTests
{
    [Fact]
    public async Task AddExpense_ValidRequest_ReturnsOk()
    {
        await using var factory = new ApiTestFactory();
        using var client = factory.CreateClient();
        var request = new ExpenseCreateDTO
        {
            Date = new DateOnly(2026, 7, 15),
            Description = "Weekly groceries",
            Value = 54.32m,
            Category = "Mercado",
            PaymentSource = "Barclays",
            CardTag = null
        };

        var response = await client.PostAsJsonAsync("/api/v1/financial/expenses", request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var expense = await response.Content.ReadFromJsonAsync<ExpenseDTO>();
        expense.Should().NotBeNull();
        expense!.Description.Should().Be("Weekly groceries");
        expense.Category.Should().Be("Mercado");
    }

    [Fact]
    public async Task AddExpense_ZeroValue_ReturnsBadRequestWithMessage()
    {
        await using var factory = new ApiTestFactory();
        using var client = factory.CreateClient();
        var request = new ExpenseCreateDTO
        {
            Date = new DateOnly(2026, 7, 15),
            Description = "Zero value expense",
            Value = 0m,
            Category = "Mercado",
            PaymentSource = "Barclays",
            CardTag = null
        };

        var response = await client.PostAsJsonAsync("/api/v1/financial/expenses", request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("zero");
    }

    [Fact]
    public async Task AddExpense_MissingCategory_ReturnsBadRequestWithMessage()
    {
        await using var factory = new ApiTestFactory();
        using var client = factory.CreateClient();
        var request = new ExpenseCreateDTO
        {
            Date = new DateOnly(2026, 7, 15),
            Description = "Bad category",
            Value = 10m,
            Category = "NotACategory",
            PaymentSource = "Barclays",
            CardTag = null
        };

        var response = await client.PostAsJsonAsync("/api/v1/financial/expenses", request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("Category");
    }

    [Fact]
    public async Task UpdateExpense_ExistingId_ReturnsOkAndUpdatesFields()
    {
        await using var factory = new ApiTestFactory();
        using var client = factory.CreateClient();
        var created = await client.PostAsJsonAsync("/api/v1/financial/expenses", new ExpenseCreateDTO
        {
            Date = new DateOnly(2026, 7, 1),
            Description = "Original",
            Value = 10m,
            Category = "Casa",
            PaymentSource = "Chase",
            CardTag = null
        });
        var createdExpense = await created.Content.ReadFromJsonAsync<ExpenseDTO>();

        var response = await client.PutAsJsonAsync($"/api/v1/financial/expenses/{createdExpense!.Id}", new ExpenseUpdateDTO
        {
            Date = new DateOnly(2026, 8, 1),
            Description = "Updated",
            Value = 20m,
            Category = "Mercado",
            PaymentSource = "Barclays",
            CardTag = null
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var updated = await response.Content.ReadFromJsonAsync<ExpenseDTO>();
        updated!.Description.Should().Be("Updated");
        updated.Category.Should().Be("Mercado");
    }

    [Fact]
    public async Task UpdateExpense_UnknownId_ReturnsNotFound()
    {
        await using var factory = new ApiTestFactory();
        using var client = factory.CreateClient();

        var response = await client.PutAsJsonAsync($"/api/v1/financial/expenses/{Guid.NewGuid()}", new ExpenseUpdateDTO
        {
            Date = new DateOnly(2026, 7, 1),
            Description = "Ghost",
            Value = 10m,
            Category = "Casa",
            PaymentSource = "Chase",
            CardTag = null
        });

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteExpense_ExistingId_ReturnsOkAndRemovesExpense()
    {
        await using var factory = new ApiTestFactory();
        using var client = factory.CreateClient();
        var created = await client.PostAsJsonAsync("/api/v1/financial/expenses", new ExpenseCreateDTO
        {
            Date = new DateOnly(2026, 7, 5),
            Description = "To delete",
            Value = 10m,
            Category = "Casa",
            PaymentSource = "Chase",
            CardTag = null
        });
        var createdExpense = await created.Content.ReadFromJsonAsync<ExpenseDTO>();

        var response = await client.DeleteAsync($"/api/v1/financial/expenses/{createdExpense!.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var list = await client.GetFromJsonAsync<List<ExpenseDTO>>("/api/v1/financial/expenses/month/2026/7");
        list.Should().NotContain(e => e.Id == createdExpense.Id);
    }

    [Fact]
    public async Task DeleteExpense_UnknownId_ReturnsNotFound()
    {
        await using var factory = new ApiTestFactory();
        using var client = factory.CreateClient();

        var response = await client.DeleteAsync($"/api/v1/financial/expenses/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetExpensesByMonth_ReturnsOnlyExpensesForThatMonth()
    {
        await using var factory = new ApiTestFactory();
        using var client = factory.CreateClient();
        await client.PostAsJsonAsync("/api/v1/financial/expenses", new ExpenseCreateDTO
        {
            Date = new DateOnly(2026, 7, 10),
            Description = "July expense",
            Value = 10m,
            Category = "Casa",
            PaymentSource = "Chase",
            CardTag = null
        });
        await client.PostAsJsonAsync("/api/v1/financial/expenses", new ExpenseCreateDTO
        {
            Date = new DateOnly(2026, 8, 10),
            Description = "August expense",
            Value = 10m,
            Category = "Casa",
            PaymentSource = "Chase",
            CardTag = null
        });

        var response = await client.GetAsync("/api/v1/financial/expenses/month/2026/7");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var items = await response.Content.ReadFromJsonAsync<List<ExpenseDTO>>();
        items.Should().ContainSingle(e => e.Description == "July expense");
    }

    [Fact]
    public async Task GetCategoryTotalsByMonth_ReturnsSummedTotalsPerCategory()
    {
        await using var factory = new ApiTestFactory();
        using var client = factory.CreateClient();
        await client.PostAsJsonAsync("/api/v1/financial/expenses", new ExpenseCreateDTO
        {
            Date = new DateOnly(2026, 7, 1),
            Description = "Groceries 1",
            Value = 10m,
            Category = "Mercado",
            PaymentSource = "Barclays",
            CardTag = null
        });
        await client.PostAsJsonAsync("/api/v1/financial/expenses", new ExpenseCreateDTO
        {
            Date = new DateOnly(2026, 7, 2),
            Description = "Groceries 2",
            Value = 5m,
            Category = "Mercado",
            PaymentSource = "Barclays",
            CardTag = null
        });

        var response = await client.GetAsync("/api/v1/financial/expenses/month/2026/7/category-totals");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var totals = await response.Content.ReadFromJsonAsync<List<CategoryTotalDTO>>();
        totals.Should().ContainSingle(t => t.Category == "Mercado" && t.TotalValue == 15m);
    }
}
