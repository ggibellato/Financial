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

    private const string V5SeedWithMae = $$"""
        {
          "Accounts": [
            { "Id": "{{BarclaysId}}", "Name": "Barclays", "Template": "Bank", "Currency": "GBP", "IsActive": true, "OpeningBalance": { "Amount": 1000, "StartDate": "2026-01-01" }, "RoundUp": { "Enabled": false }, "Pots": null },
            { "Id": "5e771e00-0000-0000-0000-000000000002", "Name": "Mãe", "Template": "Settlement", "Currency": "BRL", "IsActive": true, "OpeningBalance": null, "RoundUp": null, "Pots": null, "CategoryId": null, "Currencies": ["BRL", "GBP"], "OtherParticipant": "Mãe" }
          ],
          "SettlementTransactions": [],
          "Version": 5
        }
        """;

    private static readonly MaeLedgerEntryDTO[] V4EntriesAsReturnedBefore =
    [
        new() { Id = Guid.Parse(PoundEntryId), Date = new DateOnly(2022, 11, 25), Description = "Presente Samuel 100 libras", Note = "libras", SourceCurrency = "BRL", BrlValue = 654.24m, GbpValue = 100m },
        new() { Id = Guid.Parse(PaidByYouId), Date = new DateOnly(2026, 7, 1), Description = "School supplies", Note = "Term start", SourceCurrency = "BRL", BrlValue = 350.1234m, GbpValue = 51.23456789m },
        new() { Id = Guid.Parse(PaidByMaeId), Date = new DateOnly(2026, 7, 2), Description = "Insurance", Note = "", SourceCurrency = "BRL", BrlValue = -186m, GbpValue = null },
        new() { Id = Guid.Parse(GbpEntryId), Date = new DateOnly(2026, 7, 3), Description = "Doctor", Note = "", SourceCurrency = "GBP", BrlValue = -300.5m, GbpValue = -40m }
    ];

    private void SeedMigratedV4File() =>
        ReplaceCashFlowDataBeforeStart(DataFileMigrator.CreateDefault().Migrate(V4SeedWithMaeEntries, "seed.json").Json);

    [Fact]
    [Trait("AC", "P60-F11-settlement-template-and-mae-migration-02")]
    public async Task ControleMae_MigratedFile_ReturnsTheSameTotals()
    {
        SeedMigratedV4File();

        var totals = await Client.GetFromJsonAsync<MaeLedgerTotalsDTO>($"{Api}/controle-mae/entries/totals");

        using (new AssertionScope())
        {
            totals!.TotalBrlValue.Should().Be(350.1234m - 186m - 300.5m + 654.24m);
            totals.TotalGbpValue.Should().Be(51.23456789m - 40m + 100m);
        }
    }

    [Fact]
    public async Task AdminAccounts_MaeWithEntries_IsInUse_DeleteReturns409AndCurrenciesAreLocked()
    {
        SeedMigratedV4File();
        var mae = (await Client.GetFromJsonAsync<List<AccountDTO>>($"{Api}/accounts"))!.Single(account => account.Name == "Mãe");

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
    [Trait("AC", "P60-F11-settlement-template-and-mae-migration-03")]
    [Trait("AC", "P60-F11-settlement-template-and-mae-migration-06")]
    public async Task ControleMae_MigratedFile_ReturnsTheSameEntriesAndValues()
    {
        SeedMigratedV4File();

        var entries = await Client.GetFromJsonAsync<List<MaeLedgerEntryDTO>>($"{Api}/controle-mae/entries/from/2000-01-01");

        var expected = V4EntriesAsReturnedBefore
            .Select(entry => entry.Id == Guid.Parse(PoundEntryId)
                ? new MaeLedgerEntryDTO { Id = entry.Id, Date = entry.Date, Description = entry.Description, Note = entry.Note, SourceCurrency = "GBP", BrlValue = entry.BrlValue, GbpValue = entry.GbpValue }
                : entry);
        entries.Should().BeEquivalentTo(expected, options => options.WithStrictOrdering());
    }

    [Fact]
    [Trait("AC", "P60-F11-settlement-template-and-mae-migration-04")]
    public async Task ControleMae_CreateEntry_StoresPaidByAmountAndConvertedValueAtTheDateRate()
    {
        await using var factory = new ApiTestFactory(new StubExchangeRateProvider(6.138m));
        factory.ReplaceCashFlowData(V5SeedWithMae);
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync($"{Api}/controle-mae/entries", new MaeLedgerEntryCreateDTO
        {
            Date = new DateOnly(2026, 10, 1),
            Description = "Gift paid by Mãe",
            SourceCurrency = "GBP",
            SourceValue = -80m
        });
        var returned = await response.Content.ReadFromJsonAsync<MaeLedgerEntryDTO>();
        var stored = factory.Services.GetRequiredService<ICashFlowRepository>().GetSettlementTransactions().Single();

        using (new AssertionScope())
        {
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            stored.Account.Name.Should().Be("Mãe");
            stored.PaidBy.Should().Be(SettlementPayer.Other);
            stored.Currency.Should().Be(Currency.GBP);
            stored.Amount.Should().Be(80m);
            stored.ConvertedValue.Should().Be(80m * 6.138m);
            returned!.SourceCurrency.Should().Be("GBP");
            returned.GbpValue.Should().Be(-80m);
            returned.BrlValue.Should().Be(-80m * 6.138m);
        }
    }

    [Fact]
    public async Task ControleMae_CreateEntryWithoutASettlementAccount_Returns400()
    {
        var response = await Client.PostAsJsonAsync($"{Api}/controle-mae/entries", new MaeLedgerEntryCreateDTO
        {
            Date = new DateOnly(2026, 10, 1),
            Description = "Entry",
            SourceCurrency = "BRL",
            SourceValue = 10m
        });
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();

        using (new AssertionScope())
        {
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            problem!.Detail.Should().Be("Create a Settlement account first.");
        }
    }

    [Fact]
    [Trait("AC", "P60-F11-settlement-template-and-mae-migration-06")]
    public async Task ControleMae_UpdateValues_SignSetsPaidBy_RefusesMissingZeroOrOppositeSigns()
    {
        SeedMigratedV4File();
        var url = $"{Api}/controle-mae/entries/{PaidByYouId}/values";

        var negative = await Client.PutAsJsonAsync(url, new MaeLedgerEntryValuesUpdateDTO { BrlValue = -350m, GbpValue = -51m });
        var updated = await negative.Content.ReadFromJsonAsync<MaeLedgerEntryDTO>();
        var missing = await Client.PutAsJsonAsync(url, new MaeLedgerEntryValuesUpdateDTO { GbpValue = 51m });
        var zero = await Client.PutAsJsonAsync(url, new MaeLedgerEntryValuesUpdateDTO { BrlValue = 0m, GbpValue = 51m });
        var opposite = await Client.PutAsJsonAsync(url, new MaeLedgerEntryValuesUpdateDTO { BrlValue = 350m, GbpValue = -51m });
        var stored = Services.GetRequiredService<ICashFlowRepository>().GetSettlementTransactions().Single(entry => entry.Id == Guid.Parse(PaidByYouId));

        using (new AssertionScope())
        {
            negative.StatusCode.Should().Be(HttpStatusCode.OK);
            updated!.BrlValue.Should().Be(-350m);
            updated.GbpValue.Should().Be(-51m);
            stored.PaidBy.Should().Be(SettlementPayer.Other);
            stored.Amount.Should().Be(350m);
            stored.ConvertedValue.Should().Be(51m);
            (await missing.Content.ReadFromJsonAsync<ProblemDetails>())!.Detail.Should().Be("BRL value is required and must not be zero.");
            (await zero.Content.ReadFromJsonAsync<ProblemDetails>())!.Detail.Should().Be("BRL value is required and must not be zero.");
            (await opposite.Content.ReadFromJsonAsync<ProblemDetails>())!.Detail.Should().Be("BRL and GBP values must both be positive or both be negative.");
        }
    }
}