using Financial.CashFlow.Application.DTOs;
using FluentAssertions;
using FluentAssertions.Execution;
using System.Net;
using System.Net.Http.Json;

namespace Financial.Api.Tests;

public class CategoriesEndpointsTests : ApiEndpointTests
{
    private static readonly Guid MercadoId = Guid.Parse("8f3b1c1a-2e3a-4b1a-9a7f-600000000008");
    private static readonly Guid DizimoId = Guid.Parse("8f3b1c1a-2e3a-4b1a-9a7f-600000000012");
    private static readonly Guid InvestimentoId = Guid.Parse("8f3b1c1a-2e3a-4b1a-9a7f-600000000013");

    [Fact]
    public async Task GetCategories_ReturnsAllFourteenSeededCategories()
    {
        var response = await Client.GetAsync("/api/v1/financial/categories");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var categories = await response.Content.ReadFromJsonAsync<List<CategoryDTO>>();
        categories.Should().HaveCount(14);
        categories.Should().OnlyContain(c => c.Id != Guid.Empty);
    }

    [Fact]
    public async Task GetCategories_OnlyInvestimentoHasIsInvestmentTrueAndOnlyDizimoHasIsTitheTrue()
    {
        var response = await Client.GetAsync("/api/v1/financial/categories");

        var categories = await response.Content.ReadFromJsonAsync<List<CategoryDTO>>();
        using (new AssertionScope())
        {
            categories.Should().ContainSingle(c => c.IsInvestment).Which.Id.Should().Be(InvestimentoId);
            categories.Should().ContainSingle(c => c.IsTithe).Which.Id.Should().Be(DizimoId);
        }
    }

    [Fact]
    public async Task GetCategories_MercadoIsActiveWithNeitherClassificationFlag()
    {
        var response = await Client.GetAsync("/api/v1/financial/categories");

        var categories = await response.Content.ReadFromJsonAsync<List<CategoryDTO>>();
        var mercado = categories.Should().ContainSingle(c => c.Id == MercadoId).Which;
        using (new AssertionScope())
        {
            mercado.Name.Should().Be("Mercado");
            mercado.Active.Should().BeTrue();
            mercado.IsInvestment.Should().BeFalse();
            mercado.IsTithe.Should().BeFalse();
        }
    }

    [Fact]
    public async Task CreateCategory_ValidRequest_ReturnsOkWithNewCategory()
    {
        var response = await Client.PostAsJsonAsync("/api/v1/financial/categories", new CategoryCreateDTO
        {
            Name = "Lazer",
            Active = true,
            IsInvestment = false,
            IsTithe = false
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var category = await response.Content.ReadFromJsonAsync<CategoryDTO>();
        category!.Name.Should().Be("Lazer");
        category.Active.Should().BeTrue();
        category.Id.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public async Task CreateCategory_DuplicateName_ReturnsConflictWithMessage()
    {
        var response = await Client.PostAsJsonAsync("/api/v1/financial/categories", new CategoryCreateDTO
        {
            Name = "Mercado",
            Active = true,
            IsInvestment = false,
            IsTithe = false
        });

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("Mercado").And.Contain("already exists");
    }

    [Fact]
    public async Task CreateCategory_BlankName_ReturnsBadRequest()
    {
        var response = await Client.PostAsJsonAsync("/api/v1/financial/categories", new CategoryCreateDTO
        {
            Name = "   ",
            Active = true,
            IsInvestment = false,
            IsTithe = false
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task UpdateCategory_ValidRequest_ReturnsOkAndUpdatesFields()
    {
        var response = await Client.PutAsJsonAsync($"/api/v1/financial/categories/{MercadoId}", new CategoryUpdateDTO
        {
            Name = "Mercado Renamed",
            Active = false,
            IsInvestment = true,
            IsTithe = true
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var category = await response.Content.ReadFromJsonAsync<CategoryDTO>();
        category!.Name.Should().Be("Mercado Renamed");
        category.Active.Should().BeFalse();
        category.IsInvestment.Should().BeTrue();
        category.IsTithe.Should().BeTrue();
    }

    [Fact]
    public async Task UpdateCategory_UnknownId_ReturnsNotFound()
    {
        var response = await Client.PutAsJsonAsync($"/api/v1/financial/categories/{Guid.NewGuid()}", new CategoryUpdateDTO
        {
            Name = "X",
            Active = true,
            IsInvestment = false,
            IsTithe = false
        });

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task UpdateCategory_NameCollidesWithAnotherCategory_ReturnsConflict()
    {
        var response = await Client.PutAsJsonAsync($"/api/v1/financial/categories/{MercadoId}", new CategoryUpdateDTO
        {
            Name = "Dizimo",
            Active = true,
            IsInvestment = false,
            IsTithe = false
        });

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task DeleteCategory_WithNoReferences_ReturnsOkAndRemovesIt()
    {
        var created = await Client.PostAsJsonAsync("/api/v1/financial/categories", new CategoryCreateDTO
        {
            Name = "Lazer",
            Active = true,
            IsInvestment = false,
            IsTithe = false
        });
        var category = await created.Content.ReadFromJsonAsync<CategoryDTO>();

        var response = await Client.DeleteAsync($"/api/v1/financial/categories/{category!.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var categories = await (await Client.GetAsync("/api/v1/financial/categories")).Content.ReadFromJsonAsync<List<CategoryDTO>>();
        categories.Should().NotContain(c => c.Id == category.Id);
    }

    [Fact]
    public async Task DeleteCategory_UnknownId_ReturnsNotFound()
    {
        var response = await Client.DeleteAsync($"/api/v1/financial/categories/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteCategory_ReferencedByExpense_ReturnsConflictWithMessage()
    {
        await Client.PostAsJsonAsync("/api/v1/financial/expenses", new ExpenseCreateDTO
        {
            Date = new DateOnly(2026, 7, 5),
            Description = "Groceries",
            Value = 50m,
            CategoryId = MercadoId,
            PaymentSourceBankId = Guid.Parse("8f3b1c1a-2e3a-4b1a-9a7f-100000000001"),
            CreditCardId = null
        });

        var response = await Client.DeleteAsync($"/api/v1/financial/categories/{MercadoId}");

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("still used by a transaction");
    }

    [Fact]
    public async Task GetCategories_HasReferences_ReflectsWhetherAnExpenseExists()
    {
        var before = await (await Client.GetAsync("/api/v1/financial/categories")).Content.ReadFromJsonAsync<List<CategoryDTO>>();
        before.Should().ContainSingle(c => c.Id == MercadoId && !c.HasReferences);

        await Client.PostAsJsonAsync("/api/v1/financial/expenses", new ExpenseCreateDTO
        {
            Date = new DateOnly(2026, 7, 5),
            Description = "Groceries",
            Value = 50m,
            CategoryId = MercadoId,
            PaymentSourceBankId = Guid.Parse("8f3b1c1a-2e3a-4b1a-9a7f-100000000001"),
            CreditCardId = null
        });

        var after = await (await Client.GetAsync("/api/v1/financial/categories")).Content.ReadFromJsonAsync<List<CategoryDTO>>();
        after.Should().ContainSingle(c => c.Id == MercadoId && c.HasReferences);
    }
}
