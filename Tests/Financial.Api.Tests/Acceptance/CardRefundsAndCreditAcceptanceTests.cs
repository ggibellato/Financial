using System.Net;
using System.Net.Http.Json;
using Financial.CashFlow.Application.DTOs;
using FluentAssertions;
using FluentAssertions.Execution;
using Microsoft.AspNetCore.Mvc;

namespace Financial.Api.Tests.Acceptance;

[Trait("Category", "Integration")]
public class CardRefundsAndCreditAcceptanceTests : ApiEndpointTests
{
    private const string Api = "/api/v1/financial";
    private const string BarclaysId = "8f3b1c1a-2e3a-4b1a-9a7f-100000000001";
    private const string MercadoId = "8f3b1c1a-2e3a-4b1a-9a7f-200000000001";
    private const string AmexId = "8f3b1c1a-2e3a-4b1a-9a7f-500000000001";
    private const string SeptemberChargeId = "8f3b1c1a-2e3a-4b1a-9a7f-600000000001";
    private const string SeptemberPaymentId = "8f3b1c1a-2e3a-4b1a-9a7f-800000000001";
    private const string SeptemberStatementId = "8f3b1c1a-2e3a-4b1a-9a7f-700000000001";
    private const string NothingToPayMessage = "There is nothing to pay on this statement.";

    private const string SeededData = $$"""
        {
          "Accounts": [
            { "Id": "{{BarclaysId}}", "Name": "Barclays", "Template": "Bank", "Currency": "GBP", "IsActive": true, "OpeningBalance": { "Amount": 1000, "StartDate": "2026-01-01" }, "RoundUp": { "Enabled": false } },
            { "Id": "{{AmexId}}", "Name": "BA Amex", "Template": "CreditCard", "Currency": "GBP", "IsActive": true, "OpeningBalance": null, "RoundUp": null }
          ],
          "Categories": [
            { "Id": "{{MercadoId}}", "Name": "Mercado", "Active": true, "IsInvestment": false, "IsTithe": false }
          ],
          "Expenses": [
            { "Id": "{{SeptemberChargeId}}", "Date": "2026-09-10", "Description": "September taxi", "Value": 512.30, "CategoryId": "{{MercadoId}}", "PaymentSourceAccountId": null, "CreditCardId": "{{AmexId}}", "ChargeDate": "2026-09-10", "InvoiceDate": "2026-09-01", "RoundUpAmount": null, "CountsAsTithe": true }
          ],
          "Transfers": [
            { "Id": "{{SeptemberPaymentId}}", "Date": "2026-09-27", "SourceAccountId": "{{BarclaysId}}", "DestinationAccountId": "{{AmexId}}", "Amount": 512.30 }
          ],
          "CardStatements": [
            { "Id": "{{SeptemberStatementId}}", "CreditCardId": "{{AmexId}}", "Year": 2026, "Month": 9, "IsPaid": true, "DueDate": "2026-09-28", "PaymentTransferId": "{{SeptemberPaymentId}}" }
          ],
          "Version": 4
        }
        """;

    public CardRefundsAndCreditAcceptanceTests()
        : base(timeProvider: TestClock.At(new DateTimeOffset(2026, 11, 15, 12, 0, 0, TimeSpan.Zero)))
    {
        ReplaceCashFlowDataBeforeStart(SeededData);
    }

    private async Task<ExpenseDTO> AddCardChargeAsync(int month, int day, decimal value)
    {
        var response = await Client.PostAsJsonAsync($"{Api}/expenses", new ExpenseCreateDTO
        {
            Date = new DateOnly(2026, month, day),
            Description = value < 0 ? "Refund" : "Charge",
            Value = value,
            CategoryId = Guid.Parse(MercadoId),
            CreditCardId = Guid.Parse(AmexId),
            CountsAsTithe = true
        });
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<ExpenseDTO>())!;
    }

    private async Task<List<AccountStatementDTO>> StatementsAsync() =>
        (await Client.GetFromJsonAsync<List<AccountStatementDTO>>($"{Api}/accounts/{AmexId}/statements"))!;

    private Task<HttpResponseMessage> MarkPaidAsync(Guid statementId) =>
        Client.PostAsJsonAsync($"{Api}/card-statements/{statementId}/mark-paid", new MarkCardStatementPaidDTO { PaymentSourceBankId = Guid.Parse(BarclaysId) });

    private async Task<AccountDetailDTO> AmexDetailAsync(int month) =>
        (await Client.GetFromJsonAsync<AccountDetailDTO>($"{Api}/accounts/{AmexId}/2026/{month}"))!;

    private static AccountStatementDTO Month(IEnumerable<AccountStatementDTO> rows, int month) => rows.Single(row => row.Month == month);

    [Fact]
    [Trait("AC", "P60-F15-card-refunds-and-credit-02")]
    public async Task StatementAtOrBelowZero_IsNothingDueAndNotPayable_UntilAChargeRaisesIt()
    {
        await AddCardChargeAsync(10, 3, 25m);
        await AddCardChargeAsync(10, 8, -25m);

        var nothingDue = Month(await StatementsAsync(), 10);
        var refused = await MarkPaidAsync(nothingDue.Id!.Value);
        var problem = await refused.Content.ReadFromJsonAsync<ProblemDetails>();

        await AddCardChargeAsync(10, 9, 12m);
        var raised = Month(await StatementsAsync(), 10);
        var paid = await MarkPaidAsync(raised.Id!.Value);

        using (new AssertionScope())
        {
            nothingDue.Status.Should().Be("NothingDue");
            nothingDue.OutstandingTotal.Should().Be(0m);
            refused.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            problem!.Detail.Should().Be(NothingToPayMessage);
            raised.Status.Should().Be("Unpaid");
            raised.OutstandingTotal.Should().Be(12m);
            paid.StatusCode.Should().Be(HttpStatusCode.OK);
        }
    }

    [Fact]
    [Trait("AC", "P60-F15-card-refunds-and-credit-03")]
    public async Task AccountStatements_ListEveryMonthFromFirstChargeToCurrent_EmptyMonthsNothingDue()
    {
        var rows = await StatementsAsync();

        using (new AssertionScope())
        {
            rows.Select(row => (row.Year, row.Month)).Should().Equal((2026, 11), (2026, 10), (2026, 9));
            rows[0].Should().BeEquivalentTo(new { Id = (Guid?)null, Total = 0m, CreditIn = 0m, OutstandingTotal = 0m, CreditCarried = 0m, Status = "NothingDue", IsPaid = false });
            rows[1].Should().BeEquivalentTo(new { Id = (Guid?)null, Total = 0m, Status = "NothingDue" });
            rows[2].Should().BeEquivalentTo(new
            {
                Id = (Guid?)Guid.Parse(SeptemberStatementId),
                Total = 512.30m,
                OutstandingTotal = 0m,
                Status = "Paid",
                IsPaid = true,
                DueDate = new DateOnly(2026, 9, 28),
                PaidOn = new DateOnly(2026, 9, 27),
                PaidAmount = 512.30m,
                PaidFromAccountName = "Barclays"
            });
        }
    }

    [Fact]
    [Trait("AC", "P60-F15-card-refunds-and-credit-04")]
    public async Task Credit_LowersNextAmountToPay_AndCarriesWhileLarger()
    {
        await AddCardChargeAsync(10, 3, 25m);
        await AddCardChargeAsync(10, 8, -125m);
        await AddCardChargeAsync(11, 2, 40m);

        var whileLarger = await StatementsAsync();

        await AddCardChargeAsync(11, 4, 100m);
        var rows = await StatementsAsync();
        var november = Month(rows, 11);
        var paid = await MarkPaidAsync(november.Id!.Value);
        var transactions = await Client.GetFromJsonAsync<List<AccountTransactionDTO>>($"{Api}/accounts/{BarclaysId}/transactions/2026/11");

        using (new AssertionScope())
        {
            Month(whileLarger, 10).Should().BeEquivalentTo(new { Total = -100m, OutstandingTotal = 0m, CreditCarried = 100m, Status = "NothingDue" });
            Month(whileLarger, 11).Should().BeEquivalentTo(new { Total = 40m, CreditIn = 100m, OutstandingTotal = 0m, CreditCarried = 60m, Status = "NothingDue" });
            november.Should().BeEquivalentTo(new { Total = 140m, CreditIn = 100m, OutstandingTotal = 40m, CreditCarried = 0m, Status = "Unpaid" });
            paid.StatusCode.Should().Be(HttpStatusCode.OK);
            transactions!.Should().ContainSingle(row => row.Kind == "CardPayment").Which.Amount.Should().Be(-40m);
        }
    }

    [Fact]
    public async Task PayableStatements_ListOnlyMonthsWithAnAmountToPay_AfterTheCredit()
    {
        await AddCardChargeAsync(10, 8, -35m);
        await AddCardChargeAsync(11, 2, 50m);

        var payable = await Client.GetFromJsonAsync<List<PayableStatementDTO>>($"{Api}/accounts/payable-statements");

        payable.Should().ContainSingle().Which.Should().BeEquivalentTo(new { Year = 2026, Month = 11, Total = 15m, CreditCardName = "BA Amex" });
    }

    [Fact]
    [Trait("AC", "P60-F15-card-refunds-and-credit-07")]
    public async Task AccountDetail_Card_ReturnsViewedStatementFromTheLedger_AndKeepsBalance()
    {
        await AddCardChargeAsync(10, 28, 100m);
        await AddCardChargeAsync(11, 2, 40m);
        var october = await AmexDetailAsync(10);
        var september = await AmexDetailAsync(9);
        var beforeFirst = await AmexDetailAsync(8);
        var bank = await Client.GetFromJsonAsync<AccountDetailDTO>($"{Api}/accounts/{BarclaysId}/2026/10");

        using (new AssertionScope())
        {
            october.ViewedStatement.Should().BeEquivalentTo(new { Year = 2026, Month = 10, Total = 100m, OutstandingTotal = 100m, Status = "Unpaid" });
            october.Balance.Should().Be(-100m);
            october.TotalOwed.Should().Be(140m);
            september.ViewedStatement.Should().BeEquivalentTo(new { Month = 9, OutstandingTotal = 0m, Status = "Paid", PaidAmount = 512.30m, PaidOn = new DateOnly(2026, 9, 27) });
            beforeFirst.ViewedStatement.Should().BeNull();
            bank!.ViewedStatement.Should().BeNull();
        }
    }
}
