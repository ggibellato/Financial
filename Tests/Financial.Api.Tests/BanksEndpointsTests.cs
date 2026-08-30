using Financial.CashFlow.Application.DTOs;
using FluentAssertions;
using System.Net;
using System.Net.Http.Json;

namespace Financial.Api.Tests;

public class BanksEndpointsTests : ApiEndpointTests
{
    private static readonly Guid BarclaysId = Guid.Parse("8f3b1c1a-2e3a-4b1a-9a7f-100000000001");
    private static readonly Guid Trading212Id = Guid.Parse("8f3b1c1a-2e3a-4b1a-9a7f-100000000002");
    private static readonly Guid ChaseId = Guid.Parse("8f3b1c1a-2e3a-4b1a-9a7f-100000000003");
    private static readonly Guid GleisonId = Guid.Parse("8f3b1c1a-2e3a-4b1a-9a7f-000000000001");
    private static readonly Guid MercadoId = Guid.Parse("8f3b1c1a-2e3a-4b1a-9a7f-600000000008");

    [Fact]
    public async Task GetBanks_ReturnsTheThreeSeededBanksWithCorrectRoundUpFlags()
    {
        var response = await Client.GetAsync("/api/v1/financial/banks");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var banks = await response.Content.ReadFromJsonAsync<List<BankDTO>>();
        banks.Should().HaveCount(3);
        banks.Should().OnlyContain(b => b.Id != Guid.Empty);
        banks.Should().ContainSingle(b => b.Id == BarclaysId && b.Name == "Barclays" && !b.RoundUpEnabled);
        banks.Should().ContainSingle(b => b.Id == Trading212Id && b.Name == "Trading212" && b.RoundUpEnabled);
        banks.Should().ContainSingle(b => b.Id == ChaseId && b.Name == "Chase" && b.RoundUpEnabled);
    }

    [Fact]
    public async Task CreateBank_ValidRequest_ReturnsOkWithNewBank()
    {
        var response = await Client.PostAsJsonAsync("/api/v1/financial/banks", new BankCreateDTO { Name = "Monzo", RoundUpEnabled = true });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var bank = await response.Content.ReadFromJsonAsync<BankDTO>();
        bank!.Name.Should().Be("Monzo");
        bank.RoundUpEnabled.Should().BeTrue();
        bank.Id.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public async Task CreateBank_DuplicateName_ReturnsConflictWithMessage()
    {
        var response = await Client.PostAsJsonAsync("/api/v1/financial/banks", new BankCreateDTO { Name = "Barclays", RoundUpEnabled = false });

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("Barclays").And.Contain("already exists");
    }

    [Fact]
    public async Task CreateBank_BlankName_ReturnsBadRequest()
    {
        var response = await Client.PostAsJsonAsync("/api/v1/financial/banks", new BankCreateDTO { Name = "   ", RoundUpEnabled = false });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task UpdateBank_ValidRequest_ReturnsOkAndUpdatesFields()
    {
        var response = await Client.PutAsJsonAsync($"/api/v1/financial/banks/{ChaseId}", new BankUpdateDTO { Name = "Chase Renamed", RoundUpEnabled = false });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var bank = await response.Content.ReadFromJsonAsync<BankDTO>();
        bank!.Name.Should().Be("Chase Renamed");
        bank.RoundUpEnabled.Should().BeFalse();
    }

    [Fact]
    public async Task UpdateBank_UnknownId_ReturnsNotFound()
    {
        var response = await Client.PutAsJsonAsync($"/api/v1/financial/banks/{Guid.NewGuid()}", new BankUpdateDTO { Name = "X", RoundUpEnabled = false });

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task UpdateBank_NameCollidesWithAnotherBank_ReturnsConflict()
    {
        var response = await Client.PutAsJsonAsync($"/api/v1/financial/banks/{ChaseId}", new BankUpdateDTO { Name = "Barclays", RoundUpEnabled = false });

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task DeleteBank_WithNoReferences_ReturnsOkAndRemovesIt()
    {
        var created = await Client.PostAsJsonAsync("/api/v1/financial/banks", new BankCreateDTO { Name = "Monzo", RoundUpEnabled = false });
        var bank = await created.Content.ReadFromJsonAsync<BankDTO>();

        var response = await Client.DeleteAsync($"/api/v1/financial/banks/{bank!.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var banks = await (await Client.GetAsync("/api/v1/financial/banks")).Content.ReadFromJsonAsync<List<BankDTO>>();
        banks.Should().NotContain(b => b.Id == bank.Id);
    }

    [Fact]
    public async Task DeleteBank_UnknownId_ReturnsNotFound()
    {
        var response = await Client.DeleteAsync($"/api/v1/financial/banks/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteBank_ReferencedByBalanceAdjustment_ReturnsConflictWithMessage()
    {
        await Client.PostAsJsonAsync($"/api/v1/financial/banks/{BarclaysId}/adjustments", new BalanceAdjustmentCreateDTO
        {
            Date = new DateOnly(2026, 7, 5),
            TargetBalance = 150m
        });

        var response = await Client.DeleteAsync($"/api/v1/financial/banks/{BarclaysId}");

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("balance history or transactions");
    }

    [Fact]
    public async Task DeleteBank_ReferencedByTransfer_ReturnsConflict()
    {
        await Client.PostAsJsonAsync("/api/v1/financial/transfers", new TransferCreateDTO
        {
            Date = new DateOnly(2026, 7, 5),
            SourceBankId = BarclaysId,
            DestinationBankId = Trading212Id,
            Amount = 500m
        });

        var response = await Client.DeleteAsync($"/api/v1/financial/banks/{Trading212Id}");

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task UpdateOpeningBalance_ValidRequest_ReturnsOkAndUpdatesFields()
    {
        var request = new BankOpeningBalanceUpdateDTO
        {
            OpeningBalance = 1250.75m,
            OpeningBalanceDate = new DateOnly(2026, 7, 1)
        };

        var response = await Client.PutAsJsonAsync($"/api/v1/financial/banks/{BarclaysId}/opening-balance", request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var bank = await response.Content.ReadFromJsonAsync<BankDTO>();
        bank!.OpeningBalance.Should().Be(1250.75m);
        bank.OpeningBalanceDate.Should().Be(new DateOnly(2026, 7, 1));
    }

    [Fact]
    public async Task UpdateOpeningBalance_NegativeBalance_ReturnsBadRequest()
    {
        var request = new BankOpeningBalanceUpdateDTO
        {
            OpeningBalance = -1m,
            OpeningBalanceDate = new DateOnly(2026, 7, 1)
        };

        var response = await Client.PutAsJsonAsync($"/api/v1/financial/banks/{BarclaysId}/opening-balance", request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task UpdateOpeningBalance_UnknownBankId_ReturnsNotFound()
    {
        var request = new BankOpeningBalanceUpdateDTO
        {
            OpeningBalance = 10m,
            OpeningBalanceDate = new DateOnly(2026, 7, 1)
        };

        var response = await Client.PutAsJsonAsync($"/api/v1/financial/banks/{Guid.NewGuid()}/opening-balance", request);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task UpdateOpeningBalance_ThenGetBanks_ReflectsTheUpdate()
    {
        await Client.PutAsJsonAsync($"/api/v1/financial/banks/{ChaseId}/opening-balance", new BankOpeningBalanceUpdateDTO
        {
            OpeningBalance = 500m,
            OpeningBalanceDate = new DateOnly(2026, 6, 15)
        });

        var response = await Client.GetAsync("/api/v1/financial/banks");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var banks = await response.Content.ReadFromJsonAsync<List<BankDTO>>();
        banks.Should().ContainSingle(b => b.Name == "Chase" && b.OpeningBalance == 500m && b.OpeningBalanceDate == new DateOnly(2026, 6, 15));
    }

    [Fact]
    public async Task GetBankBalancesByMonth_CombinesOpeningBalanceIncomeAndExpenses()
    {
        await Client.PutAsJsonAsync($"/api/v1/financial/banks/{BarclaysId}/opening-balance", new BankOpeningBalanceUpdateDTO
        {
            OpeningBalance = 100m,
            OpeningBalanceDate = new DateOnly(2026, 1, 1)
        });
        await Client.PostAsJsonAsync("/api/v1/financial/incomes", new IncomeCreateDTO
        {
            Date = new DateOnly(2026, 7, 1),
            IncomeSourceId = GleisonId,
            GrossValue = null,
            NetValue = 500m,
            BankId = BarclaysId
        });
        await Client.PostAsJsonAsync("/api/v1/financial/expenses", new ExpenseCreateDTO
        {
            Date = new DateOnly(2026, 7, 5),
            Description = "Groceries",
            Value = 50m,
            CategoryId = MercadoId,
            PaymentSourceBankId = BarclaysId,
            CreditCardId = null
        });

        var response = await Client.GetAsync("/api/v1/financial/banks/month/2026/7/balances");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var balances = await response.Content.ReadFromJsonAsync<List<BankBalanceDTO>>();
        balances.Should().ContainSingle(b => b.Bank == "Barclays" && b.Balance == 550m);
    }

    [Fact]
    public async Task GetBankBalancesByMonth_WithNoActivity_ReturnsOpeningBalanceForEveryBank()
    {
        var response = await Client.GetAsync("/api/v1/financial/banks/month/2026/7/balances");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var balances = await response.Content.ReadFromJsonAsync<List<BankBalanceDTO>>();
        balances.Should().HaveCount(3);
        balances.Should().OnlyContain(b => b.Balance == 0m);
    }

    [Fact]
    public async Task GetBankBalancesByMonth_ReflectsATransferForBothSourceAndDestinationBanks()
    {
        await Client.PutAsJsonAsync($"/api/v1/financial/banks/{BarclaysId}/opening-balance", new BankOpeningBalanceUpdateDTO
        {
            OpeningBalance = 1000m,
            OpeningBalanceDate = new DateOnly(2026, 1, 1)
        });
        await Client.PostAsJsonAsync("/api/v1/financial/transfers", new TransferCreateDTO
        {
            Date = new DateOnly(2026, 7, 5),
            SourceBankId = BarclaysId,
            DestinationBankId = Trading212Id,
            Amount = 500m
        });

        var response = await Client.GetAsync("/api/v1/financial/banks/month/2026/7/balances");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var balances = await response.Content.ReadFromJsonAsync<List<BankBalanceDTO>>();
        balances.Should().ContainSingle(b => b.Bank == "Barclays" && b.Balance == 500m);
        balances.Should().ContainSingle(b => b.Bank == "Trading212" && b.Balance == 500m);
    }

    [Fact]
    public async Task GetBankBalancesByMonth_ReflectsABalanceAdjustment()
    {
        await Client.PutAsJsonAsync($"/api/v1/financial/banks/{BarclaysId}/opening-balance", new BankOpeningBalanceUpdateDTO
        {
            OpeningBalance = 100m,
            OpeningBalanceDate = new DateOnly(2026, 1, 1)
        });
        await Client.PostAsJsonAsync($"/api/v1/financial/banks/{BarclaysId}/adjustments", new BalanceAdjustmentCreateDTO
        {
            Date = new DateOnly(2026, 7, 5),
            TargetBalance = 150m
        });

        var response = await Client.GetAsync("/api/v1/financial/banks/month/2026/7/balances");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var balances = await response.Content.ReadFromJsonAsync<List<BankBalanceDTO>>();
        balances.Should().ContainSingle(b => b.Bank == "Barclays" && b.Balance == 150m);
    }
}
