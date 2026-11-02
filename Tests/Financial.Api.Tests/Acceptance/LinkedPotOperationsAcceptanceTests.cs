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
    private const string InvestimentoId = "9a1d0000-0000-0000-0000-000000000101";
    private const string HouseTreatsId = "9a1d0000-0000-0000-0000-000000000102";
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
            { "Id": "{{ReservaId}}", "Name": "Reserva", "Template": "Pots", "Currency": "GBP", "IsActive": true, "OpeningBalance": null, "RoundUp": null, "Pots": [
              { "Id": "{{InvestimentoId}}", "Name": "Investimento", "IsActive": true, "SplitPercentage": 60 },
              { "Id": "{{HouseTreatsId}}", "Name": "HouseTreats", "IsActive": true, "SplitPercentage": 40 }
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
            Date = new DateOnly(2026, 10, 12),
            Description = "Boots",
            PaymentSourceBankId = Guid.Parse(BarclaysId),
            ExpenseCategoryId = Guid.Parse(MercadoId)
        });

    private static async Task<string?> DetailOf(HttpResponseMessage response) =>
        (await response.Content.ReadFromJsonAsync<ProblemDetails>())!.Detail;

    private List<Expense> ExpensesOf(Guid? operationId) =>
        Repository.GetExpenses().Where(expense => expense.OperationId == operationId).ToList();

    [Fact]
    [Trait("AC", "P60-F10-linked-deposit-withdrawal-and-pots-screen-12")]
    public async Task ReservaRoutes_WriteLinkedRecords()
    {
        var response = await WithdrawThroughBarclaysAsync();
        var created = await response.Content.ReadFromJsonAsync<ReserveMovementDTO>();

        var movement = Repository.GetReserveMovements().Single(m => m.Id == created!.Id);
        using (new AssertionScope())
        {
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
            Repository.GetReserveMovements().Single(m => m.Id == created.Id).Amount.Should().Be(-38.25m);
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
            Repository.GetReserveMovements().Select(m => (m.Id, m.Amount, m.Description))
                .Should().Equal((Guid.Parse(LegacyDepositId), 450m, "Split corrected"));
        }
    }
}
