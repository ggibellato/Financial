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
}
