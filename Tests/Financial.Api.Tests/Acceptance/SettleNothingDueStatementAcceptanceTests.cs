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
public class SettleNothingDueStatementAcceptanceTests : ApiEndpointTests
{
    private const string BarclaysId = "8e4a0000-0000-0000-0000-000000000001";
    private const string ReservaId = "8e4a0000-0000-0000-0000-000000000002";
    private const string AmexId = "8e4a0000-0000-0000-0000-000000000003";
    private const string InvestimentoId = "8e4a0000-0000-0000-0000-000000000101";
    private const string ReservaCategoryId = "8e4a0000-0000-0000-0000-000000000301";
    private const string ViagemId = "8e4a0000-0000-0000-0000-000000000302";
    private const string MercadoId = "8e4a0000-0000-0000-0000-000000000303";
    private static readonly DateOnly ExpenseDate = new(2026, 9, 20);
    private static readonly DateOnly SettledOn = new(2026, 10, 3);

    private const string SeededData = $$"""
        {
          "Accounts": [
            { "Id": "{{BarclaysId}}", "Name": "Barclays", "Template": "Bank", "Currency": "GBP", "IsActive": true, "OpeningBalance": { "Amount": 1000, "StartDate": "2026-01-01" }, "RoundUp": { "Enabled": false }, "Pots": null },
            { "Id": "{{AmexId}}", "Name": "Amex", "Template": "CreditCard", "Currency": "GBP", "IsActive": true, "OpeningBalance": null, "RoundUp": null },
            { "Id": "{{ReservaId}}", "Name": "Reserva", "Template": "Pots", "Currency": "GBP", "IsActive": true, "OpeningBalance": null, "RoundUp": null, "Pots": [
              { "Id": "{{InvestimentoId}}", "Name": "Investimento", "IsActive": true, "SplitPercentage": 100 }
            ], "CategoryId": "{{ReservaCategoryId}}" }
          ],
          "Categories": [
            { "Id": "{{ReservaCategoryId}}", "Name": "Reserva", "Active": true, "IsInvestment": false, "IsTithe": false },
            { "Id": "{{ViagemId}}", "Name": "Viagem", "Active": true, "IsInvestment": false, "IsTithe": false },
            { "Id": "{{MercadoId}}", "Name": "Mercado", "Active": true, "IsInvestment": false, "IsTithe": false }
          ],
          "IncomeSources": [],
          "Incomes": [],
          "ReserveMovements": [
            { "Id": "8e4a0000-0000-0000-0000-000000000201", "BucketId": "{{InvestimentoId}}", "Amount": 500, "Date": "2026-09-01", "Description": "Split", "IncomeId": null }
          ],
          "Version": 5
        }
        """;

    public SettleNothingDueStatementAcceptanceTests()
    {
        ReplaceCashFlowDataBeforeStart(SeededData);
    }

    private ICashFlowRepository Repository => Services.GetRequiredService<ICashFlowRepository>();

    private List<Expense> InvestmentWithdrawalsOf(Guid operationId) =>
        Repository.GetExpenses().Where(expense => expense.OperationId == operationId && expense.PaymentSourceAccount != null).ToList();

    private async Task<PotOperationDTO> WithdrawByCardAsync(decimal amount)
    {
        var response = await Client.PostAsJsonAsync($"{Api}/accounts/{ReservaId}/withdrawals", new PotWithdrawalRequestDTO
        {
            PotId = Guid.Parse(InvestimentoId),
            Amount = amount,
            Date = ExpenseDate,
            Description = "Flights",
            CreditCardId = Guid.Parse(AmexId),
            StatementYear = 2026,
            StatementMonth = 9,
            CategoryId = Guid.Parse(ViagemId)
        });
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        return (await response.Content.ReadFromJsonAsync<PotOperationDTO>())!;
    }

    private Task<HttpResponseMessage> AddCardExpenseAsync(decimal value) =>
        Client.PostAsJsonAsync($"{Api}/expenses", new ExpenseCreateDTO
        {
            Date = ExpenseDate,
            Description = "Card entry",
            Value = value,
            CategoryId = Guid.Parse(MercadoId),
            CreditCardId = Guid.Parse(AmexId),
            InvoiceDate = new DateOnly(2026, 9, 1)
        });

    private async Task<PotOperationDTO> NothingDueStatementWithPotChargeAsync()
    {
        var flights = await WithdrawByCardAsync(120m);
        (await AddCardExpenseAsync(-120m)).StatusCode.Should().Be(HttpStatusCode.OK);
        return flights;
    }

    private async Task<AccountStatementDTO> SeptemberAsync()
    {
        var statements = await Client.GetFromJsonAsync<List<AccountStatementDTO>>($"{Api}/accounts/{AmexId}/statements");
        return statements!.Single(statement => statement.Year == 2026 && statement.Month == 9);
    }

    private async Task<HttpResponseMessage> SettleAsync(DateOnly? date = null) =>
        await Client.PostAsJsonAsync($"{Api}/card-statements/{(await SeptemberAsync()).Id}/settle-pot-withdrawals", new SettlePotWithdrawalsDTO
        {
            PaymentSourceBankId = Guid.Parse(BarclaysId),
            Date = date ?? SettledOn
        });

    private async Task<HttpResponseMessage> UnsettleAsync() =>
        await Client.PostAsync($"{Api}/card-statements/{(await SeptemberAsync()).Id}/unsettle-pot-withdrawals", null);

    [Fact]
    [Trait("AC", "P62-F04-settle-a-nothing-due-statement-02")]
    public async Task Settle_WritesMinusXOnTheChosenBankAndDate_NoTransfer_CreditUnchanged()
    {
        var flights = await NothingDueStatementWithPotChargeAsync();
        var before = await SeptemberAsync();

        var response = await SettleAsync();
        var after = await SeptemberAsync();

        using (new AssertionScope())
        {
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var withdrawal = InvestmentWithdrawalsOf(flights.OperationId).Should().ContainSingle().Subject;
            withdrawal.Value.Should().Be(-120m);
            withdrawal.PaymentSourceAccount!.Id.Should().Be(Guid.Parse(BarclaysId));
            withdrawal.Date.Should().Be(SettledOn);
            Repository.GetTransfers().Should().BeEmpty();
            before.Status.Should().Be("NothingDue");
            after.Status.Should().Be("NothingDue");
            after.SettledOn.Should().Be(SettledOn);
            after.IsPaid.Should().BeFalse();
            after.OutstandingTotal.Should().Be(before.OutstandingTotal);
            after.CreditCarried.Should().Be(before.CreditCarried);
        }
    }

    [Fact]
    [Trait("AC", "P62-F04-settle-a-nothing-due-statement-03")]
    public async Task Unsettle_RemovesMinusX_StatementNothingDueAgain()
    {
        var flights = await NothingDueStatementWithPotChargeAsync();
        await SettleAsync();

        var response = await UnsettleAsync();
        var september = await SeptemberAsync();
        var operation = await Client.GetFromJsonAsync<PotOperationDTO>($"{Api}/accounts/operations/{flights.OperationId}");

        using (new AssertionScope())
        {
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            InvestmentWithdrawalsOf(flights.OperationId).Should().BeEmpty();
            september.SettledOn.Should().BeNull();
            september.Status.Should().Be("NothingDue");
            operation!.InvestmentWithdrawal.Should().Be("Pending");
        }
    }

    [Fact]
    [Trait("AC", "P62-F04-settle-a-nothing-due-statement-04")]
    public async Task Settled_RejectsNewCharges_FutureDate400()
    {
        await NothingDueStatementWithPotChargeAsync();
        var futureDate = await SettleAsync(new DateOnly(2026, 10, 6));
        await SettleAsync();
        var newCharge = await AddCardExpenseAsync(15m);

        using (new AssertionScope())
        {
            await ShouldBeProblemAsync(futureDate, HttpStatusCode.BadRequest, "Date must not be in the future.");
            newCharge.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            (await ProblemDetailAsync(newCharge)).Should().Contain("paid");
        }
    }

    [Fact]
    [Trait("AC", "P62-F04-settle-a-nothing-due-statement-04")]
    public async Task Settle_StatementWithAnAmountToPay_Returns409()
    {
        await WithdrawByCardAsync(120m);

        var response = await SettleAsync();

        await ShouldBeProblemAsync(response, HttpStatusCode.Conflict, "This statement now has an amount to pay; mark it paid instead.");
    }

    [Fact]
    [Trait("AC", "P62-CF-cross-feature-04")]
    public async Task Settle_WritesRecordsLikeStatementPayment()
    {
        var flights = await NothingDueStatementWithPotChargeAsync();

        await SettleAsync();
        var operation = await Client.GetFromJsonAsync<PotOperationDTO>($"{Api}/accounts/operations/{flights.OperationId}");
        var withdrawal = InvestmentWithdrawalsOf(flights.OperationId).Single();

        using (new AssertionScope())
        {
            withdrawal.Category.Id.Should().Be(Guid.Parse(ReservaCategoryId));
            withdrawal.OperationId.Should().Be(flights.OperationId);
            withdrawal.Description.Should().Be(flights.Description);
            withdrawal.ReportingDate.Should().Be(new DateOnly(2026, 9, 1));
            operation!.InvestmentWithdrawal.Should().Be("Done");
            operation.InvestmentWithdrawalBankId.Should().Be(Guid.Parse(BarclaysId));
            operation.InvestmentWithdrawalDate.Should().Be(SettledOn);
        }
    }
}
