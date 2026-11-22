using System.Net;
using System.Net.Http.Json;
using Financial.CashFlow.Application.DTOs;
using Financial.CashFlow.Application.Interfaces;
using Financial.CashFlow.Domain.Entities.Transactions;
using FluentAssertions;
using FluentAssertions.Execution;
using Microsoft.Extensions.DependencyInjection;

namespace Financial.Api.Tests.Acceptance;

[Trait("Category", "Integration")]
public class PotWithdrawalRoundUpAcceptanceTests : ApiEndpointTests
{
    private const string ChaseId = "8e5a0000-0000-0000-0000-000000000001";
    private const string BarclaysId = "8e5a0000-0000-0000-0000-000000000002";
    private const string AmexId = "8e5a0000-0000-0000-0000-000000000003";
    private const string ReservaId = "8e5a0000-0000-0000-0000-000000000004";
    private const string InvestimentoId = "8e5a0000-0000-0000-0000-000000000101";
    private const string ReservaCategoryId = "8e5a0000-0000-0000-0000-000000000301";
    private const string SaudeId = "8e5a0000-0000-0000-0000-000000000302";
    private static readonly DateOnly WithdrawalDate = new(2026, 9, 20);

    private const string SeededData = $$"""
        {
          "Accounts": [
            { "Id": "{{ChaseId}}", "Name": "Chase", "Template": "Bank", "Currency": "GBP", "IsActive": true, "OpeningBalance": { "Amount": 1000, "StartDate": "2026-01-01" }, "RoundUp": { "Enabled": true }, "Pots": null },
            { "Id": "{{BarclaysId}}", "Name": "Barclays", "Template": "Bank", "Currency": "GBP", "IsActive": true, "OpeningBalance": { "Amount": 1000, "StartDate": "2026-01-01" }, "RoundUp": { "Enabled": false }, "Pots": null },
            { "Id": "{{AmexId}}", "Name": "Amex", "Template": "CreditCard", "Currency": "GBP", "IsActive": true, "OpeningBalance": null, "RoundUp": null },
            { "Id": "{{ReservaId}}", "Name": "Reserva", "Template": "Pots", "Currency": "GBP", "IsActive": true, "OpeningBalance": null, "RoundUp": null, "Pots": [
              { "Id": "{{InvestimentoId}}", "Name": "Investimento", "IsActive": true, "SplitPercentage": 100 }
            ], "CategoryId": "{{ReservaCategoryId}}" }
          ],
          "Categories": [
            { "Id": "{{ReservaCategoryId}}", "Name": "Reserva", "Active": true, "IsInvestment": false, "IsTithe": false },
            { "Id": "{{SaudeId}}", "Name": "Saude", "Active": true, "IsInvestment": false, "IsTithe": false }
          ],
          "IncomeSources": [],
          "Incomes": [],
          "ReserveMovements": [
            { "Id": "8e5a0000-0000-0000-0000-000000000201", "BucketId": "{{InvestimentoId}}", "Amount": 500, "Date": "2026-09-01", "Description": "Split", "IncomeId": null }
          ],
          "Version": 5
        }
        """;

    public PotWithdrawalRoundUpAcceptanceTests()
    {
        ReplaceCashFlowDataBeforeStart(SeededData);
    }

    private ICashFlowRepository Repository => Services.GetRequiredService<ICashFlowRepository>();

    private List<Expense> ExpensesOf(Guid operationId) => Repository.GetExpenses().Where(expense => expense.OperationId == operationId).ToList();

    private static PotWithdrawalRequestDTO Request(decimal? roundUpAmount, string? bankId = ChaseId, string? cardId = null) => new()
    {
        PotId = Guid.Parse(InvestimentoId),
        Amount = 30.30m,
        Date = WithdrawalDate,
        Description = "Dentist",
        BankId = bankId is null ? null : Guid.Parse(bankId),
        CreditCardId = cardId is null ? null : Guid.Parse(cardId),
        StatementYear = cardId is null ? null : 2026,
        StatementMonth = cardId is null ? null : 9,
        CategoryId = Guid.Parse(SaudeId),
        RoundUpAmount = roundUpAmount
    };

    private Task<HttpResponseMessage> WithdrawAsync(PotWithdrawalRequestDTO request) =>
        Client.PostAsJsonAsync($"{Api}/accounts/{ReservaId}/withdrawals", request);

    private async Task<PotOperationDTO> WithdrawCreatedAsync(PotWithdrawalRequestDTO request)
    {
        var response = await WithdrawAsync(request);
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        return (await response.Content.ReadFromJsonAsync<PotOperationDTO>())!;
    }

    [Fact]
    [Trait("AC", "P62-F05-round-up-on-bank-paid-withdrawal-02")]
    public async Task Withdraw_FromRoundUpBank_RecordsRoundUpOnTheSpendOnly()
    {
        var operation = await WithdrawCreatedAsync(Request(0.70m));
        var expenses = ExpensesOf(operation.OperationId);
        var chase = Repository.GetAccounts().Single(account => account.Id == Guid.Parse(ChaseId));

        using (new AssertionScope())
        {
            operation.RoundUpAmount.Should().Be(0.70m);
            Repository.GetPotTransactions().Should().ContainSingle(movement => movement.OperationId == operation.OperationId && movement.Amount == -30.30m);
            expenses.Select(expense => (expense.Value, expense.RoundUpAmount)).Should().BeEquivalentTo([(-30.30m, (decimal?)null), (30.30m, (decimal?)0.70m)]);
            expenses.Sum(expense => expense.ContributionTo(chase)).Should().Be(-0.70m);
        }
    }

    [Fact]
    [Trait("AC", "P62-F05-round-up-on-bank-paid-withdrawal-03")]
    public async Task Withdraw_RoundUpOnBankWithoutRoundUp_Returns400()
    {
        var response = await WithdrawAsync(Request(0.70m, bankId: BarclaysId));

        await ShouldBeProblemAsync(response, HttpStatusCode.BadRequest, "Bank 'Barclays' does not support round-up.");
    }

    [Fact]
    [Trait("AC", "P62-F05-round-up-on-bank-paid-withdrawal-03")]
    public async Task Withdraw_RoundUpWithCard_Returns400()
    {
        var response = await WithdrawAsync(Request(0.70m, bankId: null, cardId: AmexId));

        await ShouldBeProblemAsync(response, HttpStatusCode.BadRequest, "Round-up only applies to a withdrawal paid from a bank.");
    }

    [Fact]
    [Trait("AC", "P62-F05-round-up-on-bank-paid-withdrawal-04")]
    public async Task EditWithdrawal_ChangesTheRoundUp()
    {
        var created = await WithdrawCreatedAsync(Request(0.70m));

        var response = await Client.PutAsJsonAsync($"{Api}/accounts/{ReservaId}/withdrawals/{created.OperationId}", Request(0m));
        var edited = await response.Content.ReadFromJsonAsync<PotOperationDTO>();

        using (new AssertionScope())
        {
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            edited!.RoundUpAmount.Should().Be(0m);
            ExpensesOf(created.OperationId).Single(expense => expense.Value > 0).RoundUpAmount.Should().Be(0m);
        }
    }

    [Fact]
    [Trait("AC", "P62-F05-round-up-on-bank-paid-withdrawal-04")]
    public async Task EditWithdrawal_WithoutARoundUp_AddsNone()
    {
        var created = await WithdrawCreatedAsync(Request(null));

        var response = await Client.PutAsJsonAsync($"{Api}/accounts/{ReservaId}/withdrawals/{created.OperationId}", Request(null));

        using (new AssertionScope())
        {
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            ExpensesOf(created.OperationId).Should().OnlyContain(expense => expense.RoundUpAmount == null);
        }
    }
}
