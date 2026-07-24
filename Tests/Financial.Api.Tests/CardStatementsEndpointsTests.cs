using Financial.CashFlow.Application.DTOs;
using FluentAssertions;
using System.Net;
using System.Net.Http.Json;

namespace Financial.Api.Tests;

public class CardStatementsEndpointsTests
{
    [Fact]
    public async Task GetStatementsForMonth_FirstCall_GeneratesFiveUnpaidStatements()
    {
        await using var factory = new ApiTestFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/v1/financial/card-statements/2026/7");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var statements = await response.Content.ReadFromJsonAsync<List<CardStatementDTO>>();
        statements.Should().HaveCount(5);
        statements.Should().OnlyContain(s => !s.IsPaid && s.OutstandingTotal == 0m);
    }

    [Fact]
    public async Task GetStatementsForMonth_ReflectsTaggedExpenseValue()
    {
        await using var factory = new ApiTestFactory();
        using var client = factory.CreateClient();
        await client.PostAsJsonAsync("/api/v1/financial/expenses", new ExpenseCreateDTO
        {
            Date = new DateOnly(2026, 7, 10),
            Description = "Card charge",
            Value = 45m,
            Category = "Mercado",
            PaymentSource = null,
            CardTag = "BarclaysPlatinumVisa8003"
        });

        var response = await client.GetAsync("/api/v1/financial/card-statements/2026/7");

        var statements = await response.Content.ReadFromJsonAsync<List<CardStatementDTO>>();
        statements.Should().ContainSingle(s => s.Card == "BarclaysPlatinumVisa8003" && s.OutstandingTotal == 45m);
    }

    [Fact]
    public async Task MarkStatementPaid_ExistingId_ReturnsOkAndZeroesOutstandingTotal()
    {
        await using var factory = new ApiTestFactory();
        using var client = factory.CreateClient();
        await client.PostAsJsonAsync("/api/v1/financial/expenses", new ExpenseCreateDTO
        {
            Date = new DateOnly(2026, 7, 10),
            Description = "Card charge",
            Value = 45m,
            Category = "Mercado",
            PaymentSource = null,
            CardTag = "BarclaysPlatinumVisa8003"
        });
        var monthResponse = await client.GetAsync("/api/v1/financial/card-statements/2026/7");
        var statements = await monthResponse.Content.ReadFromJsonAsync<List<CardStatementDTO>>();
        var target = statements!.First(s => s.Card == "BarclaysPlatinumVisa8003");

        var response = await client.PostAsync($"/api/v1/financial/card-statements/{target.Id}/mark-paid", null);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var updated = await response.Content.ReadFromJsonAsync<CardStatementDTO>();
        updated!.IsPaid.Should().BeTrue();
        updated.OutstandingTotal.Should().Be(0m);
    }

    [Fact]
    public async Task MarkStatementPaid_CalledAgain_StillReturnsOk()
    {
        await using var factory = new ApiTestFactory();
        using var client = factory.CreateClient();
        var monthResponse = await client.GetAsync("/api/v1/financial/card-statements/2026/7");
        var statements = await monthResponse.Content.ReadFromJsonAsync<List<CardStatementDTO>>();
        var target = statements!.First();
        await client.PostAsync($"/api/v1/financial/card-statements/{target.Id}/mark-paid", null);

        var response = await client.PostAsync($"/api/v1/financial/card-statements/{target.Id}/mark-paid", null);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task MarkStatementPaid_UnknownId_ReturnsNotFound()
    {
        await using var factory = new ApiTestFactory();
        using var client = factory.CreateClient();

        var response = await client.PostAsync($"/api/v1/financial/card-statements/{Guid.NewGuid()}/mark-paid", null);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
