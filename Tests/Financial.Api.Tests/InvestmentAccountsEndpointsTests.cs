using Financial.CashFlow.Application.DTOs;
using FluentAssertions;
using System.Net;
using System.Net.Http.Json;

namespace Financial.Api.Tests;

public class InvestmentAccountsEndpointsTests : ApiEndpointTests
{
    private static readonly Guid ChaseSaveId = Guid.Parse("8f3b1c1a-2e3a-4b1a-9a7f-200000000001");
    private static readonly Guid PlatinumVisa8003Id = Guid.Parse("8f3b1c1a-2e3a-4b1a-9a7f-200000000002");

    [Fact]
    public async Task GetInvestmentAccounts_ReturnsTheElevenSeededAccountsWithCorrectFields()
    {
        var response = await Client.GetAsync("/api/v1/financial/investment-accounts");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var accounts = await response.Content.ReadFromJsonAsync<List<InvestmentAccountDTO>>();
        accounts.Should().HaveCount(11);
        accounts.Should().ContainSingle(a => a.Name == "ChaseSave" && a.IsActive && !a.IsLiability && a.Id != Guid.Empty);
        accounts.Should().ContainSingle(a => a.Name == "PlatinumVisa8003" && a.IsActive && a.IsLiability);
    }

    [Fact]
    public async Task GetInvestmentAccounts_RequiresNoParameters_AndReturnsFullUnfilteredList()
    {
        var response = await Client.GetAsync("/api/v1/financial/investment-accounts");
        var accounts = await response.Content.ReadFromJsonAsync<List<InvestmentAccountDTO>>();

        // All 11 seeded accounts come back regardless of IsActive/IsLiability value.
        accounts.Should().HaveCount(11);
        accounts.Should().OnlyContain(a => a.IsActive);
    }

    [Fact]
    public async Task GetInvestmentAccounts_WithNoSnapshot_ReturnsZeroLatestBalance()
    {
        var response = await Client.GetAsync("/api/v1/financial/investment-accounts");
        var accounts = await response.Content.ReadFromJsonAsync<List<InvestmentAccountDTO>>();

        accounts.Should().ContainSingle(a => a.Id == ChaseSaveId && a.LatestBalance == 0m);
    }

    [Fact]
    public async Task CreateInvestmentAccount_ValidRequest_ReturnsOkWithNewAccount()
    {
        var response = await Client.PostAsJsonAsync("/api/v1/financial/investment-accounts", new InvestmentAccountCreateDTO
        {
            Name = "Monzo Pot",
            IsActive = true,
            IsLiability = false
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var account = await response.Content.ReadFromJsonAsync<InvestmentAccountDTO>();
        account!.Name.Should().Be("Monzo Pot");
        account.LatestBalance.Should().Be(0m);
        account.Id.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public async Task CreateInvestmentAccount_DuplicateName_ReturnsConflictWithMessage()
    {
        var response = await Client.PostAsJsonAsync("/api/v1/financial/investment-accounts", new InvestmentAccountCreateDTO
        {
            Name = "ChaseSave",
            IsActive = true,
            IsLiability = false
        });

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("ChaseSave").And.Contain("already exists");
    }

    [Fact]
    public async Task CreateInvestmentAccount_BlankName_ReturnsBadRequest()
    {
        var response = await Client.PostAsJsonAsync("/api/v1/financial/investment-accounts", new InvestmentAccountCreateDTO
        {
            Name = "   ",
            IsActive = true,
            IsLiability = false
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task UpdateInvestmentAccount_ValidRequest_ReturnsOkAndUpdatesFields()
    {
        var response = await Client.PutAsJsonAsync($"/api/v1/financial/investment-accounts/{PlatinumVisa8003Id}", new InvestmentAccountUpdateDTO
        {
            Name = "PlatinumVisa8003Renamed",
            IsActive = false,
            IsLiability = true
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var account = await response.Content.ReadFromJsonAsync<InvestmentAccountDTO>();
        account!.Name.Should().Be("PlatinumVisa8003Renamed");
        account.IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task UpdateInvestmentAccount_UnknownId_ReturnsNotFound()
    {
        var response = await Client.PutAsJsonAsync($"/api/v1/financial/investment-accounts/{Guid.NewGuid()}", new InvestmentAccountUpdateDTO
        {
            Name = "X",
            IsActive = true,
            IsLiability = false
        });

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task UpdateInvestmentAccount_NameCollidesWithAnotherAccount_ReturnsConflict()
    {
        var response = await Client.PutAsJsonAsync($"/api/v1/financial/investment-accounts/{PlatinumVisa8003Id}", new InvestmentAccountUpdateDTO
        {
            Name = "ChaseSave",
            IsActive = true,
            IsLiability = false
        });

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task DeleteInvestmentAccount_WithNoSnapshot_ReturnsOkAndRemovesIt()
    {
        var created = await Client.PostAsJsonAsync("/api/v1/financial/investment-accounts", new InvestmentAccountCreateDTO
        {
            Name = "Monzo Pot",
            IsActive = true,
            IsLiability = false
        });
        var account = await created.Content.ReadFromJsonAsync<InvestmentAccountDTO>();

        var response = await Client.DeleteAsync($"/api/v1/financial/investment-accounts/{account!.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var accounts = await (await Client.GetAsync("/api/v1/financial/investment-accounts")).Content.ReadFromJsonAsync<List<InvestmentAccountDTO>>();
        accounts.Should().NotContain(a => a.Id == account.Id);
    }

    [Fact]
    public async Task DeleteInvestmentAccount_UnknownId_ReturnsNotFound()
    {
        var response = await Client.DeleteAsync($"/api/v1/financial/investment-accounts/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteInvestmentAccount_WithNonZeroLatestSnapshot_ReturnsConflictWithMessage()
    {
        var snapshotsResponse = await Client.GetAsync("/api/v1/financial/investment-snapshots/2026/7");
        var snapshots = await snapshotsResponse.Content.ReadFromJsonAsync<List<InvestmentSnapshotDTO>>();
        var target = snapshots!.Single(s => s.AccountId == ChaseSaveId);
        await Client.PutAsJsonAsync($"/api/v1/financial/investment-snapshots/{target.Id}", new InvestmentSnapshotValueUpdateDTO { Value = 500m });

        var response = await Client.DeleteAsync($"/api/v1/financial/investment-accounts/{ChaseSaveId}");

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("non-zero balance");
    }

    [Fact]
    public async Task GetInvestmentAccounts_LatestBalance_ReflectsANonZeroSnapshotValue()
    {
        var snapshotsResponse = await Client.GetAsync("/api/v1/financial/investment-snapshots/2026/7");
        var snapshots = await snapshotsResponse.Content.ReadFromJsonAsync<List<InvestmentSnapshotDTO>>();
        var target = snapshots!.Single(s => s.AccountId == ChaseSaveId);
        await Client.PutAsJsonAsync($"/api/v1/financial/investment-snapshots/{target.Id}", new InvestmentSnapshotValueUpdateDTO { Value = 500m });

        var response = await Client.GetAsync("/api/v1/financial/investment-accounts");

        var accounts = await response.Content.ReadFromJsonAsync<List<InvestmentAccountDTO>>();
        accounts.Should().ContainSingle(a => a.Id == ChaseSaveId && a.LatestBalance == 500m);
    }
}
