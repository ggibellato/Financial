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

        await ShouldBeRejectedAsync(response, "Date must not be in the future.");
        var stored = await Client.GetFromJsonAsync<List<TransferDTO>>("/api/v1/financial/transfers/month/2026/10");
        stored.Should().ContainSingle().Which.Date.Should().Be(new DateOnly(2026, 10, 3));
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

    private async Task ShouldBeRejectedAsync(HttpResponseMessage response, string message)
    {
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        var stored = await Client.GetFromJsonAsync<List<TransferDTO>>("/api/v1/financial/transfers/month/2026/10");

        using (new AssertionScope())
        {
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            problem!.Detail.Should().Be(message);
            stored.Should().ContainSingle().Which.Id.Should().Be(Guid.Parse(ExistingTransferId));
        }
    }
}
