using Financial.CashFlow.Application.DTOs;
using FluentAssertions;
using System.Net;
using System.Net.Http.Json;

namespace Financial.Api.Tests;

public class CardStatementsEndpointsTests : ApiEndpointTests
{
    private static readonly Guid BarclaysId = Guid.Parse("8f3b1c1a-2e3a-4b1a-9a7f-100000000001");
    private static readonly Guid Trading212Id = Guid.Parse("8f3b1c1a-2e3a-4b1a-9a7f-100000000002");
    private static readonly Guid BarclaysPlatinumVisa8003Id = Guid.Parse("8f3b1c1a-2e3a-4b1a-9a7f-500000000001");
    private static readonly Guid MercadoId = Guid.Parse("8f3b1c1a-2e3a-4b1a-9a7f-600000000008");

    [Fact]
    public async Task GetStatementsForMonth_FirstCall_GeneratesFiveUnpaidStatements()
    {
        var response = await Client.GetAsync("/api/v1/financial/card-statements/2026/7");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var statements = await response.Content.ReadFromJsonAsync<List<CardStatementDTO>>();
        statements.Should().HaveCount(5);
        statements.Should().OnlyContain(s => !s.IsPaid && s.OutstandingTotal == 0m);
    }

    [Fact]
    public async Task GetStatementsForMonth_ReflectsTaggedExpenseValue()
    {
        await Client.PostAsJsonAsync("/api/v1/financial/expenses", new ExpenseCreateDTO
        {
            Date = new DateOnly(2026, 7, 10),
            Description = "Card charge",
            Value = 45m,
            CategoryId = MercadoId,
            PaymentSourceBankId = null,
            CreditCardId = BarclaysPlatinumVisa8003Id
        });

        var response = await Client.GetAsync("/api/v1/financial/card-statements/2026/7");

        var statements = await response.Content.ReadFromJsonAsync<List<CardStatementDTO>>();
        statements.Should().ContainSingle(s => s.CreditCardName == "BarclaysPlatinumVisa8003" && s.OutstandingTotal == 45m);
    }

    [Fact]
    public async Task MarkStatementPaid_WithPaymentSource_SettlesChargesAndZeroesOutstandingTotal()
    {
        await Client.PostAsJsonAsync("/api/v1/financial/expenses", new ExpenseCreateDTO
        {
            Date = new DateOnly(2026, 7, 10),
            Description = "Card charge",
            Value = 45m,
            CategoryId = MercadoId,
            PaymentSourceBankId = null,
            CreditCardId = BarclaysPlatinumVisa8003Id
        });
        var target = await GetStatementAsync(Client, "BarclaysPlatinumVisa8003");

        var response = await Client.PostAsJsonAsync(
            $"/api/v1/financial/card-statements/{target.Id}/mark-paid",
            new MarkCardStatementPaidDTO { PaymentSourceBankId = Trading212Id });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var updated = await response.Content.ReadFromJsonAsync<CardStatementDTO>();
        updated!.IsPaid.Should().BeTrue();
        updated.OutstandingTotal.Should().Be(0m);

        var expenses = await Client.GetFromJsonAsync<List<ExpenseDTO>>("/api/v1/financial/expenses/month/2026/7");
        var settled = expenses!.Single(e => e.Description == "Card charge");
        settled.PaymentStatus.Should().Be("CreditCardSettled");
        settled.PaymentSourceBankId.Should().Be(Trading212Id);
        settled.PaymentSourceBankName.Should().Be("Trading212");
        settled.Date.Should().Be(DateOnly.FromDateTime(DateTime.Today));
    }

    [Fact]
    public async Task MarkStatementPaid_WithoutPaymentSource_ReturnsBadRequestWithoutChangingExpenses()
    {
        await Client.PostAsJsonAsync("/api/v1/financial/expenses", new ExpenseCreateDTO
        {
            Date = new DateOnly(2026, 7, 10),
            Description = "Card charge",
            Value = 45m,
            CategoryId = MercadoId,
            PaymentSourceBankId = null,
            CreditCardId = BarclaysPlatinumVisa8003Id
        });
        var target = await GetStatementAsync(Client, "BarclaysPlatinumVisa8003");

        var response = await Client.PostAsJsonAsync(
            $"/api/v1/financial/card-statements/{target.Id}/mark-paid",
            new MarkCardStatementPaidDTO { PaymentSourceBankId = null });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var unchanged = await GetStatementAsync(Client, "BarclaysPlatinumVisa8003");
        unchanged.IsPaid.Should().BeFalse();
        unchanged.OutstandingTotal.Should().Be(45m);
    }

    [Fact]
    public async Task MarkStatementPaid_CalledAgain_StillReturnsOk()
    {
        var target = await GetFirstStatementAsync(Client);
        await MarkPaidAsync(Client, target.Id, BarclaysId);

        var response = await Client.PostAsJsonAsync(
            $"/api/v1/financial/card-statements/{target.Id}/mark-paid",
            new MarkCardStatementPaidDTO { PaymentSourceBankId = BarclaysId });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task MarkStatementPaid_UnknownId_ReturnsNotFound()
    {
        var response = await Client.PostAsJsonAsync(
            $"/api/v1/financial/card-statements/{Guid.NewGuid()}/mark-paid",
            new MarkCardStatementPaidDTO { PaymentSourceBankId = BarclaysId });

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task UnmarkStatementPaid_RevertsSettledChargesToUnsettled()
    {
        await Client.PostAsJsonAsync("/api/v1/financial/expenses", new ExpenseCreateDTO
        {
            Date = new DateOnly(2026, 7, 10),
            Description = "Card charge",
            Value = 45m,
            CategoryId = MercadoId,
            PaymentSourceBankId = null,
            CreditCardId = BarclaysPlatinumVisa8003Id
        });
        var target = await GetStatementAsync(Client, "BarclaysPlatinumVisa8003");
        await MarkPaidAsync(Client, target.Id, Trading212Id);

        var response = await Client.PostAsync($"/api/v1/financial/card-statements/{target.Id}/unmark-paid", null);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var updated = await response.Content.ReadFromJsonAsync<CardStatementDTO>();
        updated!.IsPaid.Should().BeFalse();
        updated.OutstandingTotal.Should().Be(45m);

        var reExpenses = await Client.GetFromJsonAsync<List<ExpenseDTO>>("/api/v1/financial/expenses/month/2026/7");
        reExpenses.Should().NotContain(e => e.Description == "Card charge");
    }

    [Fact]
    public async Task UnmarkStatementPaid_OnUnpaidStatement_StillReturnsOk()
    {
        var target = await GetFirstStatementAsync(Client);

        var response = await Client.PostAsync($"/api/v1/financial/card-statements/{target.Id}/unmark-paid", null);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var updated = await response.Content.ReadFromJsonAsync<CardStatementDTO>();
        updated!.IsPaid.Should().BeFalse();
    }

    [Fact]
    public async Task UnmarkStatementPaid_UnknownId_ReturnsNotFound()
    {
        var response = await Client.PostAsync($"/api/v1/financial/card-statements/{Guid.NewGuid()}/unmark-paid", null);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    private static async Task<CardStatementDTO> GetStatementAsync(HttpClient client, string card)
    {
        var statements = await client.GetFromJsonAsync<List<CardStatementDTO>>("/api/v1/financial/card-statements/2026/7");
        return statements!.First(s => s.CreditCardName == card);
    }

    private static async Task<CardStatementDTO> GetFirstStatementAsync(HttpClient client)
    {
        var statements = await client.GetFromJsonAsync<List<CardStatementDTO>>("/api/v1/financial/card-statements/2026/7");
        return statements!.First();
    }

    private static Task<HttpResponseMessage> MarkPaidAsync(HttpClient client, Guid id, Guid paymentSourceBankId) =>
        client.PostAsJsonAsync(
            $"/api/v1/financial/card-statements/{id}/mark-paid",
            new MarkCardStatementPaidDTO { PaymentSourceBankId = paymentSourceBankId });
}
