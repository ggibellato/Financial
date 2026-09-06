using Financial.CashFlow.Application.DTOs;
using FluentAssertions;
using System.Net;
using System.Net.Http.Json;

namespace Financial.Api.Tests;

public class InvestmentSnapshotsEndpointsTests : ApiEndpointTests
{
    [Fact]
    public async Task GetSnapshotsForMonth_FirstCall_GeneratesElevenAccounts()
    {
        var response = await Client.GetAsync("/api/v1/financial/investment-snapshots/2026/7");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var snapshots = await response.Content.ReadFromJsonAsync<List<InvestmentSnapshotDTO>>();
        snapshots.Should().HaveCount(11);
        snapshots.Should().OnlyContain(s => s.Value == 0m);
        snapshots!.Count(s => s.IsLiability).Should().Be(6);
    }

    [Fact]
    public async Task GetSnapshotsForMonth_SecondCall_DoesNotDuplicateSnapshots()
    {
        await Client.GetAsync("/api/v1/financial/investment-snapshots/2026/7");

        var response = await Client.GetAsync("/api/v1/financial/investment-snapshots/2026/7");

        var snapshots = await response.Content.ReadFromJsonAsync<List<InvestmentSnapshotDTO>>();
        snapshots.Should().HaveCount(11);
    }

    [Fact]
    public async Task UpdateSnapshotValue_ExistingId_ReturnsOkAndUpdatesOnlyThatSnapshot()
    {
        var monthResponse = await Client.GetAsync("/api/v1/financial/investment-snapshots/2026/7");
        var snapshots = await monthResponse.Content.ReadFromJsonAsync<List<InvestmentSnapshotDTO>>();
        var target = snapshots!.First(s => s.AccountName == "ChaseSave");

        var response = await Client.PutAsJsonAsync($"/api/v1/financial/investment-snapshots/{target.Id}", new InvestmentSnapshotValueUpdateDTO
        {
            Value = 500m
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var updated = await response.Content.ReadFromJsonAsync<InvestmentSnapshotDTO>();
        updated!.Value.Should().Be(500m);

        var refetch = await Client.GetAsync("/api/v1/financial/investment-snapshots/2026/7");
        var refetched = await refetch.Content.ReadFromJsonAsync<List<InvestmentSnapshotDTO>>();
        refetched!.Where(s => s.AccountName != "ChaseSave").Should().OnlyContain(s => s.Value == 0m);
    }

    [Fact]
    public async Task UpdateSnapshotValue_NegativeValue_ReturnsBadRequestWithMessage()
    {
        var monthResponse = await Client.GetAsync("/api/v1/financial/investment-snapshots/2026/7");
        var snapshots = await monthResponse.Content.ReadFromJsonAsync<List<InvestmentSnapshotDTO>>();
        var target = snapshots!.First();

        var response = await Client.PutAsJsonAsync($"/api/v1/financial/investment-snapshots/{target.Id}", new InvestmentSnapshotValueUpdateDTO
        {
            Value = -10m
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("negative");
    }

    [Fact]
    public async Task UpdateSnapshotValue_UnknownId_ReturnsNotFound()
    {
        var response = await Client.PutAsJsonAsync($"/api/v1/financial/investment-snapshots/{Guid.NewGuid()}", new InvestmentSnapshotValueUpdateDTO
        {
            Value = 10m
        });

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetSuggestionsForMonth_SeededCreditCardAndReserveAccounts_ReturnsExpectedShape()
    {
        var accounts = await (await Client.GetAsync("/api/v1/financial/investment-accounts"))
            .Content.ReadFromJsonAsync<List<InvestmentAccountDTO>>();
        var platinumVisa = accounts!.First(a => a.Name == "PlatinumVisa8003");
        var reservasPessoais = accounts!.First(a => a.Name == "ReservasPessoais");
        var chaseSave = accounts!.First(a => a.Name == "ChaseSave");

        var cards = await (await Client.GetAsync("/api/v1/financial/credit-cards"))
            .Content.ReadFromJsonAsync<List<CreditCardDTO>>();
        var card = cards!.First(c => c.Name == "BarclaysPlatinumVisa8003");

        var categories = await (await Client.GetAsync("/api/v1/financial/categories"))
            .Content.ReadFromJsonAsync<List<CategoryDTO>>();
        var mercado = categories!.First(c => c.Name == "Mercado");

        await Client.PutAsJsonAsync($"/api/v1/financial/investment-accounts/{platinumVisa.Id}", new InvestmentAccountUpdateDTO
        {
            Name = platinumVisa.Name,
            IsActive = platinumVisa.IsActive,
            IsLiability = platinumVisa.IsLiability,
            Source = "CreditCard",
            CreditCardId = card.Id
        });

        await Client.PutAsJsonAsync($"/api/v1/financial/investment-accounts/{reservasPessoais.Id}", new InvestmentAccountUpdateDTO
        {
            Name = reservasPessoais.Name,
            IsActive = reservasPessoais.IsActive,
            IsLiability = reservasPessoais.IsLiability,
            Source = "ReserveBucketsSum",
            CreditCardId = null
        });

        await Client.PostAsJsonAsync("/api/v1/financial/expenses", new ExpenseCreateDTO
        {
            Date = new DateOnly(2026, 8, 5),
            Description = "Groceries",
            Value = 142.17m,
            CategoryId = mercado.Id,
            CreditCardId = card.Id,
            InvoiceDate = new DateOnly(2026, 8, 1)
        });

        await Client.PostAsJsonAsync("/api/v1/financial/reserve/income-split", new IncomeSplitRequestDTO
        {
            Date = new DateOnly(2026, 7, 15),
            Amount = 100m,
            Description = "Test income"
        });

        var response = await Client.GetAsync("/api/v1/financial/investment-snapshots/2026/8/suggestions");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<InvestmentSnapshotSuggestionsDTO>();

        var cardSuggestion = result!.Suggestions.Should().ContainSingle(s => s.AccountId == platinumVisa.Id).Subject;
        cardSuggestion.SuggestedValue.Should().Be(142.17m);
        cardSuggestion.SourceDescription.Should().Contain("BarclaysPlatinumVisa8003");

        var reserveSuggestion = result.Suggestions.Should().ContainSingle(s => s.AccountId == reservasPessoais.Id).Subject;
        reserveSuggestion.SuggestedValue.Should().Be(100m);

        result.Suggestions.Should().NotContain(s => s.AccountId == chaseSave.Id);
        result.NotUpdated.Should().NotContain(s => s.AccountId == chaseSave.Id);
    }
}
