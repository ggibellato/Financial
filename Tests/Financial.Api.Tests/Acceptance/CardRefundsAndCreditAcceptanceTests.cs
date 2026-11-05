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

    private Task<HttpResponseMessage> PostCardChargeAsync(DateOnly date, decimal value, DateOnly? invoiceDate = null) =>
        Client.PostAsJsonAsync($"{Api}/expenses", new ExpenseCreateDTO
        {
            Date = date,
            Description = value < 0 ? "Refund" : "Charge",
            Value = value,
            CategoryId = Guid.Parse(MercadoId),
            CreditCardId = Guid.Parse(AmexId),
            InvoiceDate = invoiceDate,
            CountsAsTithe = true
        });

    private async Task<ExpenseDTO> AddCardChargeAsync(int month, int day, decimal value)
    {
        var response = await PostCardChargeAsync(new DateOnly(2026, month, day), value);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<ExpenseDTO>())!;
    }

    private async Task<List<AccountStatementDTO>> StatementsAsync() =>
        (await Client.GetFromJsonAsync<List<AccountStatementDTO>>($"{Api}/accounts/{AmexId}/statements"))!;

    private Task<HttpResponseMessage> MarkPaidAsync(Guid statementId) =>
        Client.PostAsJsonAsync($"{Api}/card-statements/{statementId}/mark-paid", new MarkCardStatementPaidDTO { PaymentSourceBankId = Guid.Parse(BarclaysId) });

    private async Task<AccountDetailDTO> AmexDetailAsync(int month) =>
        (await Client.GetFromJsonAsync<AccountDetailDTO>($"{Api}/accounts/{AmexId}/2026/{month}"))!;

    private static AccountGroupMemberDTO AmexMember(IEnumerable<AccountTemplateGroupDTO>? groups) =>
        groups!.SelectMany(group => group.Members).Single(member => member.Name == "BA Amex");

    private static AccountStatementDTO Month(IEnumerable<AccountStatementDTO> rows, int month) => rows.Single(row => row.Month == month);

    private async Task<decimal> MercadoSpendAsync(int month)
    {
        var totals = await Client.GetFromJsonAsync<List<CategoryTotalDTO>>($"{Api}/expenses/month/2026/{month}/category-totals");
        return totals!.Single(total => total.Category == "Mercado").TotalValue;
    }

    [Fact]
    [Trait("AC", "P60-F15-card-refunds-and-credit-01")]
    public async Task AddRefund_SavesNegativeCharge_LowersOwedAndCategorySpendInStatementMonth()
    {
        await AddCardChargeAsync(11, 2, 100m);

        var refund = await AddCardChargeAsync(11, 3, -40m);

        var detail = await AmexDetailAsync(11);
        var row = (await Client.GetFromJsonAsync<List<AccountTransactionDTO>>($"{Api}/accounts/{AmexId}/transactions/2026/11"))!.Single(r => r.Id == refund.Id);
        using (new AssertionScope())
        {
            refund.Value.Should().Be(-40m);
            refund.InvoiceDate.Should().Be(new DateOnly(2026, 11, 1));
            detail.AllowedActions.Should().Contain("AddRefund");
            detail.TotalOwed.Should().Be(60m);
            detail.ViewedStatement!.OutstandingTotal.Should().Be(60m);
            (await MercadoSpendAsync(11)).Should().Be(60m);
            row.Kind.Should().Be("CardRefund");
            row.Amount.Should().Be(40m);
            row.CanEdit.Should().BeTrue();
        }
    }

    [Fact]
    [Trait("AC", "P60-F15-card-refunds-and-credit-01")]
    public async Task AddRefund_DatedAfterToday_Returns400()
    {
        await ShouldBeRefusedAsync(await PostCardChargeAsync(new DateOnly(2026, 11, 16), -40m), HttpStatusCode.BadRequest, "Date must not be in the future.");
    }

    [Fact]
    [Trait("AC", "P60-F15-card-refunds-and-credit-01")]
    public async Task AddRefund_IntoAPaidStatement_Returns400()
    {
        await ShouldBeRefusedAsync(await PostCardChargeAsync(new DateOnly(2026, 9, 20), -40m), HttpStatusCode.BadRequest, "This charge's statement is paid; unmark it first.");
    }

    [Fact]
    [Trait("AC", "P60-F15-card-refunds-and-credit-01")]
    public async Task AddRefund_ForAPaidMonthsPurchaseOnAnOpenStatement_IsSavedThere()
    {
        var response = await PostCardChargeAsync(new DateOnly(2026, 9, 20), -40m, new DateOnly(2026, 11, 1));
        response.EnsureSuccessStatusCode();

        var refund = (await response.Content.ReadFromJsonAsync<ExpenseDTO>())!;
        var november = Month(await StatementsAsync(), 11);

        using (new AssertionScope())
        {
            refund.InvoiceDate.Should().Be(new DateOnly(2026, 11, 1));
            november.Status.Should().Be("NothingDue");
            november.CreditCarried.Should().Be(40m);
        }
    }

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
    [Trait("AC", "P60-F15-card-refunds-and-credit-06")]
    public async Task TemplateGroups_Card_NextStatementSkipsNothingDue_TotalOwedNetsCredit()
    {
        await AddCardChargeAsync(10, 8, -35m);
        var inCredit = AmexMember(await Client.GetFromJsonAsync<List<AccountTemplateGroupDTO>>($"{Api}/accounts/groups/by-template/2026/11"));

        await AddCardChargeAsync(11, 2, 50m);
        var owing = AmexMember(await Client.GetFromJsonAsync<List<AccountTemplateGroupDTO>>($"{Api}/accounts/groups/by-template/2026/11"));

        using (new AssertionScope())
        {
            inCredit.NextStatement.Should().BeNull();
            inCredit.TotalOwed.Should().Be(-35m);
            owing.NextStatement.Should().BeEquivalentTo(new { Year = 2026, Month = 11, OutstandingTotal = 15m });
            owing.TotalOwed.Should().Be(15m);
            owing.Statement.Should().BeEquivalentTo(new { OutstandingTotal = 15m, IsPaid = false });
        }
    }

    [Fact]
    public async Task MonthlyCardStatements_OutstandingIsTheAmountToPay_ZeroWhileCreditCoversIt()
    {
        await AddCardChargeAsync(10, 3, 25m);
        await AddCardChargeAsync(10, 8, -60m);
        await AddCardChargeAsync(11, 2, 20m);

        var october = await Client.GetFromJsonAsync<List<CardStatementDTO>>($"{Api}/card-statements/2026/10");
        var november = await Client.GetFromJsonAsync<List<CardStatementDTO>>($"{Api}/card-statements/2026/11");

        using (new AssertionScope())
        {
            october.Should().ContainSingle().Which.Should().BeEquivalentTo(new { OutstandingTotal = 0m, AccumulatedOutstandingTotal = -15m, IsPaid = false });
            november.Should().ContainSingle().Which.Should().BeEquivalentTo(new { OutstandingTotal = 0m, AccumulatedOutstandingTotal = -15m, IsPaid = false });
        }
    }

    [Fact]
    [Trait("AC", "P60-F15-card-refunds-and-credit-02")]
    public async Task PaymentsDue_NothingDueMonthHasNoReminder_UntilAChargeRaisesIt()
    {
        var update = await Client.PutAsJsonAsync($"{Api}/credit-cards/{AmexId}", new CreditCardUpdateDTO { Name = "BA Amex", IsActive = true, NextInvoiceDueDate = new DateOnly(2026, 11, 18) });
        update.EnsureSuccessStatusCode();

        var nothingDue = await Client.GetFromJsonAsync<List<PaymentDueDTO>>($"{Api}/payments-due");
        await AddCardChargeAsync(11, 2, 20m);
        var unpaid = await Client.GetFromJsonAsync<List<PaymentDueDTO>>($"{Api}/payments-due");

        using (new AssertionScope())
        {
            nothingDue.Should().NotContain(payment => payment.Name == "BA Amex");
            unpaid.Should().ContainSingle(payment => payment.Name == "BA Amex");
        }
    }

    private Task<HttpResponseMessage> PostCreditRefundAsync(decimal amount, int month = 11) =>
        Client.PostAsJsonAsync($"{Api}/accounts/credit-refunds", new AccountTransferCreateDTO
        {
            Date = new DateOnly(2026, month, 10),
            SourceAccountId = Guid.Parse(AmexId),
            DestinationAccountId = Guid.Parse(BarclaysId),
            Amount = amount
        });

    private static async Task ShouldBeRefusedAsync(HttpResponseMessage response, HttpStatusCode status, string message)
    {
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        using (new AssertionScope())
        {
            response.StatusCode.Should().Be(status);
            problem!.Detail.Should().Be(message);
        }
    }

    [Fact]
    [Trait("AC", "P60-F15-card-refunds-and-credit-05")]
    public async Task CreditRefund_WritesOneTransfer_LowersCredit_CappedAtCredit_TransferRoutesReturn409()
    {
        const string RefundsCreditMessage = "This transfer refunds card credit; delete it from the card or bank screen instead.";
        await AddCardChargeAsync(10, 8, -60m);
        var bankBefore = (await Client.GetFromJsonAsync<AccountDetailDTO>($"{Api}/accounts/{BarclaysId}/2026/11"))!.Balance;

        await ShouldBeRefusedAsync(await PostCreditRefundAsync(60.01m), HttpStatusCode.BadRequest, "The card's credit is 60.00 GBP.");

        var created = await PostCreditRefundAsync(35m);
        created.EnsureSuccessStatusCode();
        var refund = (await created.Content.ReadFromJsonAsync<AccountTransferDTO>())!;
        var card = await AmexDetailAsync(11);
        var bankAfter = (await Client.GetFromJsonAsync<AccountDetailDTO>($"{Api}/accounts/{BarclaysId}/2026/11"))!.Balance;
        var november = Month(await StatementsAsync(), 11);
        var byMonth = await Client.GetFromJsonAsync<List<TransferDTO>>($"{Api}/transfers/month/2026/11");
        var byBank = await Client.GetFromJsonAsync<List<TransferDTO>>($"{Api}/transfers/bank/{BarclaysId}");
        var bankRows = await Client.GetFromJsonAsync<List<AccountTransactionDTO>>($"{Api}/accounts/{BarclaysId}/transactions/2026/11");

        var update = new TransferUpdateDTO { Date = refund.Date, SourceBankId = Guid.Parse(BarclaysId), DestinationBankId = Guid.Parse(BarclaysId), Amount = 1m };
        var accountUpdate = new AccountTransferUpdateDTO { Date = refund.Date, SourceAccountId = Guid.Parse(BarclaysId), DestinationAccountId = Guid.Parse(BarclaysId), Amount = 1m };
        await ShouldBeRefusedAsync(await Client.PutAsJsonAsync($"{Api}/transfers/{refund.Id}", update), HttpStatusCode.Conflict, RefundsCreditMessage);
        await ShouldBeRefusedAsync(await Client.DeleteAsync($"{Api}/transfers/{refund.Id}"), HttpStatusCode.Conflict, RefundsCreditMessage);
        await ShouldBeRefusedAsync(await Client.PutAsJsonAsync($"{Api}/accounts/transfers/{refund.Id}", accountUpdate), HttpStatusCode.Conflict, RefundsCreditMessage);
        await ShouldBeRefusedAsync(await Client.DeleteAsync($"{Api}/accounts/transfers/{refund.Id}"), HttpStatusCode.Conflict, RefundsCreditMessage);
        await ShouldBeRefusedAsync(await PostCreditRefundAsync(25.01m), HttpStatusCode.BadRequest, "The card's credit is 25.00 GBP.");

        using (new AssertionScope())
        {
            refund.Should().BeEquivalentTo(new { SourceAccountName = "BA Amex", DestinationAccountName = "Barclays", Amount = 35m, DestinationAmount = 35m });
            card.TotalOwed.Should().Be(-25m);
            bankAfter.Should().Be(bankBefore + 35m);
            november.Id.Should().NotBeNull();
            november.Should().BeEquivalentTo(new { Total = 35m, CreditIn = 60m, OutstandingTotal = 0m, CreditCarried = 25m, Status = "NothingDue" });
            byMonth.Should().BeEmpty();
            byBank.Should().NotContain(transfer => transfer.Id == refund.Id);
            bankRows.Should().ContainSingle(row => row.Id == refund.Id).Which.Amount.Should().Be(35m);
        }
    }

    [Fact]
    [Trait("AC", "P60-F15-card-refunds-and-credit-05")]
    public async Task CreditRefund_ShowsOnCardAndBankAsReadOnlyRows()
    {
        await AddCardChargeAsync(10, 8, -60m);
        var offered = (await AmexDetailAsync(11)).AllowedActions;

        var created = await PostCreditRefundAsync(60m);
        created.EnsureSuccessStatusCode();
        var refund = (await created.Content.ReadFromJsonAsync<AccountTransferDTO>())!;
        var bankRows = await Client.GetFromJsonAsync<List<AccountTransactionDTO>>($"{Api}/accounts/{BarclaysId}/transactions/2026/11");
        var cardRows = await Client.GetFromJsonAsync<List<AccountTransactionDTO>>($"{Api}/accounts/{AmexId}/transactions/2026/11");
        var offeredAfter = (await AmexDetailAsync(11)).AllowedActions;

        using (new AssertionScope())
        {
            offered.Should().Equal("AddCharge", "AddRefund", "RefundCreditToBank");
            offeredAfter.Should().Equal("AddCharge", "AddRefund");
            bankRows.Should().ContainSingle(row => row.Id == refund.Id).Which.Should().BeEquivalentTo(
                new { Kind = "CardCreditRefund", Description = "BA Amex credit refund", Counterpart = "BA Amex", Amount = 60m, CanEdit = false });
            cardRows.Should().ContainSingle(row => row.Id == refund.Id).Which.Should().BeEquivalentTo(
                new { Kind = "CardCreditRefund", Description = "BA Amex credit refund", Counterpart = "Barclays", Amount = -60m, CanEdit = false });
        }
    }

    [Fact]
    [Trait("AC", "P60-F15-card-refunds-and-credit-05")]
    public async Task CreditRefund_LaterChargeUsesTheCredit_ItsMonthCanStillBePaid()
    {
        await AddCardChargeAsync(10, 8, -60m);
        (await PostCreditRefundAsync(35m)).EnsureSuccessStatusCode();

        await AddCardChargeAsync(10, 9, 50m);
        var november = Month(await StatementsAsync(), 11);
        var paid = await MarkPaidAsync(november.Id!.Value);
        var monthly = await Client.GetFromJsonAsync<List<CardStatementDTO>>($"{Api}/card-statements/2026/11");

        using (new AssertionScope())
        {
            november.Should().BeEquivalentTo(new { CreditIn = 10m, OutstandingTotal = 25m, Status = "Unpaid" });
            paid.StatusCode.Should().Be(HttpStatusCode.OK);
            monthly.Should().ContainSingle().Which.IsPaid.Should().BeTrue();
        }
    }

    [Fact]
    [Trait("AC", "P60-F15-card-refunds-and-credit-05")]
    public async Task CreditRefund_MonthWithNoCharges_ShowsOnMonthlyAsNothingToPay()
    {
        await AddCardChargeAsync(10, 8, -60m);
        (await PostCreditRefundAsync(35m)).EnsureSuccessStatusCode();

        var monthly = await Client.GetFromJsonAsync<List<CardStatementDTO>>($"{Api}/card-statements/2026/11");

        monthly.Should().ContainSingle().Which.Should().BeEquivalentTo(new { OutstandingTotal = 0m, IsPaid = false });
    }

    [Fact]
    [Trait("AC", "P60-F15-card-refunds-and-credit-05")]
    public async Task CreditRefund_DatedBeforeTheCreditArose_Returns400()
    {
        await AddCardChargeAsync(11, 2, -60m);

        await ShouldBeRefusedAsync(
            await PostCreditRefundAsync(35m, month: 10),
            HttpStatusCode.BadRequest,
            "On that date the card had less credit than this refund; choose a later date.");
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
