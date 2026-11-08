using System.Net;
using System.Net.Http.Json;
using Financial.CashFlow.Application.DTOs;
using Financial.CashFlow.Application.Interfaces;
using Financial.CashFlow.Domain.Enums;
using Financial.Shared.Abstractions.Currencies;
using FluentAssertions;
using FluentAssertions.Execution;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

namespace Financial.Api.Tests.Acceptance;

[Trait("Category", "Integration")]
public class SettlementScreenAcceptanceTests : ApiEndpointTests
{
    private const string Api = "/api/v1/financial";
    private const string BarclaysId = "5e771e00-0000-0000-0000-000000000001";
    private const string MaeId = "5e771e00-0000-0000-0000-000000000002";
    private const string SamuelId = "5e771e00-0000-0000-0000-000000000003";
    private const string PaiId = "5e771e00-0000-0000-0000-000000000004";
    private const string MaeEntryId = "5e771e00-0000-0000-0000-000000000101";
    private const string SamuelEntryId = "5e771e00-0000-0000-0000-000000000102";
    private const string PaiEntryId = "5e771e00-0000-0000-0000-000000000103";
    private const decimal BrlToGbpRate = 0.14627391m;

    private const string SeededData = $$"""
        {
          "Accounts": [
            { "Id": "{{BarclaysId}}", "Name": "Barclays", "Template": "Bank", "Currency": "GBP", "IsActive": true, "OpeningBalance": { "Amount": 1000, "StartDate": "2026-01-01" }, "RoundUp": { "Enabled": false }, "Pots": null },
            { "Id": "{{MaeId}}", "Name": "Mãe", "Template": "Settlement", "Currency": "BRL", "IsActive": true, "OpeningBalance": null, "RoundUp": null, "Pots": null, "CategoryId": null, "Currencies": ["BRL", "GBP"], "OtherParticipant": "Mãe" },
            { "Id": "{{SamuelId}}", "Name": "Samuel", "Template": "Settlement", "Currency": "GBP", "IsActive": true, "OpeningBalance": null, "RoundUp": null, "Pots": null, "CategoryId": null, "Currencies": ["GBP"], "OtherParticipant": "Samuel" },
            { "Id": "{{PaiId}}", "Name": "Pai", "Template": "Settlement", "Currency": "GBP", "IsActive": false, "OpeningBalance": null, "RoundUp": null, "Pots": null, "CategoryId": null, "Currencies": ["GBP"], "OtherParticipant": "Pai" }
          ],
          "SettlementTransactions": [
            {"Id":"{{MaeEntryId}}","AccountId":"{{MaeId}}","Date":"2026-09-17","Description":"Farmácia","Note":"","PaidBy":"You","Currency":"BRL","Amount":150,"ConvertedValue":21.94},
            {"Id":"{{SamuelEntryId}}","AccountId":"{{SamuelId}}","Date":"2026-09-10","Description":"Trainers","Note":"","PaidBy":"You","Currency":"GBP","Amount":60,"ConvertedValue":null},
            {"Id":"{{PaiEntryId}}","AccountId":"{{PaiId}}","Date":"2026-09-10","Description":"Lunch","Note":"","PaidBy":"Other","Currency":"GBP","Amount":15,"ConvertedValue":null}
          ],
          "Version": 5
        }
        """;

    public SettlementScreenAcceptanceTests() : base(new StubExchangeRateProvider(BrlToGbpRate))
    {
        ReplaceCashFlowDataBeforeStart(SeededData);
    }

    private static string EntriesUrl(string accountId) => $"{Api}/accounts/{accountId}/settlement-entries";

    private static string EntryUrl(string accountId, string entryId) => $"{EntriesUrl(accountId)}/{entryId}";

    private static SettlementEntryCreateDTO NewEntry(string? currency, decimal amount = 150m, string paidBy = "You", DateOnly? date = null) => new()
    {
        Date = date ?? new DateOnly(2026, 9, 20),
        Description = "Mercado",
        PaidBy = paidBy,
        Currency = currency,
        Amount = amount
    };

    private static SettlementEntryUpdateDTO EntryUpdate(decimal? convertedValue, DateOnly? date = null) => new()
    {
        Date = date ?? new DateOnly(2026, 9, 16),
        Description = "Farmácia e mercado",
        Note = "metade",
        PaidBy = "Other",
        Amount = 180m,
        ConvertedValue = convertedValue
    };

    private IEnumerable<Financial.CashFlow.Domain.Entities.SettlementTransaction> StoredEntries(IServiceProvider? services = null) =>
        (services ?? Services).GetRequiredService<ICashFlowRepository>().GetSettlementTransactions();

    private static async Task<string?> DetailOf(HttpResponseMessage response) =>
        (await response.Content.ReadFromJsonAsync<ProblemDetails>())!.Detail;

    [Fact]
    [Trait("AC", "P60-F12-settlement-screen-04")]
    public async Task AddEntry_TwoCurrencies_StoresConvertedValueAtTheDateRateToTwoDecimals()
    {
        var response = await Client.PostAsJsonAsync(EntriesUrl(MaeId), NewEntry("BRL", 150.005m));
        var returned = await response.Content.ReadFromJsonAsync<SettlementEntryDTO>();

        using (new AssertionScope())
        {
            response.StatusCode.Should().Be(HttpStatusCode.Created);
            returned!.Should().BeEquivalentTo(new
            {
                AccountId = Guid.Parse(MaeId),
                Date = new DateOnly(2026, 9, 20),
                Description = "Mercado",
                Note = "",
                PaidBy = "You",
                Currency = "BRL",
                Amount = 150.01m,
                ConvertedCurrency = "GBP",
                ConvertedValue = 21.94m,
                Warning = (string?)null
            });
            var stored = StoredEntries().Single(entry => entry.Id == returned.Id);
            stored.Amount.Should().Be(150.01m);
            stored.ConvertedValue.Should().Be(21.94m);
        }
    }

    [Fact]
    [Trait("AC", "P60-F12-settlement-screen-04")]
    public async Task AddEntry_OneCurrency_NeedsNoCurrencyAndStoresNoConvertedValue()
    {
        var response = await Client.PostAsJsonAsync(EntriesUrl(SamuelId), NewEntry(null, 25m, "Other"));
        var returned = await response.Content.ReadFromJsonAsync<SettlementEntryDTO>();

        using (new AssertionScope())
        {
            response.StatusCode.Should().Be(HttpStatusCode.Created);
            returned!.Currency.Should().Be("GBP");
            returned.PaidBy.Should().Be("Other");
            returned.ConvertedCurrency.Should().BeNull();
            returned.ConvertedValue.Should().BeNull();
            returned.Warning.Should().BeNull();
            StoredEntries().Single(entry => entry.Id == returned.Id).PaidBy.Should().Be(SettlementPayer.Other);
        }
    }

    [Fact]
    [Trait("AC", "P60-F12-settlement-screen-04")]
    public async Task AddEntry_ConvertedRoundsToZero_SavesItEmptyWithWarning()
    {
        var response = await Client.PostAsJsonAsync(EntriesUrl(MaeId), NewEntry("BRL", 0.02m));
        var returned = await response.Content.ReadFromJsonAsync<SettlementEntryDTO>();

        using (new AssertionScope())
        {
            response.StatusCode.Should().Be(HttpStatusCode.Created);
            returned!.ConvertedValue.Should().BeNull();
            returned.Warning.Should().Be("The GBP value rounds to 0.00, so it was left empty.");
            StoredEntries().Single(entry => entry.Id == returned.Id).ConvertedValue.Should().BeNull();
        }
    }

    [Fact]
    [Trait("AC", "P60-F12-settlement-screen-04")]
    public async Task AddEntry_NoRate_SavesItEmptyWithWarningNamingTheOtherCurrency()
    {
        await using var factory = new ApiTestFactory(new StubExchangeRateProvider(null));
        factory.ReplaceCashFlowData(SeededData);
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync(EntriesUrl(MaeId), NewEntry("GBP", 80m, date: new DateOnly(2026, 9, 17)));
        var returned = await response.Content.ReadFromJsonAsync<SettlementEntryDTO>();

        using (new AssertionScope())
        {
            response.StatusCode.Should().Be(HttpStatusCode.Created);
            returned!.ConvertedCurrency.Should().Be("BRL");
            returned.ConvertedValue.Should().BeNull();
            returned.Warning.Should().Be("No exchange rate found for 17 Sep 2026; the BRL value is empty.");
            StoredEntries(factory.Services).Single(entry => entry.Id == returned.Id).ConvertedValue.Should().BeNull();
        }
    }

    [Fact]
    [Trait("AC", "P60-F12-settlement-screen-02")]
    public async Task EditEntry_ChangesDetailsAndValues_KeepsCurrencyAndDoesNotRecalculateTheConvertedValue()
    {
        var response = await Client.PutAsJsonAsync(EntryUrl(MaeId, MaeEntryId), EntryUpdate(25.6m));
        var returned = await response.Content.ReadFromJsonAsync<SettlementEntryDTO>();
        var stored = StoredEntries().Single(entry => entry.Id == Guid.Parse(MaeEntryId));

        using (new AssertionScope())
        {
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            returned!.Should().BeEquivalentTo(new
            {
                Id = Guid.Parse(MaeEntryId),
                Date = new DateOnly(2026, 9, 16),
                Description = "Farmácia e mercado",
                Note = "metade",
                PaidBy = "Other",
                Currency = "BRL",
                Amount = 180m,
                ConvertedCurrency = "GBP",
                ConvertedValue = 25.6m,
                Warning = (string?)null
            });
            stored.Currency.Should().Be(Currency.BRL);
            stored.PaidBy.Should().Be(SettlementPayer.Other);
            stored.Amount.Should().Be(180m);
            stored.ConvertedValue.Should().Be(25.6m);
        }
    }

    [Fact]
    public async Task EditEntry_ConvertedRoundsToZero_SavesItEmptyWithWarning()
    {
        var response = await Client.PutAsJsonAsync(EntryUrl(MaeId, MaeEntryId), EntryUpdate(0.001m));
        var returned = await response.Content.ReadFromJsonAsync<SettlementEntryDTO>();

        using (new AssertionScope())
        {
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            returned!.ConvertedValue.Should().BeNull();
            returned.Warning.Should().Be("The GBP value rounds to 0.00, so it was left empty.");
        }
    }

    [Fact]
    public async Task DeleteEntry_EntryOfTheAccount_Returns204AndRemovesIt()
    {
        var response = await Client.DeleteAsync(EntryUrl(MaeId, MaeEntryId));

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        StoredEntries().Should().NotContain(entry => entry.Id == Guid.Parse(MaeEntryId));
    }

    [Theory]
    [InlineData(BarclaysId, "GBP", "You", 10, "Barclays is not a Settlement account.")]
    [InlineData(PaiId, "GBP", "You", 10, "Pai is inactive.")]
    [InlineData(MaeId, "BRL", "Mãe", 10, "Paid by must be You or Other.")]
    [InlineData(MaeId, null, "You", 10, "Choose the currency.")]
    [InlineData(MaeId, "XYZ", "You", 10, "Unknown currency 'XYZ'.")]
    [InlineData(SamuelId, "BRL", "You", 10, "Samuel doesn't use BRL.")]
    [InlineData(MaeId, "BRL", "You", 0.004, "Amount must be greater than zero.")]
    public async Task AddEntry_InvalidRequest_Returns400WithTheMessageAndSavesNothing(string accountId, string? currency, string paidBy, decimal amount, string message)
    {
        var response = await Client.PostAsJsonAsync(EntriesUrl(accountId), NewEntry(currency, amount, paidBy));

        using (new AssertionScope())
        {
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            (await DetailOf(response)).Should().Be(message);
            StoredEntries().Should().HaveCount(3);
        }
    }

    [Fact]
    public async Task AddEntry_BlankDescriptionOrFutureDate_Returns400()
    {
        var blank = await Client.PostAsJsonAsync(EntriesUrl(MaeId), new SettlementEntryCreateDTO
        {
            Date = new DateOnly(2026, 9, 20),
            Description = " ",
            PaidBy = "You",
            Currency = "BRL",
            Amount = 10m
        });
        var future = await Client.PostAsJsonAsync(EntriesUrl(MaeId), NewEntry("BRL", date: TestClock.Today.AddDays(1)));

        using (new AssertionScope())
        {
            blank.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            (await DetailOf(blank)).Should().Be("Description is required.");
            future.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            (await DetailOf(future)).Should().Be("Date must not be in the future.");
        }
    }

    [Fact]
    public async Task EditEntry_InvalidRequest_Returns400WithTheMessage()
    {
        var future = await Client.PutAsJsonAsync(EntryUrl(MaeId, MaeEntryId), EntryUpdate(25.6m, TestClock.Today.AddDays(1)));
        var negative = await Client.PutAsJsonAsync(EntryUrl(MaeId, MaeEntryId), EntryUpdate(-1m));
        var oneCurrency = await Client.PutAsJsonAsync(EntryUrl(SamuelId, SamuelEntryId), EntryUpdate(0m));
        var inactive = await Client.PutAsJsonAsync(EntryUrl(PaiId, PaiEntryId), EntryUpdate(null));

        using (new AssertionScope())
        {
            (await DetailOf(future)).Should().Be("Date must not be in the future.");
            (await DetailOf(negative)).Should().Be("The converted value must be greater than zero.");
            (await DetailOf(oneCurrency)).Should().Be("Samuel has one currency, so an entry has no converted value.");
            (await DetailOf(inactive)).Should().Be("Pai is inactive.");
            new[] { future, negative, oneCurrency, inactive }.Should().OnlyContain(response => response.StatusCode == HttpStatusCode.BadRequest);
            StoredEntries().Single(entry => entry.Id == Guid.Parse(MaeEntryId)).Description.Should().Be("Farmácia");
        }
    }

    [Fact]
    public async Task Routes_UnknownAccountOrEntryOfAnotherAccount_Return404()
    {
        var unknownAccount = await Client.PostAsJsonAsync(EntriesUrl(Guid.NewGuid().ToString()), NewEntry("BRL"));
        var otherAccountsEdit = await Client.PutAsJsonAsync(EntryUrl(MaeId, SamuelEntryId), EntryUpdate(null));
        var otherAccountsDelete = await Client.DeleteAsync(EntryUrl(MaeId, SamuelEntryId));

        using (new AssertionScope())
        {
            unknownAccount.StatusCode.Should().Be(HttpStatusCode.NotFound);
            otherAccountsEdit.StatusCode.Should().Be(HttpStatusCode.NotFound);
            otherAccountsDelete.StatusCode.Should().Be(HttpStatusCode.NotFound);
            StoredEntries().Should().HaveCount(3);
        }
    }

    [Fact]
    public async Task DeleteEntry_InactiveAccount_Returns400AndKeepsIt()
    {
        var response = await Client.DeleteAsync(EntryUrl(PaiId, PaiEntryId));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await DetailOf(response)).Should().Be("Pai is inactive.");
        StoredEntries().Should().HaveCount(3);
    }

    [Fact]
    public async Task Routes_NullBody_Return400()
    {
        var add = await Client.PostAsync(EntriesUrl(MaeId), JsonContent.Create<SettlementEntryCreateDTO?>(null));
        var edit = await Client.PutAsync(EntryUrl(MaeId, MaeEntryId), JsonContent.Create<SettlementEntryUpdateDTO?>(null));

        add.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        edit.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    private static string SettleUrl(string accountId) => $"{Api}/accounts/{accountId}/settlements";

    private static SettleRequestDTO Settlement(decimal amount, decimal? convertedValue, string? currency = "BRL", DateOnly? date = null) => new()
    {
        Date = date ?? TestClock.Today,
        Description = "Settlement",
        PaidBy = "Other",
        Currency = currency,
        Amount = amount,
        ConvertedValue = convertedValue
    };

    private async Task<AccountDetailDTO> AccountDetail(string accountId) =>
        (await Client.GetFromJsonAsync<AccountDetailDTO>($"{Api}/accounts/{accountId}/2026/8"))!;

    private static IEnumerable<(string Currency, decimal Balance)> Figures(AccountDetailDTO detail) =>
        detail.CurrentBalances.Select(balance => (balance.Currency, balance.Balance));

    [Fact]
    [Trait("AC", "P60-F12-settlement-screen-01")]
    public async Task AccountDetail_Settlement_ReturnsCurrentBalancesPerCurrencyWhateverTheMonth()
    {
        var mae = await AccountDetail(MaeId);
        var samuel = await AccountDetail(SamuelId);
        var barclays = await AccountDetail(BarclaysId);

        using (new AssertionScope())
        {
            mae.Balance.Should().Be(0m);
            Figures(mae).Should().Equal(("BRL", 150m), ("GBP", 21.94m));
            mae.OtherParticipant.Should().Be("Mãe");
            mae.AllowedActions.Should().Equal("AddEntry", "Settle");
            mae.DisabledActions.Should().BeEmpty();
            Figures(samuel).Should().Equal(("GBP", 60m));
            samuel.OtherParticipant.Should().Be("Samuel");
            barclays.CurrentBalances.Should().BeEmpty();
            barclays.OtherParticipant.Should().BeNull();
            barclays.DisabledActions.Should().BeEmpty();
        }
    }

    [Fact]
    public async Task AccountDetail_InactiveSettlement_HasNoActions()
    {
        var pai = await AccountDetail(PaiId);

        pai.AllowedActions.Should().BeEmpty();
        pai.DisabledActions.Should().BeEmpty();
        Figures(pai).Should().Equal(("GBP", -15m));
    }

    [Fact]
    [Trait("AC", "P60-F12-settlement-screen-05")]
    public async Task Settle_PrefilledValues_LeaveEveryCurrencySettled()
    {
        var response = await Client.PostAsJsonAsync(SettleUrl(MaeId), Settlement(150m, 21.94m));
        var returned = await response.Content.ReadFromJsonAsync<SettlementEntryDTO>();
        var after = await AccountDetail(MaeId);

        using (new AssertionScope())
        {
            response.StatusCode.Should().Be(HttpStatusCode.Created);
            returned!.Should().BeEquivalentTo(new
            {
                AccountId = Guid.Parse(MaeId),
                Description = "Settlement",
                PaidBy = "Other",
                Currency = "BRL",
                Amount = 150m,
                ConvertedCurrency = "GBP",
                ConvertedValue = 21.94m,
                Warning = (string?)null
            });
            Figures(after).Should().Equal(("BRL", 0m), ("GBP", 0m));
        }
    }

    [Fact]
    [Trait("AC", "P60-F12-settlement-screen-06")]
    public async Task Settle_EditedValues_SaveAndLeaveTheRemainder()
    {
        var response = await Client.PostAsJsonAsync(SettleUrl(MaeId), Settlement(100m, null));
        var after = await AccountDetail(MaeId);

        using (new AssertionScope())
        {
            response.StatusCode.Should().Be(HttpStatusCode.Created);
            Figures(after).Should().Equal(("BRL", 50m), ("GBP", 21.94m));
            after.DisabledActions.Should().BeEmpty();
        }
    }

    [Fact]
    [Trait("AC", "P60-F12-settlement-screen-07")]
    public async Task AccountDetail_SettledAccount_DisablesSettleWithNothingToSettle()
    {
        await Client.PostAsJsonAsync(SettleUrl(MaeId), Settlement(150m, 21.94m));

        var after = await AccountDetail(MaeId);

        using (new AssertionScope())
        {
            after.AllowedActions.Should().Equal("AddEntry", "Settle");
            after.DisabledActions.Should().Equal(new Dictionary<string, string> { ["Settle"] = "Nothing to settle." });
        }
    }

    [Fact]
    [Trait("AC", "P60-F12-settlement-screen-07")]
    public async Task Settle_SettledAccount_Returns400NothingToSettle()
    {
        await Client.PostAsJsonAsync(SettleUrl(MaeId), Settlement(150m, 21.94m));

        var response = await Client.PostAsJsonAsync(SettleUrl(MaeId), Settlement(1m, null));

        using (new AssertionScope())
        {
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            (await DetailOf(response)).Should().Be("Nothing to settle.");
            StoredEntries().Should().HaveCount(4);
        }
    }

    [Theory]
    [InlineData(BarclaysId, "GBP", "Barclays is not a Settlement account.")]
    [InlineData(PaiId, "GBP", "Pai is inactive.")]
    [InlineData(MaeId, null, "Choose the currency.")]
    [InlineData(SamuelId, "GBP", "Samuel has one currency, so an entry has no converted value.")]
    public async Task Settle_InvalidRequest_Returns400WithTheMessageAndSavesNothing(string accountId, string? currency, string message)
    {
        var response = await Client.PostAsJsonAsync(SettleUrl(accountId), Settlement(10m, 1m, currency));

        using (new AssertionScope())
        {
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            (await DetailOf(response)).Should().Be(message);
            StoredEntries().Should().HaveCount(3);
        }
    }

    [Fact]
    public async Task Settle_FutureDateUnknownAccountOrNullBody_IsRefused()
    {
        var future = await Client.PostAsJsonAsync(SettleUrl(MaeId), Settlement(150m, 21.94m, date: TestClock.Today.AddDays(1)));
        var unknown = await Client.PostAsJsonAsync(SettleUrl(Guid.NewGuid().ToString()), Settlement(150m, 21.94m));
        var nullBody = await Client.PostAsync(SettleUrl(MaeId), JsonContent.Create<SettleRequestDTO?>(null));

        using (new AssertionScope())
        {
            future.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            (await DetailOf(future)).Should().Be("Date must not be in the future.");
            unknown.StatusCode.Should().Be(HttpStatusCode.NotFound);
            nullBody.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            StoredEntries().Should().HaveCount(3);
        }
    }
}
