using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Financial.CashFlow.Application.DTOs;
using Financial.CashFlow.Application.Interfaces;
using Financial.CashFlow.Domain.Entities;
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
    private const string RamsayId = "9a1d0000-0000-0000-0000-000000000401";
    private const string OlderIncomeId = "9a1d0000-0000-0000-0000-000000000501";
    private const string IncomeDepositMessage = "This deposit belongs to an income; edit the income.";
    private static readonly DateOnly IncomeDate = new(2026, 10, 1);
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
          "IncomeSources": [
            { "Id": "{{RamsayId}}", "Name": "Ramsay", "IsActive": true, "Group": "Salary", "PotsAccountId": "{{ReservaId}}" }
          ],
          "Incomes": [],
          "ReserveMovements": [
            { "Id": "{{LegacyDepositId}}", "BucketId": "{{InvestimentoId}}", "Amount": 500, "Date": "2026-09-01", "Description": "Split", "IncomeId": null },
            { "Id": "{{LegacyWithdrawalId}}", "BucketId": "{{HouseTreatsId}}", "Amount": -20, "Date": "2026-09-15", "Description": "Cinema", "IncomeId": null }
          ],
          "Version": 5
        }
        """;

    private const string OlderIncomeJson = $$"""
        "Incomes": [
          { "Id": "{{OlderIncomeId}}", "Date": "2026-10-01", "IncomeSourceId": "{{RamsayId}}", "GrossValue": null, "NetValue": 100, "AccountId": "{{BarclaysId}}", "Description": "Salary", "PotsAccountId": "{{ReservaId}}" }
        ],
        """;

    private const string OlderMovementJson = $$"""
        "ReserveMovements": [
          { "Id": "9a1d0000-0000-0000-0000-000000000502", "BucketId": "{{InvestimentoId}}", "Amount": 54, "Date": "2026-10-01", "Description": "Salary", "IncomeId": "{{OlderIncomeId}}" },
          { "Id": "9a1d0000-0000-0000-0000-000000000503", "BucketId": "{{HouseTreatsId}}", "Amount": 36, "Date": "2026-10-01", "Description": "Salary", "IncomeId": "{{OlderIncomeId}}" },
        """;

    public LinkedPotOperationsAcceptanceTests()
    {
        ReplaceCashFlowDataBeforeStart(SeededData);
    }

    private ICashFlowRepository Repository => Services.GetRequiredService<ICashFlowRepository>();

    private IReadOnlyList<Pot> ReservaPots => Repository.GetAccounts().Single(account => account.Id == Guid.Parse(ReservaId)).Pots!;

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
        FundingLines = ToFundingLines(lines)
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
        var problem = await unconfirmed.Content.ReadFromJsonAsync<JsonElement>();

        using (new AssertionScope())
        {
            unconfirmed.StatusCode.Should().Be(HttpStatusCode.Conflict);
            problem.GetProperty("code").GetString().Should().Be("overdraftConfirmationRequired");
            problem.GetProperty("potName").GetString().Should().Be("Investimento");
            problem.GetProperty("balance").GetDecimal().Should().Be(500m);
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
    public async Task LinkedRecords_EditedOrDeletedThroughTheOldRoutes_Return409AndStay()
    {
        var operation = await (await WithdrawFromPotsAccountAsync(38.25m, withBank: true)).Content.ReadFromJsonAsync<PotOperationDTO>();
        var created = Repository.GetPotTransactions().Single(m => m.OperationId == operation!.OperationId);
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
        var movementUpdate = await Client.PutAsJsonAsync($"{Api}/reserve/movements/{created.Id}", new ReserveMovementUpdateDTO
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

    [Fact]
    [Trait("AC", "P60-F10-linked-deposit-withdrawal-and-pots-screen-11")]
    public async Task LegacyManualSplit_Delete_RemovesOnlyItsLines()
    {
        var sameSplitLine = "9a1d0000-0000-0000-0000-000000000601";
        var withdrawalSharingTheText = "9a1d0000-0000-0000-0000-000000000602";
        var splitOfAnotherDay = "9a1d0000-0000-0000-0000-000000000603";
        ReplaceCashFlowDataBeforeStart(SeededData.Replace("\"ReserveMovements\": [", $$"""
            "ReserveMovements": [
              { "Id": "{{sameSplitLine}}", "BucketId": "{{HouseTreatsId}}", "Amount": 300, "Date": "2026-09-01", "Description": "Split", "IncomeId": null },
              { "Id": "{{withdrawalSharingTheText}}", "BucketId": "{{InvestimentoId}}", "Amount": -40, "Date": "2026-09-01", "Description": "Split", "IncomeId": null },
              { "Id": "{{splitOfAnotherDay}}", "BucketId": "{{InvestimentoId}}", "Amount": 70, "Date": "2026-09-02", "Description": "Split", "IncomeId": null },
            """));

        var deleteSplit = await Client.DeleteAsync($"{Api}/reserve/movements/{LegacyDepositId}");
        var remainingAfterSplit = Repository.GetPotTransactions().Select(m => m.Id).ToList();
        var deleteWithdrawal = await Client.DeleteAsync($"{Api}/reserve/movements/{withdrawalSharingTheText}");

        using (new AssertionScope())
        {
            deleteSplit.StatusCode.Should().Be(HttpStatusCode.OK);
            remainingAfterSplit.Should().BeEquivalentTo(
                [Guid.Parse(withdrawalSharingTheText), Guid.Parse(splitOfAnotherDay), Guid.Parse(LegacyWithdrawalId)]);
            deleteWithdrawal.StatusCode.Should().Be(HttpStatusCode.OK);
            Repository.GetPotTransactions().Select(m => m.Id).Should().BeEquivalentTo(
                [Guid.Parse(splitOfAnotherDay), Guid.Parse(LegacyWithdrawalId)]);
        }
    }

    [Fact]
    public async Task PotsRead_MonthEnd_ReturnsEachPotAndATotalMatchingTheHeader()
    {
        var september = await Client.GetFromJsonAsync<PotsSummaryDTO>($"{Api}/accounts/{ReservaId}/pots/2026/9");
        var august = await Client.GetFromJsonAsync<PotsSummaryDTO>($"{Api}/accounts/{ReservaId}/pots/2026/8");
        var header = await Client.GetFromJsonAsync<AccountDetailDTO>($"{Api}/accounts/{ReservaId}/2026/9");

        using (new AssertionScope())
        {
            september!.PotsAccountId.Should().Be(Guid.Parse(ReservaId));
            september.Currency.Should().Be("GBP");
            september.Total.Should().Be(480m);
            header!.Balance.Should().Be(480m);
            september.Pots.Select(pot => (pot.Name, pot.SplitPercentage, pot.IsActive, pot.Balance)).Should().Equal(
                ("HouseTreats", 40m, true, -20m),
                ("Investimento", 60m, true, 500m),
                ("Holidays", 0m, false, 0m));
            august!.Total.Should().Be(0m);
            august.Pots.Should().OnlyContain(pot => pot.Balance == 0m);
        }
    }

    [Fact]
    public async Task PotsRead_UnknownAccountOrInvalidMonth_IsRejected()
    {
        var notAPotsAccount = await Client.GetAsync($"{Api}/accounts/{BarclaysId}/pots/2026/9");
        var unknown = await Client.GetAsync($"{Api}/accounts/{Guid.NewGuid()}/pots/2026/9");
        var invalidMonth = await Client.GetAsync($"{Api}/accounts/{ReservaId}/pots/2026/13");

        using (new AssertionScope())
        {
            notAPotsAccount.StatusCode.Should().Be(HttpStatusCode.NotFound);
            unknown.StatusCode.Should().Be(HttpStatusCode.NotFound);
            invalidMonth.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }
    }

    private static PotUpdateDTO PotLine(string? id, string name, decimal split, bool isActive = true) => new()
    {
        Id = id is null ? null : Guid.Parse(id),
        Name = name,
        SplitPercentage = split,
        IsActive = isActive
    };

    private Task<HttpResponseMessage> ManagePotsAsync(params PotUpdateDTO[] pots) =>
        Client.PutAsJsonAsync($"{Api}/accounts/{ReservaId}/pots", new PotsUpdateDTO { Pots = pots });

    [Fact]
    [Trait("AC", "P60-F10-linked-deposit-withdrawal-and-pots-screen-14")]
    public async Task ManagePots_TotalNot100_Returns400()
    {
        var response = await ManagePotsAsync(
            PotLine(InvestimentoId, "Renamed", 55m), PotLine(HouseTreatsId, "HouseTreats", 40m), PotLine(HolidaysId, "Holidays", 0m, isActive: false));

        using (new AssertionScope())
        {
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            (await DetailOf(response)).Should().Be("Active pots must total 100% (now 95%).");
            ReservaPots.Select(pot => pot.Name).Should().Equal("Investimento", "HouseTreats", "Holidays");
        }
    }

    [Fact]
    [Trait("AC", "P60-F10-linked-deposit-withdrawal-and-pots-screen-14")]
    public async Task ManagePots_ValidList_SavesAllTogether()
    {
        var response = await ManagePotsAsync(
            PotLine(InvestimentoId, "Long term", 50m),
            PotLine(HouseTreatsId, "HouseTreats", 25m),
            PotLine(HolidaysId, "Holidays", 25m),
            PotLine(null, "Car", 0m, isActive: false));

        using (new AssertionScope())
        {
            response.StatusCode.Should().Be(HttpStatusCode.NoContent);
            ReservaPots.Select(pot => (pot.Name, pot.SplitPercentage, pot.IsActive)).Should().Equal(
                ("Long term", 50m, true), ("HouseTreats", 25m, true), ("Holidays", 25m, true), ("Car", 0m, false));
        }
    }

    [Fact]
    public async Task ManagePots_MissingPotOrDuplicateNameOrBlankName_IsRejected()
    {
        var missing = await ManagePotsAsync(PotLine(InvestimentoId, "Investimento", 60m), PotLine(HouseTreatsId, "HouseTreats", 40m));
        var duplicate = await ManagePotsAsync(
            PotLine(InvestimentoId, "Investimento", 60m), PotLine(HouseTreatsId, "HOLIDAYS", 40m), PotLine(HolidaysId, "Holidays", 0m, isActive: false));
        var blank = await ManagePotsAsync(
            PotLine(InvestimentoId, " ", 60m), PotLine(HouseTreatsId, "HouseTreats", 40m), PotLine(HolidaysId, "Holidays", 0m, isActive: false));

        using (new AssertionScope())
        {
            missing.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            (await DetailOf(missing)).Should().Be("Every pot must be in the list; pots can't be deleted.");
            duplicate.StatusCode.Should().Be(HttpStatusCode.Conflict);
            (await DetailOf(duplicate)).Should().Be("A pot named \"HOLIDAYS\" already exists.");
            blank.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            (await DetailOf(blank)).Should().Be("Pot name is required.");
            ReservaPots.Select(pot => pot.Name).Should().Equal("Investimento", "HouseTreats", "Holidays");
        }
    }

    [Fact]
    public async Task ManagePots_AccountWithoutPots_Returns404()
    {
        var response = await Client.PutAsJsonAsync($"{Api}/accounts/{BarclaysId}/pots", new PotsUpdateDTO { Pots = [] });

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    [Trait("AC", "P60-F10-linked-deposit-withdrawal-and-pots-screen-13")]
    public async Task ReserveBucketWrites_AreGone()
    {
        var bucket = new { name = "Ferias", splitPercentage = 0m, isActive = false };

        var create = await Client.PostAsJsonAsync($"{Api}/reserve-buckets", bucket);
        var update = await Client.PutAsJsonAsync($"{Api}/reserve-buckets/{InvestimentoId}", bucket);
        var list = await Client.GetAsync($"{Api}/reserve-buckets");
        var splitStatus = await Client.GetAsync($"{Api}/reserve-buckets/split-status");

        using (new AssertionScope())
        {
            create.StatusCode.Should().Be(HttpStatusCode.NotFound);
            update.StatusCode.Should().Be(HttpStatusCode.NotFound);
            list.StatusCode.Should().Be(HttpStatusCode.NotFound);
            splitStatus.StatusCode.Should().Be(HttpStatusCode.NotFound);
            ReservaPots.Select(pot => pot.Name).Should().Equal("Investimento", "HouseTreats", "Holidays");
        }
    }

    private Task<HttpResponseMessage> AddSplitIncomeAsync(decimal net, params (string BankId, decimal Amount)[] lines) =>
        Client.PostAsJsonAsync($"{Api}/incomes", new IncomeCreateDTO
        {
            Date = IncomeDate,
            IncomeSourceId = Guid.Parse(RamsayId),
            NetValue = net,
            BankId = Guid.Parse(BarclaysId),
            Description = "Salary",
            PotsAccountId = Guid.Parse(ReservaId),
            FundingLines = ToFundingLines(lines)
        });

    private Task<HttpResponseMessage> UpdateSplitIncomeAsync(Guid id, decimal net, string? potsAccountId = ReservaId, params (string BankId, decimal Amount)[] lines) =>
        Client.PutAsJsonAsync($"{Api}/incomes/{id}", new IncomeUpdateDTO
        {
            Date = IncomeDate,
            IncomeSourceId = Guid.Parse(RamsayId),
            NetValue = net,
            BankId = Guid.Parse(BarclaysId),
            Description = "Salary",
            PotsAccountId = potsAccountId is null ? null : Guid.Parse(potsAccountId),
            FundingLines = ToFundingLines(lines)
        });

    private static List<FundingLineDTO> ToFundingLines((string BankId, decimal Amount)[] lines) =>
        lines.Select(line => new FundingLineDTO { BankId = Guid.Parse(line.BankId), Amount = line.Amount }).ToList();

    private List<Expense> ExpensesOfIncome(Guid incomeId) =>
        ExpensesOf(Repository.GetPotTransactions().First(m => m.Income?.Id == incomeId).OperationId);

    [Fact]
    [Trait("AC", "P60-F10-linked-deposit-withdrawal-and-pots-screen-01")]
    public async Task Income_WithSplit_WritesLinkedDeposit()
    {
        var response = await AddSplitIncomeAsync(100m);
        var income = await response.Content.ReadFromJsonAsync<IncomeDTO>();

        using (new AssertionScope())
        {
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            income!.HasLinkedDeposit.Should().BeTrue();
            income.FundingLines.Select(line => (line.BankId, line.Amount)).Should().Equal((Guid.Parse(BarclaysId), 90m));
            var expense = ExpensesOfIncome(income.Id).Should().ContainSingle().Which;
            (expense.Category.Id, expense.Value, expense.PaymentSourceAccount!.Id, expense.Description, expense.Date, expense.CountsAsTithe)
                .Should().Be((Guid.Parse(ReservaCategoryId), 90m, Guid.Parse(BarclaysId), "Deposit to Reserva \u2013 Ramsay", IncomeDate, false));
            MovementsOf(expense.OperationId!.Value).Should().Equal((Guid.Parse(InvestimentoId), 54m), (Guid.Parse(HouseTreatsId), 36m));
        }
    }

    [Fact]
    [Trait("AC", "P60-F10-linked-deposit-withdrawal-and-pots-screen-02")]
    public async Task Income_HandSplitLines_OneExpensePerBankOrRejected()
    {
        var split = await AddSplitIncomeAsync(100m, (BarclaysId, 85m), (ChaseId, 5m));
        var mismatch = await AddSplitIncomeAsync(100m, (BarclaysId, 88m));
        var income = await split.Content.ReadFromJsonAsync<IncomeDTO>();

        using (new AssertionScope())
        {
            ExpensesOfIncome(income!.Id).Select(e => (e.PaymentSourceAccount!.Id, e.Value))
                .Should().Equal((Guid.Parse(BarclaysId), 85m), (Guid.Parse(ChaseId), 5m));
            mismatch.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            (await DetailOf(mismatch)).Should().Be("Funding lines must add up to \u00a390.00.");
            Repository.GetIncomes().Should().ContainSingle();
        }
    }

    [Fact]
    [Trait("AC", "P60-F10-linked-deposit-withdrawal-and-pots-screen-04")]
    public async Task Income_EditAndDelete_RebuildAndRemoveDeposit()
    {
        var created = await (await AddSplitIncomeAsync(100m)).Content.ReadFromJsonAsync<IncomeDTO>();
        var operationId = ExpensesOfIncome(created!.Id).Single().OperationId!.Value;

        var edit = await UpdateSplitIncomeAsync(created.Id, 200m);
        var editedExpense = Repository.GetExpenses().Single();
        var editedMovements = MovementsOf(operationId);
        var rejected = await UpdateSplitIncomeAsync(created.Id, 300m, ReservaId, (BarclaysId, 180m));
        var incomeAfterRejection = Repository.GetIncomes().Single().NetValue;
        var expensesAfterRejection = Repository.GetExpenses().Select(e => e.Value).ToList();
        var delete = await Client.DeleteAsync($"{Api}/incomes/{created.Id}");

        using (new AssertionScope())
        {
            edit.StatusCode.Should().Be(HttpStatusCode.OK);
            (editedExpense.Value, editedExpense.OperationId).Should().Be((180m, (Guid?)operationId));
            editedMovements.Should().Equal((Guid.Parse(InvestimentoId), 108m), (Guid.Parse(HouseTreatsId), 72m));
            rejected.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            (await DetailOf(rejected)).Should().Be("Funding lines must add up to \u00a3270.00.");
            incomeAfterRejection.Should().Be(200m);
            expensesAfterRejection.Should().Equal(180m);
            delete.IsSuccessStatusCode.Should().BeTrue();
            Repository.GetExpenses().Should().BeEmpty();
            MovementsOf(operationId).Should().BeEmpty();
        }
    }

    [Fact]
    [Trait("AC", "P60-F10-linked-deposit-withdrawal-and-pots-screen-04")]
    public async Task Income_SplitUnticked_RemovesTheDeposit()
    {
        var created = await (await AddSplitIncomeAsync(100m)).Content.ReadFromJsonAsync<IncomeDTO>();
        var operationId = ExpensesOfIncome(created!.Id).Single().OperationId!.Value;

        var edit = await UpdateSplitIncomeAsync(created.Id, 100m, potsAccountId: null);

        using (new AssertionScope())
        {
            edit.StatusCode.Should().Be(HttpStatusCode.OK);
            Repository.GetExpenses().Should().BeEmpty();
            MovementsOf(operationId).Should().BeEmpty();
        }
    }

    [Fact]
    public async Task IncomeDeposit_ThroughTheOperationAndOldRoutes_Returns409AndStays()
    {
        var created = await (await AddSplitIncomeAsync(100m)).Content.ReadFromJsonAsync<IncomeDTO>();
        var expense = ExpensesOfIncome(created!.Id).Single();
        var operationId = expense.OperationId!.Value;

        var read = await Client.GetFromJsonAsync<PotOperationDTO>($"{Api}/accounts/operations/{operationId}");
        var update = await Client.PutAsJsonAsync($"{Api}/accounts/{ReservaId}/deposits/{operationId}", DepositRequest(90m, null, []));
        var delete = await Client.DeleteAsync($"{Api}/accounts/operations/{operationId}");
        var expenseDelete = await Client.DeleteAsync($"{Api}/expenses/{expense.Id}");

        using (new AssertionScope())
        {
            read!.IncomeId.Should().Be(created.Id);
            read.FundingLines.Select(line => line.Amount).Should().Equal(90m);
            update.StatusCode.Should().Be(HttpStatusCode.Conflict);
            (await DetailOf(update)).Should().Be(IncomeDepositMessage);
            delete.StatusCode.Should().Be(HttpStatusCode.Conflict);
            (await DetailOf(delete)).Should().Be(IncomeDepositMessage);
            expenseDelete.StatusCode.Should().Be(HttpStatusCode.Conflict);
            (await DetailOf(expenseDelete)).Should().Be(ExpenseMessage);
            Repository.GetExpenses().Should().ContainSingle();
            MovementsOf(operationId).Should().HaveCount(2);
        }
    }

    [Fact]
    [Trait("AC", "P60-F10-linked-deposit-withdrawal-and-pots-screen-10")]
    public async Task OlderSplitIncome_Edit_AddsNoExpense()
    {
        SeedOlderSplitIncome();

        var edit = await UpdateSplitIncomeAsync(Guid.Parse(OlderIncomeId), 200m, ReservaId, (ChaseId, 180m));
        var incomes = await Client.GetFromJsonAsync<List<IncomeDTO>>($"{Api}/incomes/month/{IncomeDate.Year}/{IncomeDate.Month}");

        using (new AssertionScope())
        {
            edit.StatusCode.Should().Be(HttpStatusCode.OK);
            Repository.GetExpenses().Should().BeEmpty();
            Repository.GetPotTransactions().Where(m => m.Income?.Id == Guid.Parse(OlderIncomeId))
                .Select(m => (m.Pot.Id, m.Amount, m.OperationId))
                .Should().Equal((Guid.Parse(InvestimentoId), 108m, (Guid?)null), (Guid.Parse(HouseTreatsId), 72m, (Guid?)null));
            incomes!.Single().HasLinkedDeposit.Should().BeFalse();
        }
    }

    private async Task<AccountDetailDTO> AccountDetailAsync(string accountId) =>
        (await Client.GetFromJsonAsync<AccountDetailDTO>($"{Api}/accounts/{accountId}/2026/10"))!;

    private async Task<List<AccountTransactionDTO>> RowsAsync(string accountId, int month = 10) =>
        (await Client.GetFromJsonAsync<List<AccountTransactionDTO>>($"{Api}/accounts/{accountId}/transactions/2026/{month}"))!;

    private void SeedOlderSplitIncome() =>
        ReplaceCashFlowDataBeforeStart(SeededData
            .Replace("\"Incomes\": [],", OlderIncomeJson)
            .Replace("\"ReserveMovements\": [", OlderMovementJson));

    [Fact]
    [Trait("AC", "P60-F10-linked-deposit-withdrawal-and-pots-screen-07")]
    public async Task Withdrawal_FromBankOrPotsAccount_SameRecords()
    {
        var bank = await AccountDetailAsync(BarclaysId);
        var pots = await AccountDetailAsync(ReservaId);

        var response = await WithdrawFromPotsAccountAsync(38.25m, withBank: true);
        var operation = await response.Content.ReadFromJsonAsync<PotOperationDTO>();

        var bankRows = await RowsAsync(BarclaysId);
        var potRows = await RowsAsync(ReservaId);

        using (new AssertionScope())
        {
            bank.AllowedActions.Should().Contain("WithdrawFromPots").And.NotContain("Withdraw");
            pots.AllowedActions.Should().Equal("Deposit", "Withdraw", "ManagePots");
            bankRows.Select(row => (row.Kind, row.Amount)).Should().BeEquivalentTo([("Expense", 38.25m), ("Expense", -38.25m)]);
            potRows.Select(row => (row.Kind, row.Counterpart, row.Description, row.Amount))
                .Should().Equal(("PotTransaction", "Investimento", "Boots", -38.25m));
            bankRows.Concat(potRows).Should().OnlyContain(row =>
                !row.CanEdit
                && row.Operation!.OperationId == operation!.OperationId
                && row.Operation.PotsAccountId == Guid.Parse(ReservaId)
                && row.Operation.Year == 2026
                && row.Operation.Month == 10
                && row.Operation.IncomeId == null);
        }
    }

    [Fact]
    public async Task IncomeDeposit_AccountRows_CarryTheIncomeAndItsFundingLines()
    {
        var response = await AddSplitIncomeAsync(100m);
        var income = await response.Content.ReadFromJsonAsync<IncomeDTO>();

        var bankRows = await RowsAsync(BarclaysId);
        var potRows = await RowsAsync(ReservaId);
        var incomeRow = bankRows.Single(row => row.Kind == "Income");
        var depositRow = bankRows.Single(row => row.Kind == "Expense");

        using (new AssertionScope())
        {
            incomeRow.CanEdit.Should().BeTrue();
            incomeRow.Operation.Should().BeNull();
            incomeRow.Income!.HasLinkedDeposit.Should().BeTrue();
            incomeRow.Income.FundingLines.Select(line => (line.BankId, line.Amount)).Should().Equal((Guid.Parse(BarclaysId), 90m));
            depositRow.CanEdit.Should().BeFalse();
            depositRow.Operation!.IncomeId.Should().Be(income!.Id);
            potRows.Select(row => (row.Counterpart, row.Amount)).Should().BeEquivalentTo([("Investimento", 54m), ("HouseTreats", 36m)]);
            potRows.Should().OnlyContain(row => !row.CanEdit && row.Operation!.IncomeId == income.Id);
        }
    }

    [Fact]
    public async Task OlderSplitIncome_AccountRows_HaveNoLinkAndCannotBeEdited()
    {
        SeedOlderSplitIncome();

        var incomeRow = (await RowsAsync(BarclaysId)).Single();
        var potRows = await RowsAsync(ReservaId);

        using (new AssertionScope())
        {
            incomeRow.Income!.HasLinkedDeposit.Should().BeFalse();
            incomeRow.Income.FundingLines.Should().BeEmpty();
            potRows.Should().HaveCount(2).And.OnlyContain(row => !row.CanEdit && row.Operation == null);
        }
    }

    [Fact]
    public async Task LegacyMovement_PotsAccountRow_HasNoLinkAndCanBeEdited()
    {
        var rows = await RowsAsync(ReservaId, month: 9);

        using (new AssertionScope())
        {
            rows.Select(row => (row.Id, row.Date, row.Counterpart, row.Description, row.Amount)).Should().Equal(
                (Guid.Parse(LegacyWithdrawalId), new DateOnly(2026, 9, 15), "HouseTreats", "Cinema", -20m),
                (Guid.Parse(LegacyDepositId), new DateOnly(2026, 9, 1), "Investimento", "Split", 500m));
            rows.Should().OnlyContain(row => row.Kind == "PotTransaction" && row.CanEdit && row.Operation == null && row.Movement!.Id == row.Id);
        }
    }
}
