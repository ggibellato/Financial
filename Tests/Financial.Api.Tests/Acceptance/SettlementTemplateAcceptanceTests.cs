using System.Net;
using System.Net.Http.Json;
using Financial.CashFlow.Application.DTOs;
using FluentAssertions;
using FluentAssertions.Execution;
using Microsoft.AspNetCore.Mvc;

namespace Financial.Api.Tests.Acceptance;

[Trait("Category", "Integration")]
public class SettlementTemplateAcceptanceTests : ApiEndpointTests
{
    private const string Api = "/api/v1/financial";
    private const string BarclaysId = "5e771e00-0000-0000-0000-000000000001";

    private const string SeededData = $$"""
        {
          "Accounts": [
            { "Id": "{{BarclaysId}}", "Name": "Barclays", "Template": "Bank", "Currency": "GBP", "IsActive": true, "OpeningBalance": { "Amount": 1000, "StartDate": "2026-01-01" }, "RoundUp": { "Enabled": false }, "Pots": null }
          ],
          "Version": 4
        }
        """;

    public SettlementTemplateAcceptanceTests()
    {
        ReplaceCashFlowDataBeforeStart(SeededData);
    }

    private static AccountCreateDTO Settlement(string name, string currency, IReadOnlyList<string> currencies, string? otherParticipant) => new()
    {
        TemplateName = "Settlement",
        Name = name,
        Currency = currency,
        Currencies = currencies,
        OtherParticipant = otherParticipant
    };

    [Fact]
    [Trait("AC", "P60-F11-settlement-template-and-mae-migration-05")]
    public async Task AdminAccounts_CreateGbpOnlySettlement_HasOneCurrencyAndItsOtherParticipant()
    {
        var created = await Client.PostAsJsonAsync($"{Api}/accounts", Settlement("Samuel", "GBP", ["GBP"], "Samuel"));
        var samuel = await created.Content.ReadFromJsonAsync<AccountDTO>();
        var listed = (await Client.GetFromJsonAsync<List<AccountDTO>>($"{Api}/accounts"))!;

        using (new AssertionScope())
        {
            created.StatusCode.Should().Be(HttpStatusCode.Created);
            samuel!.TemplateName.Should().Be("Settlement");
            samuel.Currency.Should().Be("GBP");
            samuel.Currencies.Should().Equal("GBP");
            samuel.OtherParticipant.Should().Be("Samuel");
            samuel.HasRecords.Should().BeFalse();
            listed.Should().ContainSingle(account => account.Id == samuel.Id)
                .Which.Should().BeEquivalentTo(new { Currencies = new[] { "GBP" }, OtherParticipant = "Samuel" });
            listed.Should().ContainSingle(account => account.Name == "Barclays")
                .Which.Should().BeEquivalentTo(new { Currencies = new[] { "GBP" }, OtherParticipant = (string?)null });
        }
    }

    [Fact]
    public async Task AdminAccounts_EditTwoCurrencySettlement_KeepsBothCurrencies()
    {
        var created = await Client.PostAsJsonAsync($"{Api}/accounts", Settlement("Mãe", "BRL", ["GBP", "BRL"], "Mãe"));
        var mae = (await created.Content.ReadFromJsonAsync<AccountDTO>())!;

        var edited = await Client.PutAsJsonAsync($"{Api}/accounts/{mae.Id}", new AccountUpdateDTO
        {
            TemplateName = "Settlement",
            Name = "Mãe",
            Currency = mae.Currency,
            Currencies = mae.Currencies,
            OtherParticipant = "Mother",
            IsActive = false
        });
        var listed = (await Client.GetFromJsonAsync<List<AccountDTO>>($"{Api}/accounts"))!;

        using (new AssertionScope())
        {
            mae.Currency.Should().Be("BRL");
            mae.Currencies.Should().Equal("BRL", "GBP");
            edited.StatusCode.Should().Be(HttpStatusCode.OK);
            listed.Should().ContainSingle(account => account.Id == mae.Id)
                .Which.Should().BeEquivalentTo(new { Currency = "BRL", Currencies = new[] { "BRL", "GBP" }, OtherParticipant = "Mother", IsActive = false });
        }
    }

    [Theory]
    [InlineData("BRL", "", "Mãe", "Choose at least one currency.")]
    [InlineData("GBP", "BRL", "Mãe", "Currency must be one of the account's currencies.")]
    [InlineData("BRL", "BRL", null, "Enter the other participant's name.")]
    [InlineData("BRL", "BRL", "YOU", "The other participant can't be called You.")]
    public async Task AdminAccounts_SettlementWithAnInvalidField_Returns400WithTheMessage(
        string currency, string currencies, string? otherParticipant, string message)
    {
        var response = await Client.PostAsJsonAsync(
            $"{Api}/accounts",
            Settlement("Mãe", currency, currencies.Split(',', StringSplitOptions.RemoveEmptyEntries), otherParticipant));
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        var listed = (await Client.GetFromJsonAsync<List<AccountDTO>>($"{Api}/accounts"))!;

        using (new AssertionScope())
        {
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            problem!.Detail.Should().Be(message);
            listed.Should().ContainSingle();
        }
    }

    [Fact]
    public async Task AdminAccounts_SettlementInUsd_Returns400WithTheCurrencyMessage()
    {
        var response = await Client.PostAsJsonAsync($"{Api}/accounts", Settlement("Mãe", "USD", ["USD"], "Mãe"));
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();

        using (new AssertionScope())
        {
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            problem!.Detail.Should().Be("Settlement accounts must be BRL or GBP.");
        }
    }

    [Fact]
    public async Task AdminAccounts_BankWithAnOtherParticipant_Returns400()
    {
        var response = await Client.PutAsJsonAsync($"{Api}/accounts/{BarclaysId}", new AccountUpdateDTO
        {
            TemplateName = "Bank",
            Name = "Barclays",
            Currency = "GBP",
            Currencies = ["GBP"],
            OtherParticipant = "Mãe",
            OpeningBalance = 1000m,
            BalanceStartDate = new DateOnly(2026, 1, 1),
            RoundUpEnabled = false,
            IsActive = true
        });
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();

        using (new AssertionScope())
        {
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            problem!.Detail.Should().Be("Barclays has no participants.");
        }
    }
}
