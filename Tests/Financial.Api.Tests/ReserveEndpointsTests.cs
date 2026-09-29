using Financial.CashFlow.Application.DTOs;
using FluentAssertions;
using System.Net;
using System.Net.Http.Json;

namespace Financial.Api.Tests;

public class ReserveEndpointsTests : ApiEndpointTests
{
    private static readonly Guid InvestimentoId = Guid.Parse("8f3b1c1a-2e3a-4b1a-9a7f-300000000001");
    private static readonly Guid ArianaId = Guid.Parse("8f3b1c1a-2e3a-4b1a-9a7f-300000000003");
    private static readonly Guid ArianaIncomeSourceId = Guid.Parse("8f3b1c1a-2e3a-4b1a-9a7f-000000000002");
    private static readonly Guid ChaseId = Guid.Parse("8f3b1c1a-2e3a-4b1a-9a7f-100000000003");
    private static readonly Guid MercadoCategoryId = Guid.Parse("8f3b1c1a-2e3a-4b1a-9a7f-600000000008");
    private static readonly Guid InvestimentoCategoryId = Guid.Parse("8f3b1c1a-2e3a-4b1a-9a7f-600000000013");
    private static readonly Guid ReservaCategoryId = Guid.Parse("8f3b1c1a-2e3a-4b1a-9a7f-600000000014");

    [Fact]
    public async Task PostIncomeSplit_ValidRequest_ReturnsOkWithComputedSplit()
    {
        var request = new IncomeSplitRequestDTO
        {
            Date = new DateOnly(2026, 7, 1),
            Amount = 1963m,
            Description = "Ramsay"
        };

        var response = await Client.PostAsJsonAsync("/api/v1/financial/reserve/income-split", request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<IncomeSplitResultDTO>();
        result.Should().NotBeNull();
        result!.Buckets.Should().ContainSingle(b => b.BucketName == "Investimento" && b.Amount == 654.27m);
        result.Total.Should().Be(1963.00m);
    }

    [Fact]
    public async Task PostIncomeSplit_NonPositiveAmount_ReturnsBadRequestWithMessage()
    {
        var request = new IncomeSplitRequestDTO
        {
            Date = new DateOnly(2026, 7, 1),
            Amount = 0m,
            Description = "Ramsay"
        };

        var response = await Client.PostAsJsonAsync("/api/v1/financial/reserve/income-split", request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("greater than zero");
    }

    [Fact]
    public async Task PostIncomeSplit_MissingDescription_ReturnsBadRequestWithMessage()
    {
        var request = new IncomeSplitRequestDTO
        {
            Date = new DateOnly(2026, 7, 1),
            Amount = 1963m,
            Description = ""
        };

        var response = await Client.PostAsJsonAsync("/api/v1/financial/reserve/income-split", request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("Description is required");
    }

    [Fact]
    public async Task GetBucketBalances_ReflectsPostedIncomeSplit()
    {
        await Client.PostAsJsonAsync("/api/v1/financial/reserve/income-split", new IncomeSplitRequestDTO
        {
            Date = new DateOnly(2026, 7, 1),
            Amount = 1963m,
            Description = "Ramsay"
        });

        var response = await Client.GetAsync("/api/v1/financial/reserve/balances");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var balances = await response.Content.ReadFromJsonAsync<List<ReserveBucketBalanceDTO>>();
        balances.Should().HaveCount(4);
        balances.Should().ContainSingle(b => b.BucketName == "Investimento" && b.Balance == 654.27m);
        balances.Should().ContainSingle(b => b.BucketId == InvestimentoId && b.Balance == 654.27m);
    }

    [Fact]
    public async Task PostWithdrawal_ExceedingBalanceUnconfirmed_ReturnsConflictWithMessage()
    {
        await Client.PostAsJsonAsync("/api/v1/financial/reserve/income-split", new IncomeSplitRequestDTO
        {
            Date = new DateOnly(2026, 7, 1),
            Amount = 1963m,
            Description = "Ramsay"
        });

        var response = await Client.PostAsJsonAsync("/api/v1/financial/reserve/withdrawals", new WithdrawalRequestDTO
        {
            BucketId = ArianaId,
            Amount = 99999m,
            Date = new DateOnly(2026, 7, 2),
            Description = "Too much",
            Confirmed = false
        });

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("Ariana");
    }

    [Fact]
    public async Task PostWithdrawal_ExceedingBalanceConfirmed_ReturnsOk()
    {
        await Client.PostAsJsonAsync("/api/v1/financial/reserve/income-split", new IncomeSplitRequestDTO
        {
            Date = new DateOnly(2026, 7, 1),
            Amount = 1963m,
            Description = "Ramsay"
        });

        var response = await Client.PostAsJsonAsync("/api/v1/financial/reserve/withdrawals", new WithdrawalRequestDTO
        {
            BucketId = ArianaId,
            Amount = 99999m,
            Date = new DateOnly(2026, 7, 2),
            Description = "Too much but confirmed",
            Confirmed = true
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var movement = await response.Content.ReadFromJsonAsync<ReserveMovementDTO>();
        movement!.Amount.Should().Be(-99999m);
    }

    [Fact]
    public async Task PostWithdrawal_WithinBalance_UpdatesOnlyThatBucket()
    {
        await Client.PostAsJsonAsync("/api/v1/financial/reserve/income-split", new IncomeSplitRequestDTO
        {
            Date = new DateOnly(2026, 7, 1),
            Amount = 1963m,
            Description = "Ramsay"
        });
        var balancesBefore = await Client.GetFromJsonAsync<List<ReserveBucketBalanceDTO>>("/api/v1/financial/reserve/balances");
        var gleisonBefore = balancesBefore!.Single(b => b.BucketName == "Gleison").Balance;

        var response = await Client.PostAsJsonAsync("/api/v1/financial/reserve/withdrawals", new WithdrawalRequestDTO
        {
            BucketId = InvestimentoId,
            Amount = 100m,
            Date = new DateOnly(2026, 7, 2),
            Description = "Small withdrawal",
            Confirmed = false
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var balancesAfter = await Client.GetFromJsonAsync<List<ReserveBucketBalanceDTO>>("/api/v1/financial/reserve/balances");
        balancesAfter!.Single(b => b.BucketName == "Gleison").Balance.Should().Be(gleisonBefore);
    }

    [Fact]
    public async Task PostWithdrawal_WithBankAndCategory_CreatesMovementAndTwoExpenses()
    {
        var response = await Client.PostAsJsonAsync("/api/v1/financial/reserve/withdrawals", BankWithdrawal(confirmed: true));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var expenses = await Client.GetFromJsonAsync<List<ExpenseDTO>>("/api/v1/financial/expenses/month/2026/7");
        expenses.Should().HaveCount(2);
        expenses.Should().ContainSingle(e => e.CategoryId == ReservaCategoryId && e.Value == -250m && e.PaymentSourceBankId == ChaseId);
        expenses.Should().ContainSingle(e => e.CategoryId == MercadoCategoryId && e.Value == 250m && e.PaymentSourceBankId == ChaseId);
        var movements = await Client.GetFromJsonAsync<List<ReserveMovementDTO>>("/api/v1/financial/reserve/movements");
        movements.Should().ContainSingle(m => m.Amount == -250m);
    }

    [Fact]
    public async Task PostWithdrawal_WithoutBank_CreatesNoExpenses()
    {
        var response = await Client.PostAsJsonAsync(
            "/api/v1/financial/reserve/withdrawals",
            BankWithdrawal(withBank: false, withCategory: false, confirmed: true));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var expenses = await Client.GetFromJsonAsync<List<ExpenseDTO>>("/api/v1/financial/expenses/month/2026/7");
        expenses.Should().BeEmpty();
    }

    [Fact]
    public async Task PostWithdrawal_BankWithoutCategory_ReturnsBadRequestAndSavesNothing()
    {
        var response = await Client.PostAsJsonAsync("/api/v1/financial/reserve/withdrawals", BankWithdrawal(withCategory: false));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await response.Content.ReadAsStringAsync()).Should().Contain("Category is required when a bank is selected");
        await AssertNothingCreatedAsync();
    }

    [Theory]
    [InlineData("unknown")]
    [InlineData("investment")]
    [InlineData("reserva")]
    public async Task PostWithdrawal_InvalidCategory_ReturnsBadRequestAndSavesNothing(string kind)
    {
        var categoryId = kind switch
        {
            "investment" => InvestimentoCategoryId,
            "reserva" => ReservaCategoryId,
            _ => Guid.NewGuid()
        };

        var response = await Client.PostAsJsonAsync("/api/v1/financial/reserve/withdrawals", BankWithdrawal(categoryId: categoryId));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        await AssertNothingCreatedAsync();
    }

    [Fact]
    public async Task PostWithdrawal_BankPathOverdraftUnconfirmed_ReturnsConflictAndSavesNothing()
    {
        var response = await Client.PostAsJsonAsync("/api/v1/financial/reserve/withdrawals", BankWithdrawal(amount: 99999m));

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        await AssertNothingCreatedAsync();
    }

    private static WithdrawalRequestDTO BankWithdrawal(
        bool withBank = true, bool withCategory = true, Guid? categoryId = null, decimal amount = 250m, bool confirmed = false) =>
        new()
        {
            BucketId = ArianaId,
            Amount = amount,
            Date = new DateOnly(2026, 7, 2),
            Description = "Car service",
            Confirmed = confirmed,
            PaymentSourceBankId = withBank ? ChaseId : null,
            ExpenseCategoryId = withCategory ? categoryId ?? MercadoCategoryId : null
        };

    private async Task AssertNothingCreatedAsync()
    {
        var expenses = await Client.GetFromJsonAsync<List<ExpenseDTO>>("/api/v1/financial/expenses/month/2026/7");
        var movements = await Client.GetFromJsonAsync<List<ReserveMovementDTO>>("/api/v1/financial/reserve/movements");
        expenses.Should().BeEmpty();
        movements.Should().BeEmpty();
    }

    [Fact]
    public async Task GetMovementHistory_ReturnsAllPostedMovements()
    {
        await Client.PostAsJsonAsync("/api/v1/financial/reserve/income-split", new IncomeSplitRequestDTO
        {
            Date = new DateOnly(2026, 7, 1),
            Amount = 1963m,
            Description = "Ramsay"
        });

        var response = await Client.GetAsync("/api/v1/financial/reserve/movements");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var movements = await response.Content.ReadFromJsonAsync<List<ReserveMovementDTO>>();
        movements.Should().HaveCount(4);
        movements.Should().OnlyContain(m => m.Description == "Ramsay");
    }

    [Fact]
    public async Task UpdateMovement_ExistingId_ReturnsOkAndUpdatesFields()
    {
        var withdrawal = await Client.PostAsJsonAsync("/api/v1/financial/reserve/withdrawals", new WithdrawalRequestDTO
        {
            BucketId = ArianaId,
            Amount = 30m,
            Date = new DateOnly(2026, 7, 2),
            Description = "Groceries",
            Confirmed = true
        });
        var movement = await withdrawal.Content.ReadFromJsonAsync<ReserveMovementDTO>();

        var response = await Client.PutAsJsonAsync($"/api/v1/financial/reserve/movements/{movement!.Id}", new ReserveMovementUpdateDTO
        {
            BucketId = ArianaId,
            Amount = -45m,
            Date = new DateOnly(2026, 7, 3),
            Description = "Groceries (corrected)"
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var updated = await response.Content.ReadFromJsonAsync<ReserveMovementDTO>();
        updated!.Amount.Should().Be(-45m);
        updated.Description.Should().Be("Groceries (corrected)");
    }

    [Fact]
    public async Task UpdateMovement_UnknownId_ReturnsNotFound()
    {
        var response = await Client.PutAsJsonAsync($"/api/v1/financial/reserve/movements/{Guid.NewGuid()}", new ReserveMovementUpdateDTO
        {
            BucketId = ArianaId,
            Amount = 10m,
            Date = new DateOnly(2026, 7, 1),
            Description = "Test"
        });

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteMovement_MovementFromASplit_DeletesAllFourLines()
    {
        await Client.PostAsJsonAsync("/api/v1/financial/reserve/income-split", new IncomeSplitRequestDTO
        {
            Date = new DateOnly(2026, 7, 1),
            Amount = 1963m,
            Description = "Ramsay"
        });
        var movements = await Client.GetFromJsonAsync<List<ReserveMovementDTO>>("/api/v1/financial/reserve/movements");
        var oneLineOfTheSplit = movements!.First();

        var response = await Client.DeleteAsync($"/api/v1/financial/reserve/movements/{oneLineOfTheSplit.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var remaining = await Client.GetFromJsonAsync<List<ReserveMovementDTO>>("/api/v1/financial/reserve/movements");
        remaining.Should().BeEmpty();
    }

    [Fact]
    public async Task DeleteMovement_UnknownId_ReturnsNotFound()
    {
        var response = await Client.DeleteAsync($"/api/v1/financial/reserve/movements/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    private async Task<Guid> CreateLinkedMovementIdAsync()
    {
        var income = await Client.PostAsJsonAsync("/api/v1/financial/incomes", new IncomeCreateDTO
        {
            Date = new DateOnly(2026, 7, 25),
            IncomeSourceId = ArianaIncomeSourceId,
            GrossValue = null,
            NetValue = 2450.00m,
            BankId = ChaseId,
            SplitToReserve = true
        });
        var incomeDto = await income.Content.ReadFromJsonAsync<IncomeDTO>();
        var movements = await Client.GetFromJsonAsync<List<ReserveMovementDTO>>("/api/v1/financial/reserve/movements");
        return movements!.First(m => m.IncomeId == incomeDto!.Id).Id;
    }

    [Fact]
    public async Task UpdateMovement_OnLinkedMovement_ReturnsConflict()
    {
        var linkedMovementId = await CreateLinkedMovementIdAsync();

        var response = await Client.PutAsJsonAsync($"/api/v1/financial/reserve/movements/{linkedMovementId}", new ReserveMovementUpdateDTO
        {
            BucketId = ArianaId,
            Amount = 10m,
            Date = new DateOnly(2026, 7, 1),
            Description = "Attempted direct edit"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("linked to an income");
    }

    [Fact]
    public async Task DeleteMovement_OnLinkedMovement_ReturnsConflict()
    {
        var linkedMovementId = await CreateLinkedMovementIdAsync();

        var response = await Client.DeleteAsync($"/api/v1/financial/reserve/movements/{linkedMovementId}");

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("linked to an income");
    }

    [Fact]
    public async Task PostIncomeSplit_StillCreatesUnlinkedMovements()
    {
        var response = await Client.PostAsJsonAsync("/api/v1/financial/reserve/income-split", new IncomeSplitRequestDTO
        {
            Date = new DateOnly(2026, 7, 1),
            Amount = 1963m,
            Description = "Ramsay"
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var movements = await Client.GetFromJsonAsync<List<ReserveMovementDTO>>("/api/v1/financial/reserve/movements");
        movements.Should().OnlyContain(m => m.IncomeId == null);
    }
}
