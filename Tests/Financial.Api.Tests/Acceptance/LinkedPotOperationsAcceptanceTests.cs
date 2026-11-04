using System.Net;
using System.Net.Http.Json;
using Financial.CashFlow.Application.DTOs;
using Financial.CashFlow.Application.Interfaces;
using Financial.CashFlow.Domain.Entities.Transactions;
using FluentAssertions;
using FluentAssertions.Execution;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

namespace Financial.Api.Tests.Acceptance;

[Trait("Category", "Integration")]
public class LinkedPotOperationsAcceptanceTests : ApiEndpointTests
{
    private const string Api = "/api/v1/financial";
    private const string BarclaysId = "9a1d0000-0000-0000-0000-000000000001";
    private const string ReservaId = "9a1d0000-0000-0000-0000-000000000002";
    private const string ChaseId = "9a1d0000-0000-0000-0000-000000000003";
    private const string InvestimentoId = "9a1d0000-0000-0000-0000-000000000101";
    private const string HouseTreatsId = "9a1d0000-0000-0000-0000-000000000102";
    private const string HolidaysId = "9a1d0000-0000-0000-0000-000000000103";
    private const string ReservaCategoryId = "9a1d0000-0000-0000-0000-000000000301";
    private const string MercadoId = "9a1d0000-0000-0000-0000-000000000302";
    private const string LegacyDepositId = "9a1d0000-0000-0000-0000-000000000201";
    private const string LegacyWithdrawalId = "9a1d0000-0000-0000-0000-000000000202";
    private const string ExpenseMessage = "This expense belongs to a pot deposit or withdrawal; edit it from the Pots account.";
    private const string MovementMessage = "This movement belongs to a pot deposit or withdrawal; edit it from the Pots account.";

    private const string SeededData = $$"""
        {
          "Accounts": [
            { "Id": "{{BarclaysId}}", "Name": "Barclays", "Template": "Bank", "Currency": "GBP", "IsActive": true, "OpeningBalance": { "Amount": 1000, "StartDate": "2026-01-01" }, "RoundUp": { "Enabled": false }, "Pots": null },
            { "Id": "{{ChaseId}}", "Name": "Chase", "Template": "Bank", "Currency": "GBP", "IsActive": true, "OpeningBalance": { "Amount": 1000, "StartDate": "2026-01-01" }, "RoundUp": { "Enabled": false }, "Pots": null },
            { "Id": "{{ReservaId}}", "Name": "Reserva", "Template": "Pots", "Currency": "GBP", "IsActive": true, "OpeningBalance": null, "RoundUp": null, "Pots": [
              { "Id": "{{InvestimentoId}}", "Name": "Investimento", "IsActive": true, "SplitPercentage": 60 },
              { "Id": "{{HouseTreatsId}}", "Name": "HouseTreats", "IsActive": true, "SplitPercentage": 40 },
              { "Id": "{{HolidaysId}}", "Name": "Holidays", "IsActive": false, "SplitPercentage": 0 }
            ], "CategoryId": "{{ReservaCategoryId}}" }
          ],
          "Categories": [
            { "Id": "{{ReservaCategoryId}}", "Name": "Reserva", "Active": true, "IsInvestment": false, "IsTithe": false },
            { "Id": "{{MercadoId}}", "Name": "Mercado", "Active": true, "IsInvestment": false, "IsTithe": false }
          ],
          "ReserveMovements": [
            { "Id": "{{LegacyDepositId}}", "BucketId": "{{InvestimentoId}}", "Amount": 500, "Date": "2026-09-01", "Description": "Split", "IncomeId": null },
            { "Id": "{{LegacyWithdrawalId}}", "BucketId": "{{HouseTreatsId}}", "Amount": -20, "Date": "2026-09-15", "Description": "Cinema", "IncomeId": null }
          ],
          "Version": 4
        }
        """;

    public LinkedPotOperationsAcceptanceTests()
    {
        ReplaceCashFlowDataBeforeStart(SeededData);
    }

    private ICashFlowRepository Repository => Services.GetRequiredService<ICashFlowRepository>();

    private Task<HttpResponseMessage> WithdrawThroughBarclaysAsync() =>
        Client.PostAsJsonAsync($"{Api}/reserve/withdrawals", new WithdrawalRequestDTO
        {
            BucketId = Guid.Parse(InvestimentoId),
            Amount = 38.25m,
            Date = new DateOnly(2026, 10, 3),
            Description = "Boots",
            PaymentSourceBankId = Guid.Parse(BarclaysId),
            ExpenseCategoryId = Guid.Parse(MercadoId)
        });

    private static async Task<string?> DetailOf(HttpResponseMessage response) =>
        (await response.Content.ReadFromJsonAsync<ProblemDetails>())!.Detail;

    private List<Expense> ExpensesOf(Guid? operationId) =>
        Repository.GetExpenses().Where(expense => expense.OperationId == operationId).ToList();

    private Task<HttpResponseMessage> WithdrawFromPotsAccountAsync(decimal amount, bool withBank, bool confirmed = false) =>
        Client.PostAsJsonAsync($"{Api}/accounts/{ReservaId}/withdrawals", new PotWithdrawalRequestDTO
        {
            PotId = Guid.Parse(InvestimentoId),
            Amount = amount,
            Date = new DateOnly(2026, 10, 2),
            Description = "Boots",
            BankId = withBank ? Guid.Parse(BarclaysId) : null,
            CategoryId = withBank ? Guid.Parse(MercadoId) : null,
            Confirmed = confirmed
        });

    private Task<HttpResponseMessage> DepositAsync(decimal amount, string? potId = null, params (string BankId, decimal Amount)[] lines) =>
        Client.PostAsJsonAsync($"{Api}/accounts/{ReservaId}/deposits", DepositRequest(amount, potId, lines));

    private static PotDepositRequestDTO DepositRequest(decimal amount, string? potId, (string BankId, decimal Amount)[] lines) => new()
    {
        Amount = amount,
        Date = new DateOnly(2026, 10, 2),
        Description = "Ramsay bonus",
        PotId = potId is null ? null : Guid.Parse(potId),
        FundingLines = lines.Select(line => new FundingLineDTO { BankId = Guid.Parse(line.BankId), Amount = line.Amount }).ToList()
    };

    private List<(Guid PotId, decimal Amount)> MovementsOf(Guid operationId) =>
        Repository.GetPotTransactions().Where(m => m.OperationId == operationId).Select(m => (m.Pot.Id, m.Amount)).ToList();

    [Fact]
    [Trait("AC", "P60-F10-linked-deposit-withdrawal-and-pots-screen-02")]
    public async Task Deposit_FundingLines_OneExpensePerBankOrRejected()
    {
        var split = await DepositAsync(90m, null, (BarclaysId, 85m), (ChaseId, 5m));
        var mismatch = await DepositAsync(90m, null, (BarclaysId, 80m));
        var created = await split.Content.ReadFromJsonAsync<PotOperationDTO>();

        using (new AssertionScope())
        {
            split.StatusCode.Should().Be(HttpStatusCode.Created);
            ExpensesOf(created!.OperationId).Select(expense => (expense.Category.Id, expense.Value, expense.PaymentSourceAccount!.Id, expense.Description))
                .Should().Equal(
                    (Guid.Parse(ReservaCategoryId), 85m, Guid.Parse(BarclaysId), "Deposit to Reserva"),
                    (Guid.Parse(ReservaCategoryId), 5m, Guid.Parse(ChaseId), "Deposit to Reserva"));
            MovementsOf(created.OperationId).Should().Equal((Guid.Parse(InvestimentoId), 54m), (Guid.Parse(HouseTreatsId), 36m));
            mismatch.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            (await DetailOf(mismatch)).Should().Be("Funding lines must add up to £90.00.");
            Repository.GetExpenses().Should().HaveCount(2);
        }
    }

    [Fact]
    [Trait("AC", "P60-F10-linked-deposit-withdrawal-and-pots-screen-03")]
    public async Task ManualDeposit_NoLines_CreditsPotsOnly()
    {
        var response = await DepositAsync(50m);
        var created = await response.Content.ReadFromJsonAsync<PotOperationDTO>();

        using (new AssertionScope())
        {
            response.StatusCode.Should().Be(HttpStatusCode.Created);
            created!.Into.Should().Be("ActivePots");
            MovementsOf(created.OperationId).Should().Equal((Guid.Parse(InvestimentoId), 30m), (Guid.Parse(HouseTreatsId), 20m));
            Repository.GetExpenses().Should().BeEmpty();
        }
    }

    [Fact]
    [Trait("AC", "P60-F10-linked-deposit-withdrawal-and-pots-screen-09")]
    public async Task ManualDeposit_OnePot_CreditsOnlyThatPot()
    {
        var active = await (await DepositAsync(25m, HouseTreatsId)).Content.ReadFromJsonAsync<PotOperationDTO>();
        var inactive = await (await DepositAsync(40m, HolidaysId)).Content.ReadFromJsonAsync<PotOperationDTO>();

        using (new AssertionScope())
        {
            MovementsOf(active!.OperationId).Should().Equal((Guid.Parse(HouseTreatsId), 25m));
            MovementsOf(inactive!.OperationId).Should().Equal((Guid.Parse(HolidaysId), 40m));
            (active.Into, active.PotId).Should().Be(("OnePot", Guid.Parse(HouseTreatsId)));
            (inactive.Into, inactive.PotId).Should().Be(("OnePot", Guid.Parse(HolidaysId)));
        }
    }

    [Fact]
    public async Task DepositOperation_ReadEditedAndDeleted_ChangesEveryRecordTogether()
    {
        var created = await (await DepositAsync(90m, null, (BarclaysId, 90m))).Content.ReadFromJsonAsync<PotOperationDTO>();
        var operationUrl = $"{Api}/accounts/operations/{created!.OperationId}";

        var read = await Client.GetFromJsonAsync<PotOperationDTO>(operationUrl);
        var asWithdrawal = await Client.PutAsJsonAsync($"{Api}/accounts/{ReservaId}/withdrawals/{created.OperationId}", new PotWithdrawalRequestDTO
        {
            PotId = Guid.Parse(InvestimentoId),
            Amount = 5m,
            Date = new DateOnly(2026, 10, 3),
            Description = "Cinema"
        });
        var update = await Client.PutAsJsonAsync(
            $"{Api}/accounts/{ReservaId}/deposits/{created.OperationId}", DepositRequest(30m, InvestimentoId, [(ChaseId, 30m)]));
        var updated = await update.Content.ReadFromJsonAsync<PotOperationDTO>();
        var movementsAfterUpdate = MovementsOf(created.OperationId);
        var expensesAfterUpdate = ExpensesOf(created.OperationId).Select(expense => (expense.Value, expense.PaymentSourceAccount!.Id)).ToList();
        var delete = await Client.DeleteAsync(operationUrl);

        using (new AssertionScope())
        {
            read.Should().BeEquivalentTo(created);
            read!.FundingLines.Select(line => (line.BankId, line.Amount)).Should().Equal((Guid.Parse(BarclaysId), 90m));
            asWithdrawal.StatusCode.Should().Be(HttpStatusCode.NotFound);
            update.StatusCode.Should().Be(HttpStatusCode.OK);
            (updated!.Into, updated.PotId, updated.Amount).Should().Be(("OnePot", Guid.Parse(InvestimentoId), 30m));
            movementsAfterUpdate.Should().Equal((Guid.Parse(InvestimentoId), 30m));
            expensesAfterUpdate.Should().Equal((30m, Guid.Parse(ChaseId)));
            delete.StatusCode.Should().Be(HttpStatusCode.NoContent);
            MovementsOf(created.OperationId).Should().BeEmpty();
            ExpensesOf(created.OperationId).Should().BeEmpty();
        }
    }

    [Fact]
    [Trait("AC", "P60-F10-linked-deposit-withdrawal-and-pots-screen-05")]
    public async Task Withdrawal_ThroughBank_WritesLinkedRecords()
    {
        var throughBank = await WithdrawFromPotsAccountAsync(38.25m, withBank: true);
        var potOnly = await WithdrawFromPotsAccountAsync(10m, withBank: false);
        var linked = await throughBank.Content.ReadFromJsonAsync<PotOperationDTO>();
        var unlinked = await potOnly.Content.ReadFromJsonAsync<PotOperationDTO>();

        using (new AssertionScope())
        {
            throughBank.StatusCode.Should().Be(HttpStatusCode.Created);
            potOnly.StatusCode.Should().Be(HttpStatusCode.Created);
            Repository.GetPotTransactions().Where(m => m.OperationId == linked!.OperationId).Select(m => (m.Pot.Id, m.Amount))
                .Should().Equal((Guid.Parse(InvestimentoId), -38.25m));
            ExpensesOf(linked!.OperationId).Select(expense => (expense.Category.Id, expense.Value, expense.PaymentSourceAccount!.Id))
                .Should().BeEquivalentTo([
                    (Guid.Parse(ReservaCategoryId), -38.25m, Guid.Parse(BarclaysId)),
                    (Guid.Parse(MercadoId), 38.25m, Guid.Parse(BarclaysId))]);
            Repository.GetPotTransactions().Should().ContainSingle(m => m.OperationId == unlinked!.OperationId && m.Amount == -10m);
            ExpensesOf(unlinked!.OperationId).Should().BeEmpty();
        }
    }

    [Fact]
    [Trait("AC", "P60-F10-linked-deposit-withdrawal-and-pots-screen-06")]
    public async Task Withdrawal_Overdraft_Returns409UntilConfirmed()
    {
        var unconfirmed = await WithdrawFromPotsAccountAsync(600m, withBank: true);
        var movementsAfterRefusal = Repository.GetPotTransactions().Count();
        var confirmed = await WithdrawFromPotsAccountAsync(600m, withBank: true, confirmed: true);

        using (new AssertionScope())
        {
            unconfirmed.StatusCode.Should().Be(HttpStatusCode.Conflict);
            movementsAfterRefusal.Should().Be(2);
            confirmed.StatusCode.Should().Be(HttpStatusCode.Created);
            Repository.GetPotTransactions().Should().ContainSingle(m => m.Amount == -600m);
        }
    }

    [Fact]
    public async Task WithdrawalOperation_ReadEditedAndDeleted_ChangesEveryRecordTogether()
    {
        var created = await (await WithdrawFromPotsAccountAsync(38.25m, withBank: true)).Content.ReadFromJsonAsync<PotOperationDTO>();
        var operationUrl = $"{Api}/accounts/operations/{created!.OperationId}";

        var read = await Client.GetFromJsonAsync<PotOperationDTO>(operationUrl);
        var update = await Client.PutAsJsonAsync($"{Api}/accounts/{ReservaId}/withdrawals/{created.OperationId}", new PotWithdrawalRequestDTO
        {
            PotId = Guid.Parse(HouseTreatsId),
            Amount = 5m,
            Date = new DateOnly(2026, 10, 3),
            Description = "Cinema",
            Confirmed = true
        });
        var afterUpdate = ExpensesOf(created.OperationId).Count;
        var movementAfterUpdate = Repository.GetPotTransactions().Single(m => m.OperationId == created.OperationId);
        var delete = await Client.DeleteAsync(operationUrl);
        var readAfterDelete = await Client.GetAsync(operationUrl);

        using (new AssertionScope())
        {
            read.Should().BeEquivalentTo(created);
            read!.Kind.Should().Be("Withdrawal");
            update.StatusCode.Should().Be(HttpStatusCode.OK);
            afterUpdate.Should().Be(0);
            (movementAfterUpdate.Pot.Id, movementAfterUpdate.Amount, movementAfterUpdate.Description)
                .Should().Be((Guid.Parse(HouseTreatsId), -5m, "Cinema"));
            delete.StatusCode.Should().Be(HttpStatusCode.NoContent);
            readAfterDelete.StatusCode.Should().Be(HttpStatusCode.NotFound);
            Repository.GetPotTransactions().Should().NotContain(m => m.OperationId == created.OperationId);
        }
    }

    [Fact]
    [Trait("AC", "P60-F10-linked-deposit-withdrawal-and-pots-screen-12")]
    public async Task ReservaRoutes_WriteLinkedRecords()
    {
        var split = await Client.PostAsJsonAsync($"{Api}/reserve/income-split", new IncomeSplitRequestDTO
        {
            Date = new DateOnly(2026, 10, 2),
            Amount = 90m,
            Description = "Ramsay"
        });
        var splitResult = await split.Content.ReadFromJsonAsync<IncomeSplitResultDTO>();
        var splitMovements = Repository.GetPotTransactions().Where(m => m.Description == "Ramsay").ToList();

        var response = await WithdrawThroughBarclaysAsync();
        var created = await response.Content.ReadFromJsonAsync<ReserveMovementDTO>();

        var movement = Repository.GetPotTransactions().Single(m => m.Id == created!.Id);
        using (new AssertionScope())
        {
            split.StatusCode.Should().Be(HttpStatusCode.OK);
            splitResult!.Total.Should().Be(90m);
            splitResult.Buckets.Select(bucket => (bucket.BucketId, bucket.BucketName, bucket.Amount)).Should().BeEquivalentTo([
                (Guid.Parse(InvestimentoId), "Investimento", 54m),
                (Guid.Parse(HouseTreatsId), "HouseTreats", 36m)]);
            splitMovements.Select(m => m.OperationId).Distinct().Should().ContainSingle().Which.Should().NotBeNull();
            ExpensesOf(splitMovements[0].OperationId).Should().BeEmpty();
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            movement.Amount.Should().Be(-38.25m);
            movement.OperationId.Should().NotBeNull();
            ExpensesOf(movement.OperationId).Select(expense => (expense.Category.Id, expense.Value, expense.PaymentSourceAccount!.Id))
                .Should().BeEquivalentTo([
                    (Guid.Parse(ReservaCategoryId), -38.25m, Guid.Parse(BarclaysId)),
                    (Guid.Parse(MercadoId), 38.25m, Guid.Parse(BarclaysId))]);
            Repository.GetExpenses().Should().HaveCount(2);
        }
    }

    [Fact]
    public async Task LinkedRecords_EditedOrDeletedThroughTheOldRoutes_Return409AndStay()
    {
        var created = await (await WithdrawThroughBarclaysAsync()).Content.ReadFromJsonAsync<ReserveMovementDTO>();
        var expense = Repository.GetExpenses().First();

        var expenseUpdate = await Client.PutAsJsonAsync($"{Api}/expenses/{expense.Id}", new ExpenseUpdateDTO
        {
            Date = expense.Date,
            Description = "Changed",
            Value = 1m,
            CategoryId = expense.Category.Id,
            PaymentSourceBankId = Guid.Parse(BarclaysId)
        });
        var expenseDelete = await Client.DeleteAsync($"{Api}/expenses/{expense.Id}");
        var movementUpdate = await Client.PutAsJsonAsync($"{Api}/reserve/movements/{created!.Id}", new ReserveMovementUpdateDTO
        {
            BucketId = Guid.Parse(HouseTreatsId),
            Amount = -1m,
            Date = created.Date,
            Description = "Changed"
        });
        var movementDelete = await Client.DeleteAsync($"{Api}/reserve/movements/{created.Id}");

        using (new AssertionScope())
        {
            expenseUpdate.StatusCode.Should().Be(HttpStatusCode.Conflict);
            (await DetailOf(expenseUpdate)).Should().Be(ExpenseMessage);
            expenseDelete.StatusCode.Should().Be(HttpStatusCode.Conflict);
            (await DetailOf(expenseDelete)).Should().Be(ExpenseMessage);
            movementUpdate.StatusCode.Should().Be(HttpStatusCode.Conflict);
            (await DetailOf(movementUpdate)).Should().Be(MovementMessage);
            movementDelete.StatusCode.Should().Be(HttpStatusCode.Conflict);
            (await DetailOf(movementDelete)).Should().Be(MovementMessage);
            Repository.GetExpenses().Select(e => (e.Description, e.Value)).Should().BeEquivalentTo([("Boots", -38.25m), ("Boots", 38.25m)]);
            Repository.GetPotTransactions().Single(m => m.Id == created.Id).Amount.Should().Be(-38.25m);
        }
    }

    [Fact]
    [Trait("AC", "P60-F10-linked-deposit-withdrawal-and-pots-screen-08")]
    public async Task LegacyMovements_KeepTodaysEditAndDelete()
    {
        var update = await Client.PutAsJsonAsync($"{Api}/reserve/movements/{LegacyDepositId}", new ReserveMovementUpdateDTO
        {
            BucketId = Guid.Parse(InvestimentoId),
            Amount = 450m,
            Date = new DateOnly(2026, 9, 1),
            Description = "Split corrected"
        });
        var delete = await Client.DeleteAsync($"{Api}/reserve/movements/{LegacyWithdrawalId}");

        using (new AssertionScope())
        {
            update.StatusCode.Should().Be(HttpStatusCode.OK);
            delete.StatusCode.Should().Be(HttpStatusCode.OK);
            Repository.GetPotTransactions().Select(m => (m.Id, m.Amount, m.Description))
                .Should().Equal((Guid.Parse(LegacyDepositId), 450m, "Split corrected"));
        }
    }
}
