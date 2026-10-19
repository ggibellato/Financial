using System.Net;
using System.Net.Http.Json;
using Financial.CashFlow.Application.DTOs;
using FluentAssertions;
using FluentAssertions.Execution;
using Microsoft.AspNetCore.Mvc;

namespace Financial.Api.Tests.Acceptance;

[Trait("Category", "Integration")]
public class GenericAccountTransferAcceptanceTests : ApiEndpointTests
{
    private const string BarclaysId = "8f3b1c1a-2e3a-4b1a-9a7f-100000000001";
    private const string ChaseId = "8f3b1c1a-2e3a-4b1a-9a7f-100000000002";
    private const string ItauId = "8f3b1c1a-2e3a-4b1a-9a7f-100000000004";
    private const string ClosedId = "8f3b1c1a-2e3a-4b1a-9a7f-100000000005";
    private const string ExistingTransferId = "8f3b1c1a-2e3a-4b1a-9a7f-900000000001";

    private const string SeededData = $$"""
        {
          "Accounts": [
            { "Id": "{{BarclaysId}}", "Name": "Barclays", "Template": "Bank", "Currency": "GBP", "IsActive": true, "OpeningBalance": { "Amount": 0, "StartDate": "0001-01-01" }, "RoundUp": { "Enabled": false } },
            { "Id": "{{ChaseId}}", "Name": "Chase", "Template": "Bank", "Currency": "GBP", "IsActive": true, "OpeningBalance": { "Amount": 0, "StartDate": "0001-01-01" }, "RoundUp": { "Enabled": true } },
            { "Id": "{{ItauId}}", "Name": "Itaú", "Template": "Bank", "Currency": "BRL", "IsActive": true, "OpeningBalance": { "Amount": 0, "StartDate": "0001-01-01" }, "RoundUp": { "Enabled": false } },
            { "Id": "{{ClosedId}}", "Name": "Closed", "Template": "Bank", "Currency": "GBP", "IsActive": false, "OpeningBalance": { "Amount": 0, "StartDate": "0001-01-01" }, "RoundUp": { "Enabled": false } }
          ],
          "Transfers": [
            { "Id": "{{ExistingTransferId}}", "Date": "2026-10-03", "SourceAccountId": "{{BarclaysId}}", "DestinationAccountId": "{{ChaseId}}", "Amount": 75.25, "Note": "Top-up" }
          ],
          "Version": 2
        }
        """;

    public GenericAccountTransferAcceptanceTests()
        : base(timeProvider: TestClock.At(new DateTimeOffset(2026, 10, 15, 12, 0, 0, TimeSpan.FromHours(1))))
    {
        ReplaceCashFlowDataBeforeStart(SeededData);
    }

    [Fact]
    [Trait("AC", "P60-F03-generic-account-to-account-transfer-01")]
    public async Task PostAccountTransfer_BankToBank_MovesBothBalancesOnItsDate()
    {
        var septemberBefore = await BalancesAsync(9);
        var octoberBefore = await BalancesAsync(10);

        var response = await PostAccountTransferAsync(BarclaysId, ChaseId);

        var septemberAfter = await BalancesAsync(9);
        var octoberAfter = await BalancesAsync(10);
        using (new AssertionScope())
        {
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            (octoberAfter["Barclays"] - octoberBefore["Barclays"]).Should().Be(-100m);
            (octoberAfter["Chase"] - octoberBefore["Chase"]).Should().Be(100m);
            septemberAfter.Should().Equal(septemberBefore);
        }
    }

    [Fact]
    [Trait("AC", "P60-F03-generic-account-to-account-transfer-03")]
    public async Task PostAccountTransfer_SameAccount_Returns400()
    {
        var response = await PostAccountTransferAsync(BarclaysId, BarclaysId);

        await ShouldBeRejectedAsync(response, "Source and destination must be different accounts.");
    }

    [Fact]
    [Trait("AC", "P60-F03-generic-account-to-account-transfer-04")]
    public async Task PostAccountTransfer_DifferentCurrenciesWithoutDestinationAmount_Returns400()
    {
        var response = await PostAccountTransferAsync(BarclaysId, ItauId);

        await ShouldBeRejectedAsync(response, "Enter the amount received in BRL.");
    }

    [Fact]
    [Trait("AC", "P60-F03-generic-account-to-account-transfer-04")]
    public async Task PostAccountTransfer_DifferentCurrenciesWithDestinationAmount_EachSideMovesByItsOwnAmount()
    {
        var before = await BalancesAsync(10);

        var response = await PostAccountTransferAsync(BarclaysId, ItauId, destinationAmount: 640.50m);

        var transfer = await response.Content.ReadFromJsonAsync<AccountTransferDTO>();
        var after = await BalancesAsync(10);
        using (new AssertionScope())
        {
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            transfer!.SourceCurrency.Should().Be("GBP");
            transfer.DestinationCurrency.Should().Be("BRL");
            transfer.Amount.Should().Be(100m);
            transfer.DestinationAmount.Should().Be(640.50m);
            (after["Barclays"] - before["Barclays"]).Should().Be(-100m);
            (after["Itaú"] - before["Itaú"]).Should().Be(640.50m);
        }
    }

    [Fact]
    public async Task PostAccountTransfer_SameCurrencyWithDestinationAmount_Returns400()
    {
        var response = await PostAccountTransferAsync(BarclaysId, ChaseId, destinationAmount: 100m);

        await ShouldBeRejectedAsync(response, "Destination amount only applies when the currencies differ.");
    }

    [Fact]
    [Trait("AC", "P60-F03-generic-account-to-account-transfer-05")]
    public async Task GetTransferDestinations_Bank_ListsOtherActiveBanksOnly()
    {
        var destinations = await Client.GetFromJsonAsync<List<TransferAccountOptionDTO>>(
            $"/api/v1/financial/accounts/{BarclaysId}/transfer-destinations");

        destinations!.Select(option => (option.Name, option.Currency)).Should().Equal(("Chase", "GBP"), ("Itaú", "BRL"));
    }

    [Fact]
    [Trait("AC", "P60-F03-generic-account-to-account-transfer-05")]
    public async Task GetTransferSources_Bank_ListsOtherActiveBanksOnly()
    {
        var sources = await Client.GetFromJsonAsync<List<TransferAccountOptionDTO>>(
            $"/api/v1/financial/accounts/{ChaseId}/transfer-sources");

        sources!.Select(option => option.Name).Should().Equal("Barclays", "Itaú");
    }

    [Fact]
    public async Task GetTransferDestinations_UnknownAccount_Returns404()
    {
        var response = await Client.GetAsync($"/api/v1/financial/accounts/{Guid.NewGuid()}/transfer-destinations");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task PutThenDeleteAccountTransfer_ExistingTransfer_UpdatesThenRemovesIt()
    {
        var put = await Client.PutAsJsonAsync($"/api/v1/financial/accounts/transfers/{ExistingTransferId}", new AccountTransferUpdateDTO
        {
            Date = new DateOnly(2026, 10, 4),
            SourceAccountId = Guid.Parse(BarclaysId),
            DestinationAccountId = Guid.Parse(ItauId),
            Amount = 75.25m,
            DestinationAmount = 480m,
            Note = "Sent to Itaú"
        });
        var updated = await put.Content.ReadFromJsonAsync<AccountTransferDTO>();
        var delete = await Client.DeleteAsync($"/api/v1/financial/accounts/transfers/{ExistingTransferId}");
        var remaining = await Client.GetFromJsonAsync<List<TransferDTO>>("/api/v1/financial/transfers/month/2026/10");

        using (new AssertionScope())
        {
            put.StatusCode.Should().Be(HttpStatusCode.OK);
            updated!.DestinationAccountName.Should().Be("Itaú");
            updated.DestinationAmount.Should().Be(480m);
            delete.StatusCode.Should().Be(HttpStatusCode.OK);
            remaining.Should().BeEmpty();
        }
    }

    [Fact]
    public async Task PutAccountTransfer_UnknownTransfer_Returns404()
    {
        var response = await Client.PutAsJsonAsync($"/api/v1/financial/accounts/transfers/{Guid.NewGuid()}", new AccountTransferUpdateDTO
        {
            Date = new DateOnly(2026, 10, 4),
            SourceAccountId = Guid.Parse(BarclaysId),
            DestinationAccountId = Guid.Parse(ChaseId),
            Amount = 10m
        });

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    [Trait("AC", "P60-F03-generic-account-to-account-transfer-06")]
    public async Task GetTransfersByMonth_ExistingTransfer_ReturnsSameDataAsBefore()
    {
        var transfers = await Client.GetStringAsync("/api/v1/financial/transfers/month/2026/10");
        var balances = await Client.GetFromJsonAsync<List<BankBalanceDTO>>("/api/v1/financial/banks/month/2026/10/balances");

        using (new AssertionScope())
        {
            transfers.Should().Be(
                $$"""[{"id":"{{ExistingTransferId}}","date":"2026-10-03","sourceBankId":"{{BarclaysId}}","sourceBankName":"Barclays","destinationBankId":"{{ChaseId}}","destinationBankName":"Chase","amount":75.25,"note":"Top-up"}]""");
            balances.Should().ContainSingle(b => b.Bank == "Barclays").Which.Balance.Should().Be(-75.25m);
            balances.Should().ContainSingle(b => b.Bank == "Chase").Which.Balance.Should().Be(75.25m);
        }
    }

    [Fact]
    [Trait("AC", "P60-F03-generic-account-to-account-transfer-08")]
    public async Task PostTransfers_FutureDate_Returns400WithSameMessage()
    {
        var response = await PostTransferAsync(new DateOnly(2026, 10, 16), BarclaysId, ChaseId, note: null);

        await ShouldBeRejectedAsync(response, "Date must not be in the future.");
    }

    [Fact]
    [Trait("AC", "P60-F03-generic-account-to-account-transfer-08")]
    public async Task PostTransfers_DatedToday_Returns200()
    {
        var response = await PostTransferAsync(new DateOnly(2026, 10, 15), BarclaysId, ChaseId, note: null);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    [Trait("AC", "P60-F03-generic-account-to-account-transfer-08")]
    public async Task PostTransfers_NoteOver200_Returns400WithSameMessage()
    {
        var response = await PostTransferAsync(new DateOnly(2026, 10, 10), BarclaysId, ChaseId, new string('a', 201));

        await ShouldBeRejectedAsync(response, "Note must be 200 characters or fewer.");
    }

    [Fact]
    [Trait("AC", "P60-F03-generic-account-to-account-transfer-08")]
    public async Task PostTransfers_InactiveAccount_Returns400WithSameMessage()
    {
        var response = await PostTransferAsync(new DateOnly(2026, 10, 10), BarclaysId, ClosedId, note: null);

        await ShouldBeRejectedAsync(response, "Closed is inactive.");
    }

    [Fact]
    [Trait("AC", "P60-F03-generic-account-to-account-transfer-08")]
    public async Task PutTransfers_FutureDate_Returns400AndKeepsTheStoredTransfer()
    {
        var response = await Client.PutAsJsonAsync($"/api/v1/financial/transfers/{ExistingTransferId}", new TransferUpdateDTO
        {
            Date = new DateOnly(2026, 10, 16),
            SourceBankId = Guid.Parse(BarclaysId),
            DestinationBankId = Guid.Parse(ChaseId),
            Amount = 75.25m,
            Note = "Top-up"
        });

        var stored = await ShouldBeRejectedAsync(response, "Date must not be in the future.");

        stored.Single().Date.Should().Be(new DateOnly(2026, 10, 3));
    }

    private Task<HttpResponseMessage> PostTransferAsync(DateOnly date, string sourceId, string destinationId, string? note) =>
        Client.PostAsJsonAsync("/api/v1/financial/transfers", new TransferCreateDTO
        {
            Date = date,
            SourceBankId = Guid.Parse(sourceId),
            DestinationBankId = Guid.Parse(destinationId),
            Amount = 10m,
            Note = note
        });

    private Task<HttpResponseMessage> PostAccountTransferAsync(string sourceId, string destinationId, decimal? destinationAmount = null) =>
        Client.PostAsJsonAsync("/api/v1/financial/accounts/transfers", new AccountTransferCreateDTO
        {
            Date = new DateOnly(2026, 10, 12),
            SourceAccountId = Guid.Parse(sourceId),
            DestinationAccountId = Guid.Parse(destinationId),
            Amount = 100m,
            DestinationAmount = destinationAmount,
            Note = "Send"
        });

    private async Task<Dictionary<string, decimal>> BalancesAsync(int month) =>
        (await Client.GetFromJsonAsync<List<BankBalanceDTO>>($"/api/v1/financial/banks/month/2026/{month}/balances"))!
            .ToDictionary(balance => balance.Bank, balance => balance.Balance);

    private async Task<List<TransferDTO>> ShouldBeRejectedAsync(HttpResponseMessage response, string message)
    {
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        var stored = await Client.GetFromJsonAsync<List<TransferDTO>>("/api/v1/financial/transfers/month/2026/10");

        using (new AssertionScope())
        {
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            problem!.Detail.Should().Be(message);
            stored.Should().ContainSingle().Which.Id.Should().Be(Guid.Parse(ExistingTransferId));
        }

        return stored!;
    }
}
