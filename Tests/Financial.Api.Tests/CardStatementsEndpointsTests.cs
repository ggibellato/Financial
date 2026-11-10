using Financial.CashFlow.Application.DTOs;
using FluentAssertions;
using System.Net;
using System.Net.Http.Json;

namespace Financial.Api.Tests;

[Trait("Category", "Integration")]
public class CardStatementsEndpointsTests : ApiEndpointTests
{
    private static readonly Guid BarclaysId = Guid.Parse("8f3b1c1a-2e3a-4b1a-9a7f-100000000001");
    private static readonly Guid Trading212Id = Guid.Parse("8f3b1c1a-2e3a-4b1a-9a7f-100000000002");
    private static readonly Guid BarclaysPlatinumVisa8003Id = Guid.Parse("8f3b1c1a-2e3a-4b1a-9a7f-500000000001");
    private static readonly Guid MercadoId = Guid.Parse("8f3b1c1a-2e3a-4b1a-9a7f-600000000008");

    [Fact]
    public async Task MarkStatementPaid_WithPaymentSource_ZeroesOutstandingTotalAndListsTheChargeAsStatementPaid()
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
        var target = await GetVisaStatementAsync();

        var response = await Client.PostAsJsonAsync(
            $"/api/v1/financial/card-statements/{target.Id}/mark-paid",
            new MarkCardStatementPaidDTO { PaymentSourceBankId = Trading212Id });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var updated = await response.Content.ReadFromJsonAsync<CardStatementDTO>();
        updated!.IsPaid.Should().BeTrue();
        updated.OutstandingTotal.Should().Be(0m);

        var paid = (await GetVisaChargeAsync()).Expense!;
        paid.PaymentStatus.Should().Be("CreditCardStatementPaid");
        paid.PaymentSourceBankId.Should().BeNull();
        paid.Date.Should().Be(new DateOnly(2026, 7, 10));
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
        var target = await GetVisaStatementAsync();

        var response = await Client.PostAsJsonAsync(
            $"/api/v1/financial/card-statements/{target.Id}/mark-paid",
            new MarkCardStatementPaidDTO { PaymentSourceBankId = null });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var unchanged = await GetVisaStatementAsync();
        unchanged.IsPaid.Should().BeFalse();
        unchanged.OutstandingTotal.Should().Be(45m);
    }

    [Fact]
    public async Task MarkStatementPaid_CalledAgain_ReturnsConflict()
    {
        await AddChargeAsync(45m);
        var target = await GetVisaStatementAsync();
        await MarkPaidAsync(Client, target.Id, BarclaysId);

        var response = await MarkPaidAsync(Client, target.Id, BarclaysId);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
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
        var target = await GetVisaStatementAsync();
        await MarkPaidAsync(Client, target.Id, Trading212Id);

        var response = await Client.PostAsync($"/api/v1/financial/card-statements/{target.Id}/unmark-paid", null);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var updated = await response.Content.ReadFromJsonAsync<CardStatementDTO>();
        updated!.IsPaid.Should().BeFalse();
        updated.OutstandingTotal.Should().Be(45m);

        (await GetVisaChargeAsync()).Expense!.PaymentStatus.Should().Be("CreditCardCharge");
    }

    [Fact]
    public async Task UnmarkStatementPaid_OnUnpaidStatement_ReturnsConflict()
    {
        await AddChargeAsync(45m);
        var target = await GetVisaStatementAsync();

        var response = await Client.PostAsync($"/api/v1/financial/card-statements/{target.Id}/unmark-paid", null);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await GetVisaStatementAsync()).IsPaid.Should().BeFalse();
    }

    [Fact]
    public async Task UnmarkStatementPaid_UnknownId_ReturnsNotFound()
    {
        var response = await Client.PostAsync($"/api/v1/financial/card-statements/{Guid.NewGuid()}/unmark-paid", null);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    private async Task<AccountStatementDTO> GetVisaStatementAsync() =>
        (await Client.GetFromJsonAsync<AccountDetailDTO>($"/api/v1/financial/accounts/{BarclaysPlatinumVisa8003Id}/2026/7"))!.ViewedStatement!;

    private async Task<AccountTransactionDTO> GetVisaChargeAsync() =>
        (await Client.GetFromJsonAsync<List<AccountTransactionDTO>>($"/api/v1/financial/accounts/{BarclaysPlatinumVisa8003Id}/transactions/2026/7"))!
            .Single(row => row.Description == "Card charge");

    private Task<HttpResponseMessage> AddChargeAsync(decimal value) =>
        Client.PostAsJsonAsync("/api/v1/financial/expenses", new ExpenseCreateDTO
        {
            Date = new DateOnly(2026, 7, 10),
            Description = "Card charge",
            Value = value,
            CategoryId = MercadoId,
            PaymentSourceBankId = null,
            CreditCardId = BarclaysPlatinumVisa8003Id
        });

    private static Task<HttpResponseMessage> MarkPaidAsync(HttpClient client, Guid? id, Guid paymentSourceBankId) =>
        client.PostAsJsonAsync(
            $"/api/v1/financial/card-statements/{id}/mark-paid",
            new MarkCardStatementPaidDTO { PaymentSourceBankId = paymentSourceBankId });
}
