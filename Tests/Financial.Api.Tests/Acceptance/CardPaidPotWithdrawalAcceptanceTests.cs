using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using Financial.CashFlow.Application.DTOs;
using Financial.CashFlow.Application.Interfaces;
using Financial.CashFlow.Domain.Entities.Transactions;
using FluentAssertions;
using FluentAssertions.Execution;
using Microsoft.Extensions.DependencyInjection;

namespace Financial.Api.Tests.Acceptance;

[Trait("Category", "Integration")]
public class CardPaidPotWithdrawalAcceptanceTests : ApiEndpointTests
{
    private const string BarclaysId = "6c2e0000-0000-0000-0000-000000000001";
    private const string ReservaId = "6c2e0000-0000-0000-0000-000000000002";
    private const string AmexId = "6c2e0000-0000-0000-0000-000000000003";
    private const string OldCardId = "6c2e0000-0000-0000-0000-000000000004";
    private const string InvestimentoId = "6c2e0000-0000-0000-0000-000000000101";
    private const string ReservaCategoryId = "6c2e0000-0000-0000-0000-000000000301";
    private const string ViagemId = "6c2e0000-0000-0000-0000-000000000302";
    private const string DizimoId = "6c2e0000-0000-0000-0000-000000000303";
    private const string MercadoId = "6c2e0000-0000-0000-0000-000000000304";
    private const string LockedMessage = "Unmark the statement paid to change this.";
    private const string ExpenseMessage = "This expense belongs to a pot deposit or withdrawal; edit it from the Pots account.";
    private const string MovementMessage = "This pot transaction belongs to a pot deposit or withdrawal; edit it from the Pots account.";
    private static readonly DateOnly ExpenseDate = new(2026, 10, 2);

    private const string SeededData = $$"""
        {
          "Accounts": [
            { "Id": "{{BarclaysId}}", "Name": "Barclays", "Template": "Bank", "Currency": "GBP", "IsActive": true, "OpeningBalance": { "Amount": 1000, "StartDate": "2026-01-01" }, "RoundUp": { "Enabled": false }, "Pots": null },
            { "Id": "{{AmexId}}", "Name": "Amex", "Template": "CreditCard", "Currency": "GBP", "IsActive": true, "OpeningBalance": null, "RoundUp": null },
            { "Id": "{{OldCardId}}", "Name": "Old card", "Template": "CreditCard", "Currency": "GBP", "IsActive": false, "OpeningBalance": null, "RoundUp": null },
            { "Id": "{{ReservaId}}", "Name": "Reserva", "Template": "Pots", "Currency": "GBP", "IsActive": true, "OpeningBalance": null, "RoundUp": null, "Pots": [
              { "Id": "{{InvestimentoId}}", "Name": "Investimento", "IsActive": true, "SplitPercentage": 100 }
            ], "CategoryId": "{{ReservaCategoryId}}" }
          ],
          "Categories": [
            { "Id": "{{ReservaCategoryId}}", "Name": "Reserva", "Active": true, "IsInvestment": false, "IsTithe": false },
            { "Id": "{{ViagemId}}", "Name": "Viagem", "Active": true, "IsInvestment": false, "IsTithe": false },
            { "Id": "{{DizimoId}}", "Name": "Dizimo", "Active": true, "IsInvestment": false, "IsTithe": true },
            { "Id": "{{MercadoId}}", "Name": "Mercado", "Active": true, "IsInvestment": false, "IsTithe": false }
          ],
          "IncomeSources": [],
          "Incomes": [],
          "ReserveMovements": [
            { "Id": "6c2e0000-0000-0000-0000-000000000201", "BucketId": "{{InvestimentoId}}", "Amount": 500, "Date": "2026-09-01", "Description": "Split", "IncomeId": null }
          ],
          "Version": 5
        }
        """;

    public CardPaidPotWithdrawalAcceptanceTests()
    {
        ReplaceCashFlowDataBeforeStart(SeededData);
    }

    private ICashFlowRepository Repository => Services.GetRequiredService<ICashFlowRepository>();

    private List<Expense> ExpensesOf(Guid operationId) =>
        Repository.GetExpenses().Where(expense => expense.OperationId == operationId).ToList();

    private List<PotTransaction> MovementsOf(Guid operationId) =>
        Repository.GetPotTransactions().Where(movement => movement.OperationId == operationId).ToList();

    private static PotWithdrawalRequestDTO CardRequest(
        decimal amount = 120m,
        int? statementYear = 2026,
        int? statementMonth = 11,
        string cardId = AmexId,
        string categoryId = ViagemId,
        string description = "Flights",
        DateOnly? date = null,
        bool confirmed = false) => new()
    {
        PotId = Guid.Parse(InvestimentoId),
        Amount = amount,
        Date = date ?? ExpenseDate,
        Description = description,
        CreditCardId = Guid.Parse(cardId),
        StatementYear = statementYear,
        StatementMonth = statementMonth,
        CategoryId = Guid.Parse(categoryId),
        Confirmed = confirmed
    };

    private Task<HttpResponseMessage> WithdrawAsync(PotWithdrawalRequestDTO request) =>
        Client.PostAsJsonAsync($"{Api}/accounts/{ReservaId}/withdrawals", request);

    private async Task<PotOperationDTO> CreatedAsync(PotWithdrawalRequestDTO request)
    {
        var response = await WithdrawAsync(request);
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        return (await response.Content.ReadFromJsonAsync<PotOperationDTO>())!;
    }

    private Task<HttpResponseMessage> UpdateAsync(Guid operationId, PotWithdrawalRequestDTO request) =>
        Client.PutAsJsonAsync($"{Api}/accounts/{ReservaId}/withdrawals/{operationId}", request);

    private async Task<AccountStatementDTO> StatementAsync(int year, int month)
    {
        var statements = await Client.GetFromJsonAsync<List<AccountStatementDTO>>($"{Api}/accounts/{AmexId}/statements");
        return statements!.Single(statement => statement.Year == year && statement.Month == month);
    }

    private async Task MarkPaidAsync(int year, int month)
    {
        var statement = await StatementAsync(year, month);
        var response = await Client.PostAsJsonAsync($"{Api}/card-statements/{statement.Id}/mark-paid", new MarkCardStatementPaidDTO
        {
            PaymentSourceBankId = Guid.Parse(BarclaysId),
            Date = new DateOnly(2026, 10, 5)
        });
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    private async Task<decimal> CategoryTotalAsync(int year, int month, string category)
    {
        var totals = await Client.GetFromJsonAsync<List<CategoryTotalDTO>>($"{Api}/expenses/month/{year}/{month}/category-totals");
        return totals!.Where(total => total.Category == category).Sum(total => total.TotalValue);
    }

    [Fact]
    [Trait("AC", "P62-F01-card-paid-pot-withdrawal-01")]
    public async Task CardWithdrawal_WritesPotMovementAndCardCharge_NoBankRecord()
    {
        var operation = await CreatedAsync(CardRequest());

        var fetched = await Client.GetFromJsonAsync<PotOperationDTO>($"{Api}/accounts/operations/{operation.OperationId}");
        var charge = ExpensesOf(operation.OperationId).Single();
        using (new AssertionScope())
        {
            MovementsOf(operation.OperationId).Select(movement => (movement.Amount, movement.Date)).Should().Equal((-120m, ExpenseDate));
            charge.Value.Should().Be(120m);
            charge.CreditCard!.Id.Should().Be(Guid.Parse(AmexId));
            charge.PaymentSourceAccount.Should().BeNull();
            charge.ChargeDate.Should().Be(ExpenseDate);
            charge.InvoiceDate.Should().Be(new DateOnly(2026, 11, 1));
            charge.Category.Id.Should().Be(Guid.Parse(ViagemId));
            Repository.GetExpenses().Should().NotContain(expense => expense.PaymentSourceAccount != null);
            fetched.Should().BeEquivalentTo(operation);
            fetched!.CreditCardId.Should().Be(Guid.Parse(AmexId));
            fetched.StatementYear.Should().Be(2026);
            fetched.StatementMonth.Should().Be(11);
            fetched.CategoryId.Should().Be(Guid.Parse(ViagemId));
            fetched.Amount.Should().Be(120m);
            fetched.BankId.Should().BeNull();
        }
    }

    [Fact]
    [Trait("AC", "P62-F01-card-paid-pot-withdrawal-02")]
    public async Task CardWithdrawal_StatementDefaultsAndRejectsPaidOrEarlierMonth()
    {
        var defaulted = await CreatedAsync(CardRequest(amount: 10m, statementYear: null, statementMonth: null));
        await MarkPaidAsync(2026, 10);

        var onPaid = await WithdrawAsync(CardRequest(amount: 5m, statementMonth: 10));
        var earlier = await WithdrawAsync(CardRequest(amount: 5m, statementMonth: 9));

        using (new AssertionScope())
        {
            defaulted.StatementYear.Should().Be(2026);
            defaulted.StatementMonth.Should().Be(10);
            await ShouldBeProblemAsync(onPaid, HttpStatusCode.BadRequest, "This statement is already paid; choose a later month.");
            await ShouldBeProblemAsync(earlier, HttpStatusCode.BadRequest, "The statement can't be before the expense date.");
            Repository.GetExpenses().Should().ContainSingle(expense => expense.CreditCard != null);
        }
    }

    [Fact]
    [Trait("AC", "P62-F01-card-paid-pot-withdrawal-03")]
    public async Task CardWithdrawal_RejectsInactiveCardExcludedCategoryAndUnconfirmedOverdraft()
    {
        var inactiveCard = await WithdrawAsync(CardRequest(cardId: OldCardId));
        var excludedCategory = await WithdrawAsync(CardRequest(categoryId: ReservaCategoryId));
        var overdraft = await WithdrawAsync(CardRequest(amount: 600m));
        var confirmed = await WithdrawAsync(CardRequest(amount: 600m, confirmed: true));

        using (new AssertionScope())
        {
            inactiveCard.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            (await ProblemDetailAsync(inactiveCard)).Should().Contain("inactive");
            await ShouldBeProblemAsync(excludedCategory, HttpStatusCode.BadRequest, "Category 'Reserva' cannot be used for a withdrawal expense.");
            overdraft.StatusCode.Should().Be(HttpStatusCode.Conflict);
            confirmed.StatusCode.Should().Be(HttpStatusCode.Created);
            Repository.GetExpenses().Should().ContainSingle(expense => expense.Value == 600m);
        }
    }

    [Fact]
    [Trait("AC", "P62-F01-card-paid-pot-withdrawal-04")]
    public async Task CardWithdrawal_CountsInStatementMonthForCategoriesTitheAndAmountToPay()
    {
        await CreatedAsync(CardRequest(amount: 120m));
        await CreatedAsync(CardRequest(amount: 15m, categoryId: DizimoId, description: "Offering"));

        var november = await StatementAsync(2026, 11);
        var octoberTithe = await Client.GetFromJsonAsync<TitheSummaryDTO>($"{Api}/tithe/month/2026/10");
        var novemberTithe = await Client.GetFromJsonAsync<TitheSummaryDTO>($"{Api}/tithe/month/2026/11");

        using (new AssertionScope())
        {
            (await CategoryTotalAsync(2026, 11, "Viagem")).Should().Be(120m);
            (await CategoryTotalAsync(2026, 10, "Viagem")).Should().Be(0m);
            novemberTithe!.TithePaid.Should().Be(15m);
            octoberTithe!.TithePaid.Should().Be(0m);
            november.OutstandingTotal.Should().Be(135m);
        }
    }

    [Fact]
    [Trait("AC", "P62-F01-card-paid-pot-withdrawal-05")]
    public async Task CardWithdrawal_Unpaid_EditSwitchDeleteAndMoveKeepLink()
    {
        var operation = await CreatedAsync(CardRequest());
        var charge = ExpensesOf(operation.OperationId).Single();

        var moved = await Client.PostAsJsonAsync($"{Api}/expenses/{charge.Id}/move-to-statement", new MoveChargeToStatementDTO { Year = 2026, Month = 12 });
        var afterMove = await Client.GetFromJsonAsync<PotOperationDTO>($"{Api}/accounts/operations/{operation.OperationId}");
        var toBank = await UpdateAsync(operation.OperationId, new PotWithdrawalRequestDTO
        {
            PotId = Guid.Parse(InvestimentoId),
            Amount = 80m,
            Date = ExpenseDate,
            Description = "Flights",
            BankId = Guid.Parse(BarclaysId),
            CategoryId = Guid.Parse(ViagemId)
        });
        var bankExpenses = ExpensesOf(operation.OperationId).Select(expense => (expense.Value, expense.CreditCard)).ToList();
        var backToCard = await UpdateAsync(operation.OperationId, CardRequest(amount: 90m, statementMonth: 10));
        var cardExpenses = ExpensesOf(operation.OperationId);
        var deleted = await Client.DeleteAsync($"{Api}/accounts/operations/{operation.OperationId}");

        using (new AssertionScope())
        {
            moved.StatusCode.Should().Be(HttpStatusCode.OK);
            afterMove!.StatementMonth.Should().Be(12);
            afterMove.OperationId.Should().Be(operation.OperationId);
            toBank.StatusCode.Should().Be(HttpStatusCode.OK);
            bankExpenses.Should().BeEquivalentTo([(-80m, (object?)null), (80m, (object?)null)]);
            backToCard.StatusCode.Should().Be(HttpStatusCode.OK);
            cardExpenses.Should().ContainSingle(expense => expense.Value == 90m && expense.InvoiceDate == new DateOnly(2026, 10, 1));
            deleted.StatusCode.Should().Be(HttpStatusCode.NoContent);
            ExpensesOf(operation.OperationId).Should().BeEmpty();
            MovementsOf(operation.OperationId).Should().BeEmpty();
        }
    }

    [Fact]
    [Trait("AC", "P62-F01-card-paid-pot-withdrawal-06")]
    public async Task CardWithdrawal_PaidStatement_LocksFieldsAndDeleteButAllowsDescriptionAndCategory()
    {
        var operation = await CreatedAsync(CardRequest(statementMonth: 10));
        await MarkPaidAsync(2026, 10);

        var changedAmount = await UpdateAsync(operation.OperationId, CardRequest(amount: 100m, statementMonth: 10));
        var changedStatement = await UpdateAsync(operation.OperationId, CardRequest(statementMonth: 11));
        var deleted = await Client.DeleteAsync($"{Api}/accounts/operations/{operation.OperationId}");
        var relabelled = await UpdateAsync(operation.OperationId, CardRequest(statementMonth: 10, categoryId: MercadoId, description: "Lisbon"));

        using (new AssertionScope())
        {
            await ShouldBeProblemAsync(changedAmount, HttpStatusCode.BadRequest, LockedMessage);
            await ShouldBeProblemAsync(changedStatement, HttpStatusCode.BadRequest, LockedMessage);
            await ShouldBeProblemAsync(deleted, HttpStatusCode.BadRequest, "Unmark the statement paid before deleting this withdrawal.");
            relabelled.StatusCode.Should().Be(HttpStatusCode.OK);
            ExpensesOf(operation.OperationId).Should().ContainSingle(expense =>
                expense.Value == 120m && expense.Description == "Lisbon" && expense.Category.Id == Guid.Parse(MercadoId));
            MovementsOf(operation.OperationId).Should().ContainSingle(movement => movement.Amount == -120m && movement.Description == "Lisbon");
        }
    }

    [Fact]
    [Trait("AC", "P62-F01-card-paid-pot-withdrawal-07")]
    public async Task CardWithdrawal_SingleRecordEditOrDelete_Returns409()
    {
        var operation = await CreatedAsync(CardRequest());
        var charge = ExpensesOf(operation.OperationId).Single();
        var movement = MovementsOf(operation.OperationId).Single();

        var expenseUpdate = await Client.PutAsJsonAsync($"{Api}/expenses/{charge.Id}", new ExpenseUpdateDTO
        {
            Date = charge.Date,
            Description = "Changed",
            Value = 1m,
            CategoryId = charge.Category.Id,
            CreditCardId = Guid.Parse(AmexId)
        });
        var expenseDelete = await Client.DeleteAsync($"{Api}/expenses/{charge.Id}");
        var movementDelete = await Client.DeleteAsync($"{Api}/reserve/movements/{movement.Id}");

        using (new AssertionScope())
        {
            await ShouldBeProblemAsync(expenseUpdate, HttpStatusCode.Conflict, ExpenseMessage);
            await ShouldBeProblemAsync(expenseDelete, HttpStatusCode.Conflict, ExpenseMessage);
            await ShouldBeProblemAsync(movementDelete, HttpStatusCode.Conflict, MovementMessage);
            ExpensesOf(operation.OperationId).Should().ContainSingle(expense => expense.Value == 120m && expense.Description == "Flights");
        }
    }

    [Fact]
    [Trait("AC", "P62-F01-card-paid-pot-withdrawal-08")]
    public async Task CardWithdrawal_ReadsPending_FailedSaveWritesNothing_DataVersionUnchanged()
    {
        var operation = await CreatedAsync(CardRequest());
        var savedBefore = SavedCashFlowData();

        var rejected = await WithdrawAsync(CardRequest(statementMonth: 9));

        using (new AssertionScope())
        {
            operation.InvestmentWithdrawal.Should().Be("Pending");
            rejected.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            SavedCashFlowData().Should().Be(savedBefore);
            JsonNode.Parse(savedBefore)!["Version"]!.GetValue<int>().Should().Be(5);
        }
    }
}
