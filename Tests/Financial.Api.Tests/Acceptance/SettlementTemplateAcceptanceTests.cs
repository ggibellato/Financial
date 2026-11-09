using System.Net;
using System.Net.Http.Json;
using Financial.CashFlow.Application.DTOs;
using Financial.CashFlow.Application.Interfaces;
using Financial.CashFlow.Domain.Enums;
using Financial.CashFlow.Infrastructure.Tools.CashFlowSpreadsheetImport.DataFileMigration;
using Financial.Shared.Abstractions.Currencies;
using FluentAssertions;
using FluentAssertions.Execution;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

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
          "Version": 5
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
    public async Task AdminAccounts_CreateGbpOnlySettlement_HasOneCurrencyAndIsSettled()
    {
        var created = await Client.PostAsJsonAsync($"{Api}/accounts", Settlement("Samuel", "GBP", ["GBP"], "Samuel"));
        var samuel = await created.Content.ReadFromJsonAsync<AccountDTO>();
        var listed = (await Client.GetFromJsonAsync<List<AccountDTO>>($"{Api}/accounts"))!;
        var groups = (await Client.GetFromJsonAsync<List<AccountTemplateGroupDTO>>($"{Api}/accounts/groups/by-template/2026/11"))!;

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
            var settlement = groups.Should().ContainSingle(group => group.TemplateName == "Settlement").Which;
            settlement.Members.Should().ContainSingle().Which.Should().BeEquivalentTo(new
            {
                Name = "Samuel",
                OtherParticipant = "Samuel",
                CurrencyBalances = new[] { new { Currency = "GBP", Balance = 0m } }
            });
            settlement.Totals.Should().BeEmpty();
        }
    }

    [Fact]
    [Trait("AC", "P60-F11-settlement-template-and-mae-migration-07")]
    public async Task TemplateGroups_Settlement_HasPerCurrencyBalancesAndNoTotals()
    {
        SeedMigratedV4File();

        var groups = (await Client.GetFromJsonAsync<List<AccountTemplateGroupDTO>>($"{Api}/accounts/groups/by-template/2026/7"))!;

        using (new AssertionScope())
        {
            var settlement = groups.Should().ContainSingle(group => group.TemplateName == "Settlement").Which;
            settlement.Members.Should().ContainSingle().Which.Should().BeEquivalentTo(new
            {
                Name = "Mãe",
                Currency = "BRL",
                Balance = 350.1234m - 186m - 300.5m + 654.24m,
                OtherParticipant = "Mãe",
                CurrencyBalances = new[]
                {
                    new { Currency = "BRL", Balance = 350.1234m - 186m - 300.5m + 654.24m },
                    new { Currency = "GBP", Balance = 51.23456789m - 40m + 100m }
                }
            }, options => options.WithStrictOrdering());
            settlement.Totals.Should().BeEmpty();
            groups.Should().ContainSingle(group => group.TemplateName == "Bank").Which.Totals.Should().NotBeEmpty();
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

    private const string PaidByYouId = "5e771e00-0000-0000-0000-000000000101";
    private const string PaidByMaeId = "5e771e00-0000-0000-0000-000000000102";
    private const string GbpEntryId = "5e771e00-0000-0000-0000-000000000103";
    private const string PoundEntryId = "5e771e00-0000-0000-0000-000000000104";

    private const string V4SeedWithMaeEntries = $$"""
        {
          "Accounts": [
            { "Id": "{{BarclaysId}}", "Name": "Barclays", "Template": "Bank", "Currency": "GBP", "IsActive": true, "OpeningBalance": { "Amount": 1000, "StartDate": "2026-01-01" }, "RoundUp": { "Enabled": false }, "Pots": null, "CategoryId": null }
          ],
          "MaeLedgerEntries": [
            { "Id": "{{PaidByYouId}}", "Date": "2026-07-01", "Description": "School supplies", "Note": "Term start", "SourceCurrency": "BRL", "BrlValue": 350.1234, "GbpValue": 51.23456789 },
            { "Id": "{{PaidByMaeId}}", "Date": "2026-07-02", "Description": "Insurance", "Note": "", "SourceCurrency": "BRL", "BrlValue": -186, "GbpValue": null },
            { "Id": "{{GbpEntryId}}", "Date": "2026-07-03", "Description": "Doctor", "Note": "", "SourceCurrency": "GBP", "BrlValue": -300.5, "GbpValue": -40 },
            { "Id": "{{PoundEntryId}}", "Date": "2022-11-25", "Description": "Presente Samuel 100 libras", "Note": "libras", "SourceCurrency": "BRL", "BrlValue": 654.24, "GbpValue": 100 }
          ],
          "Version": 4
        }
        """;

    private const string MaeAccountId = "5e771e00-0000-0000-0000-000000000002";

    private const string V5SeedWithMae = $$"""
        {
          "Accounts": [
            { "Id": "{{BarclaysId}}", "Name": "Barclays", "Template": "Bank", "Currency": "GBP", "IsActive": true, "OpeningBalance": { "Amount": 1000, "StartDate": "2026-01-01" }, "RoundUp": { "Enabled": false }, "Pots": null },
            { "Id": "{{MaeAccountId}}", "Name": "Mãe", "Template": "Settlement", "Currency": "BRL", "IsActive": true, "OpeningBalance": null, "RoundUp": null, "Pots": null, "CategoryId": null, "Currencies": ["BRL", "GBP"], "OtherParticipant": "Mãe" }
          ],
          "SettlementTransactions": [],
          "Version": 5
        }
        """;

    private void SeedMigratedV4File() =>
        ReplaceCashFlowDataBeforeStart(DataFileMigrator.CreateDefault().Migrate(V4SeedWithMaeEntries, "seed.json").Json);

    private async Task<AccountDTO> MigratedMaeAccount() =>
        (await Client.GetFromJsonAsync<List<AccountDTO>>($"{Api}/accounts"))!.Single(account => account.Name == "Mãe");

    [Fact]
    [Trait("AC", "P60-F11-settlement-template-and-mae-migration-02")]
    public async Task MaeAccount_MigratedFile_CurrentBalancesKeepTheTotalsAtStoredPrecision()
    {
        SeedMigratedV4File();
        var mae = await MigratedMaeAccount();

        var detail = await Client.GetFromJsonAsync<AccountDetailDTO>($"{Api}/accounts/{mae.Id}/2026/7");

        detail!.CurrentBalances.Should().BeEquivalentTo(new[]
        {
            new { Currency = "BRL", Balance = 517.8634m },
            new { Currency = "GBP", Balance = 111.23456789m }
        });
    }

    [Fact]
    public async Task AdminAccounts_MaeWithEntries_IsInUse_DeleteReturns409AndCurrenciesAreLocked()
    {
        SeedMigratedV4File();
        var mae = await MigratedMaeAccount();

        var deleted = await Client.DeleteAsync($"{Api}/accounts/{mae.Id}");
        var deleteProblem = await deleted.Content.ReadFromJsonAsync<ProblemDetails>();
        var edited = await Client.PutAsJsonAsync($"{Api}/accounts/{mae.Id}", new AccountUpdateDTO
        {
            TemplateName = "Settlement",
            Name = "Mãe",
            Currency = "BRL",
            Currencies = ["BRL"],
            OtherParticipant = "Mãe",
            IsActive = true
        });
        var editProblem = await edited.Content.ReadFromJsonAsync<ProblemDetails>();

        using (new AssertionScope())
        {
            mae.HasRecords.Should().BeTrue();
            deleted.StatusCode.Should().Be(HttpStatusCode.Conflict);
            deleteProblem!.Detail.Should().Be("Mãe has transactions and cannot be deleted. Deactivate it instead.");
            edited.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            editProblem!.Detail.Should().Be("Template and currency can't change once the account has transactions.");
        }
    }

    [Fact]
    [Trait("AC", "P60-F11-settlement-template-and-mae-migration-02")]
    [Trait("AC", "P60-F11-settlement-template-and-mae-migration-03")]
    public async Task MaeTransactions_MigratedFile_KeepEachEntryItsCurrencyAmountAndConvertedValueOrEmpty()
    {
        SeedMigratedV4File();
        var mae = await MigratedMaeAccount();

        var july = await Client.GetFromJsonAsync<List<AccountTransactionDTO>>($"{Api}/accounts/{mae.Id}/transactions/2026/7");
        var november = await Client.GetFromJsonAsync<List<AccountTransactionDTO>>($"{Api}/accounts/{mae.Id}/transactions/2022/11");

        using (new AssertionScope())
        {
            july!.Select(row => (row.Description, row.Amount, row.Currency, row.Settlement!.ConvertedValue)).Should().Equal(
                ("Doctor", -40m, "GBP", 300.5m),
                ("Insurance", -186m, "BRL", (decimal?)null),
                ("School supplies", 350.1234m, "BRL", 51.23456789m));
            november!.Select(row => (row.Description, row.Amount, row.Currency, row.Settlement!.ConvertedValue)).Should().Equal(
                ("Presente Samuel 100 libras", 100m, "GBP", 654.24m));
        }
    }

    [Fact]
    [Trait("AC", "P60-F11-settlement-template-and-mae-migration-04")]
    public async Task AddSettlementEntry_StoresPaidByAmountAndConvertedValueAtTheDateRate()
    {
        await using var factory = new ApiTestFactory(new StubExchangeRateProvider(6.138m));
        factory.ReplaceCashFlowData(V5SeedWithMae);
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync($"{Api}/accounts/{MaeAccountId}/settlement-entries", new SettlementEntryCreateDTO
        {
            Date = new DateOnly(2026, 10, 1),
            Description = "Gift paid by Mãe",
            PaidBy = "Other",
            Currency = "GBP",
            Amount = 80m
        });
        var returned = await response.Content.ReadFromJsonAsync<SettlementEntryDTO>();
        var stored = factory.Services.GetRequiredService<ICashFlowRepository>().GetSettlementTransactions().Single();

        using (new AssertionScope())
        {
            response.StatusCode.Should().Be(HttpStatusCode.Created);
            stored.Account.Name.Should().Be("Mãe");
            stored.PaidBy.Should().Be(SettlementPayer.Other);
            stored.Currency.Should().Be(Currency.GBP);
            stored.Amount.Should().Be(80m);
            stored.ConvertedValue.Should().Be(491.04m);
            returned!.Currency.Should().Be("GBP");
            returned.Amount.Should().Be(80m);
            returned.ConvertedValue.Should().Be(491.04m);
        }
    }
}
