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
public class InvestmentWithdrawalOnStatementPaymentAcceptanceTests : ApiEndpointTests
{
    private const string BarclaysId = "7d3f0000-0000-0000-0000-000000000001";
    private const string ReservaId = "7d3f0000-0000-0000-0000-000000000002";
    private const string AmexId = "7d3f0000-0000-0000-0000-000000000003";
    private const string InvestimentoId = "7d3f0000-0000-0000-0000-000000000101";
    private const string ReservaCategoryId = "7d3f0000-0000-0000-0000-000000000301";
    private const string ViagemId = "7d3f0000-0000-0000-0000-000000000302";
    private const string MercadoId = "7d3f0000-0000-0000-0000-000000000303";
    private static readonly DateOnly ExpenseDate = new(2026, 9, 20);
    private static readonly DateOnly PaymentDate = new(2026, 10, 3);

    private static string SeededData(string? reservaCategoryId = ReservaCategoryId) => $$"""
        {
          "Accounts": [
            { "Id": "{{BarclaysId}}", "Name": "Barclays", "Template": "Bank", "Currency": "GBP", "IsActive": true, "OpeningBalance": { "Amount": 1000, "StartDate": "2026-01-01" }, "RoundUp": { "Enabled": false }, "Pots": null },
            { "Id": "{{AmexId}}", "Name": "Amex", "Template": "CreditCard", "Currency": "GBP", "IsActive": true, "OpeningBalance": null, "RoundUp": null },
            { "Id": "{{ReservaId}}", "Name": "Reserva", "Template": "Pots", "Currency": "GBP", "IsActive": true, "OpeningBalance": null, "RoundUp": null, "Pots": [
              { "Id": "{{InvestimentoId}}", "Name": "Investimento", "IsActive": true, "SplitPercentage": 100 }
            ], "CategoryId": {{(reservaCategoryId is null ? "null" : $"\"{reservaCategoryId}\"")}} }
          ],
          "Categories": [
            { "Id": "{{ReservaCategoryId}}", "Name": "Reserva", "Active": true, "IsInvestment": false, "IsTithe": false },
            { "Id": "{{ViagemId}}", "Name": "Viagem", "Active": true, "IsInvestment": false, "IsTithe": false },
            { "Id": "{{MercadoId}}", "Name": "Mercado", "Active": true, "IsInvestment": false, "IsTithe": false }
          ],
          "IncomeSources": [],
          "Incomes": [],
          "ReserveMovements": [
            { "Id": "7d3f0000-0000-0000-0000-000000000201", "BucketId": "{{InvestimentoId}}", "Amount": 500, "Date": "2026-09-01", "Description": "Split", "IncomeId": null }
          ],
          "Version": 5
        }
        """;

    public InvestmentWithdrawalOnStatementPaymentAcceptanceTests()
    {
        ReplaceCashFlowDataBeforeStart(SeededData());
    }

    private ICashFlowRepository Repository => Services.GetRequiredService<ICashFlowRepository>();

    private List<Expense> InvestmentWithdrawalsOf(Guid operationId) =>
        Repository.GetExpenses().Where(expense => expense.OperationId == operationId && expense.PaymentSourceAccount != null).ToList();

    private async Task<PotOperationDTO> WithdrawByCardAsync(decimal amount, string description = "Flights")
    {
        var response = await Client.PostAsJsonAsync($"{Api}/accounts/{ReservaId}/withdrawals", new PotWithdrawalRequestDTO
        {
            PotId = Guid.Parse(InvestimentoId),
            Amount = amount,
            Date = ExpenseDate,
            Description = description,
            CreditCardId = Guid.Parse(AmexId),
            StatementYear = 2026,
            StatementMonth = 9,
            CategoryId = Guid.Parse(ViagemId)
        });
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        return (await response.Content.ReadFromJsonAsync<PotOperationDTO>())!;
    }

    private async Task<Guid> SeptemberStatementIdAsync()
    {
        var statements = await Client.GetFromJsonAsync<List<AccountStatementDTO>>($"{Api}/accounts/{AmexId}/statements");
        return statements!.Single(statement => statement.Year == 2026 && statement.Month == 9).Id!.Value;
    }

    private async Task<HttpResponseMessage> MarkPaidAsync(Guid statementId) =>
        await Client.PostAsJsonAsync($"{Api}/card-statements/{statementId}/mark-paid", new MarkCardStatementPaidDTO
        {
            PaymentSourceBankId = Guid.Parse(BarclaysId),
            Date = PaymentDate
        });

    private async Task<decimal> CategoryTotalAsync(int month, string category)
    {
        var totals = await Client.GetFromJsonAsync<List<CategoryTotalDTO>>($"{Api}/expenses/month/2026/{month}/category-totals");
        return totals!.Where(total => total.Category == category).Sum(total => total.TotalValue);
    }

    private async Task<decimal> BarclaysBalanceAsync(int month) =>
        (await Client.GetFromJsonAsync<AccountDetailDTO>($"{Api}/accounts/{BarclaysId}/2026/{month}"))!.Balance;

    [Fact]
    [Trait("AC", "P62-F02-investment-withdrawal-on-statement-payment-01")]
    public async Task MarkPaid_WritesMinusXPerPotChargeOnThePayingBank()
    {
        var flights = await WithdrawByCardAsync(120m);
        var hotel = await WithdrawByCardAsync(80m, "Hotel");

        var response = await MarkPaidAsync(await SeptemberStatementIdAsync());

        using (new AssertionScope())
        {
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            foreach (var (operation, amount, description) in new[] { (flights, 120m, "Flights"), (hotel, 80m, "Hotel") })
            {
                var withdrawal = InvestmentWithdrawalsOf(operation.OperationId).Should().ContainSingle().Subject;
                withdrawal.Value.Should().Be(-amount);
                withdrawal.PaymentSourceAccount!.Id.Should().Be(Guid.Parse(BarclaysId));
                withdrawal.Date.Should().Be(PaymentDate);
                withdrawal.Category.Id.Should().Be(Guid.Parse(ReservaCategoryId));
                withdrawal.Description.Should().Be(description);
            }

            Repository.GetTransfers().Should().ContainSingle(transfer => transfer.Amount == 200m);
        }
    }

    [Fact]
    [Trait("AC", "P62-F02-investment-withdrawal-on-statement-payment-02")]
    public async Task MarkPaid_CardCreditCoversPart_WritesFullX()
    {
        var flights = await WithdrawByCardAsync(120m);
        var refund = await Client.PostAsJsonAsync($"{Api}/expenses", new ExpenseCreateDTO
        {
            Date = ExpenseDate,
            Description = "Refund",
            Value = -50m,
            CategoryId = Guid.Parse(MercadoId),
            CreditCardId = Guid.Parse(AmexId)
        });

        await MarkPaidAsync(await SeptemberStatementIdAsync());

        using (new AssertionScope())
        {
            refund.StatusCode.Should().Be(HttpStatusCode.OK);
            Repository.GetTransfers().Should().ContainSingle(transfer => transfer.Amount == 70m);
            InvestmentWithdrawalsOf(flights.OperationId).Should().ContainSingle(withdrawal => withdrawal.Value == -120m);
        }
    }

    [Fact]
    [Trait("AC", "P62-F02-investment-withdrawal-on-statement-payment-03")]
    public async Task SeptemberStatementPaidInOctober_BothSidesInSeptember_BankMovesOnPaymentDay()
    {
        await WithdrawByCardAsync(120m);
        var septemberBalanceBefore = await BarclaysBalanceAsync(9);

        await MarkPaidAsync(await SeptemberStatementIdAsync());

        using (new AssertionScope())
        {
            (await CategoryTotalAsync(9, "Viagem")).Should().Be(120m);
            (await CategoryTotalAsync(9, "Reserva")).Should().Be(-120m);
            (await CategoryTotalAsync(10, "Viagem")).Should().Be(0m);
            (await CategoryTotalAsync(10, "Reserva")).Should().Be(0m);
            (await BarclaysBalanceAsync(9)).Should().Be(septemberBalanceBefore);
            (await BarclaysBalanceAsync(10)).Should().Be(1000m);
        }
    }

    [Fact]
    [Trait("AC", "P62-F02-investment-withdrawal-on-statement-payment-04")]
    public async Task UnmarkPaid_RemovesTransferAndMinusX_OperationPendingAgain()
    {
        var flights = await WithdrawByCardAsync(120m);
        var statementId = await SeptemberStatementIdAsync();
        await MarkPaidAsync(statementId);

        var unmarked = await Client.PostAsync($"{Api}/card-statements/{statementId}/unmark-paid", null);
        var operation = await Client.GetFromJsonAsync<PotOperationDTO>($"{Api}/accounts/operations/{flights.OperationId}");

        using (new AssertionScope())
        {
            unmarked.StatusCode.Should().Be(HttpStatusCode.OK);
            Repository.GetTransfers().Should().BeEmpty();
            InvestmentWithdrawalsOf(flights.OperationId).Should().BeEmpty();
            operation!.InvestmentWithdrawal.Should().Be("Pending");
            operation.InvestmentWithdrawalBankId.Should().BeNull();
            operation.InvestmentWithdrawalDate.Should().BeNull();
        }
    }

    [Fact]
    [Trait("AC", "P62-F02-investment-withdrawal-on-statement-payment-05")]
    public async Task MarkPaid_PotsAccountWithoutCategory_Returns400AndSavesNothing()
    {
        ReplaceCashFlowDataBeforeStart(SeededData(reservaCategoryId: null));
        await WithdrawByCardAsync(120m);
        var statementId = await SeptemberStatementIdAsync();

        var response = await MarkPaidAsync(statementId);

        using (new AssertionScope())
        {
            await ShouldBeProblemAsync(response, HttpStatusCode.BadRequest, "Set a category on Reserva first.");
            Repository.GetTransfers().Should().BeEmpty();
            Repository.GetCardStatements().Single(statement => statement.Id == statementId).IsPaid.Should().BeFalse();
            Repository.GetExpenses().Should().NotContain(expense => expense.PaymentSourceAccount != null);
        }
    }

    [Fact]
    [Trait("AC", "P62-CF-cross-feature-01")]
    public async Task PaidOperation_ReadsDoneWithBankAndDate_FromF01Values()
    {
        var flights = await WithdrawByCardAsync(120m);

        await MarkPaidAsync(await SeptemberStatementIdAsync());
        var operation = await Client.GetFromJsonAsync<PotOperationDTO>($"{Api}/accounts/operations/{flights.OperationId}");
        var withdrawal = InvestmentWithdrawalsOf(flights.OperationId).Single();

        using (new AssertionScope())
        {
            operation!.InvestmentWithdrawal.Should().Be("Done");
            operation.InvestmentWithdrawalBankId.Should().Be(Guid.Parse(BarclaysId));
            operation.InvestmentWithdrawalDate.Should().Be(PaymentDate);
            withdrawal.OperationId.Should().Be(flights.OperationId);
            withdrawal.Value.Should().Be(-flights.Amount);
            withdrawal.Description.Should().Be(flights.Description);
            withdrawal.ReportingDate.Should().Be(new DateOnly(flights.StatementYear!.Value, flights.StatementMonth!.Value, 1));
        }
    }

    [Fact]
    [Trait("AC", "P62-F02-investment-withdrawal-on-statement-payment-06")]
    public async Task StatementReads_ListThePotWithdrawalsTheConfirmationsName()
    {
        var flights = await WithdrawByCardAsync(120m);

        var cardStatements = await Client.GetFromJsonAsync<List<AccountStatementDTO>>($"{Api}/accounts/{AmexId}/statements");
        var payable = await Client.GetFromJsonAsync<List<PayableStatementDTO>>($"{Api}/accounts/payable-statements");
        var viewed = await Client.GetFromJsonAsync<AccountDetailDTO>($"{Api}/accounts/{AmexId}/2026/9");

        using (new AssertionScope())
        {
            foreach (var withdrawals in new[]
            {
                cardStatements!.Single(statement => statement.Year == 2026 && statement.Month == 9).PotWithdrawals,
                payable!.Single(statement => statement.Year == 2026 && statement.Month == 9).PotWithdrawals,
                viewed!.ViewedStatement!.PotWithdrawals
            })
            {
                var withdrawal = withdrawals.Should().ContainSingle().Subject;
                withdrawal.OperationId.Should().Be(flights.OperationId);
                withdrawal.PotsAccountName.Should().Be("Reserva");
                withdrawal.PotName.Should().Be("Investimento");
                withdrawal.Amount.Should().Be(120m);
            }

            cardStatements!.Where(statement => statement.Month != 9).Should().OnlyContain(statement => statement.PotWithdrawals.Count == 0);
        }
    }
}
