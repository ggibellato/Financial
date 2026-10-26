using System.Net;
using System.Net.Http.Json;
using Financial.CashFlow.Application.DTOs;
using Financial.CashFlow.Application.Interfaces;
using FluentAssertions;
using FluentAssertions.Execution;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

namespace Financial.Api.Tests.Acceptance;

[Trait("Category", "Integration")]
public class StatementPaymentAcceptanceTests : ApiEndpointTests
{
    private const string Api = "/api/v1/financial";
    private const string BarclaysId = "8f3b1c1a-2e3a-4b1a-9a7f-100000000001";
    private const string MercadoId = "8f3b1c1a-2e3a-4b1a-9a7f-200000000001";
    private const string CasaId = "8f3b1c1a-2e3a-4b1a-9a7f-200000000002";
    private const string AmexId = "8f3b1c1a-2e3a-4b1a-9a7f-500000000001";
    private const string VisaId = "8f3b1c1a-2e3a-4b1a-9a7f-500000000002";
    private const string SeptemberChargeId = "8f3b1c1a-2e3a-4b1a-9a7f-600000000001";
    private const string OctoberChargeId = "8f3b1c1a-2e3a-4b1a-9a7f-600000000002";
    private const string SeptemberStatementId = "8f3b1c1a-2e3a-4b1a-9a7f-700000000001";
    private const string OctoberStatementId = "8f3b1c1a-2e3a-4b1a-9a7f-700000000002";
    private const string NovemberStatementId = "8f3b1c1a-2e3a-4b1a-9a7f-700000000003";
    private const string PaidStatementMessage = "This charge's statement is paid; unmark it first.";

    private const string SeededData = $$"""
        {
          "Accounts": [
            { "Id": "{{BarclaysId}}", "Name": "Barclays", "Template": "Bank", "Currency": "GBP", "IsActive": true, "OpeningBalance": { "Amount": 1000, "StartDate": "2026-01-01" }, "RoundUp": { "Enabled": false } },
            { "Id": "{{AmexId}}", "Name": "BA Amex", "Template": "CreditCard", "Currency": "GBP", "IsActive": true, "OpeningBalance": null, "RoundUp": null },
            { "Id": "{{VisaId}}", "Name": "Platinum Visa 8003", "Template": "CreditCard", "Currency": "GBP", "IsActive": true, "OpeningBalance": null, "RoundUp": null }
          ],
          "Categories": [
            { "Id": "{{MercadoId}}", "Name": "Mercado", "Active": true, "IsInvestment": false, "IsTithe": false },
            { "Id": "{{CasaId}}", "Name": "Casa", "Active": true, "IsInvestment": false, "IsTithe": false }
          ],
          "Expenses": [
            { "Id": "{{SeptemberChargeId}}", "Date": "2026-09-10", "Description": "September taxi", "Value": 10, "CategoryId": "{{MercadoId}}", "PaymentSourceAccountId": null, "CreditCardId": "{{AmexId}}", "ChargeDate": "2026-09-10", "InvoiceDate": "2026-09-01", "RoundUpAmount": null, "CountsAsTithe": true },
            { "Id": "{{OctoberChargeId}}", "Date": "2026-10-03", "Description": "Taxi", "Value": 30, "CategoryId": "{{MercadoId}}", "PaymentSourceAccountId": null, "CreditCardId": "{{AmexId}}", "ChargeDate": "2026-10-03", "InvoiceDate": "2026-10-01", "RoundUpAmount": null, "CountsAsTithe": true }
          ],
          "CardStatements": [
            { "Id": "{{SeptemberStatementId}}", "CreditCardId": "{{AmexId}}", "Year": 2026, "Month": 9, "IsPaid": true, "DueDate": "2026-10-03" },
            { "Id": "{{OctoberStatementId}}", "CreditCardId": "{{AmexId}}", "Year": 2026, "Month": 10, "IsPaid": false, "DueDate": null },
            { "Id": "{{NovemberStatementId}}", "CreditCardId": "{{AmexId}}", "Year": 2026, "Month": 11, "IsPaid": false, "DueDate": "2026-12-03" }
          ],
          "Version": 3
        }
        """;

    public StatementPaymentAcceptanceTests()
        : base(timeProvider: TestClock.At(new DateTimeOffset(2026, 10, 15, 12, 0, 0, TimeSpan.FromHours(1))))
    {
        ReplaceCashFlowDataBeforeStart(SeededData);
    }

    private Task<List<CardStatementDTO>?> StatementsAsync(int year, int month) =>
        Client.GetFromJsonAsync<List<CardStatementDTO>>($"{Api}/card-statements/{year}/{month}");

    private Task<HttpResponseMessage> MarkPaidAsync(string statementId) =>
        Client.PostAsJsonAsync($"{Api}/card-statements/{statementId}/mark-paid", new MarkCardStatementPaidDTO { PaymentSourceBankId = Guid.Parse(BarclaysId) });

    private async Task<List<ExpenseDTO>> UnpaidChargesAsync(int month) =>
        (await Client.GetFromJsonAsync<List<ExpenseDTO>>($"{Api}/expenses/month/2026/{month}/unpaid-card-charges"))!;

    private Task<HttpResponseMessage> UpdateChargeAsync(string chargeId, DateOnly date, DateOnly invoiceDate, decimal value = 10m,
        string description = "September taxi", string categoryId = MercadoId, bool countsAsTithe = true, string creditCardId = AmexId) =>
        Client.PutAsJsonAsync($"{Api}/expenses/{chargeId}", new ExpenseUpdateDTO
        {
            Date = date,
            Description = description,
            Value = value,
            CategoryId = Guid.Parse(categoryId),
            CreditCardId = Guid.Parse(creditCardId),
            InvoiceDate = invoiceDate,
            CountsAsTithe = countsAsTithe
        });

    private static async Task ShouldBeRefused(HttpResponseMessage response, HttpStatusCode status, string message)
    {
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();

        using (new AssertionScope())
        {
            response.StatusCode.Should().Be(status);
            problem!.Detail.Should().Be(message);
        }
    }

    [Fact]
    [Trait("AC", "P60-F08-statement-payment-and-card-screen-05")]
    public async Task MarkPaid_AlreadyPaidOrEmpty_IsRejectedAndSavesNothing()
    {
        var alreadyPaid = await MarkPaidAsync(SeptemberStatementId);
        var empty = await MarkPaidAsync(NovemberStatementId);

        await ShouldBeRefused(alreadyPaid, HttpStatusCode.Conflict, "This statement is already paid.");
        await ShouldBeRefused(empty, HttpStatusCode.BadRequest, "There is nothing to pay on this statement.");
        using (new AssertionScope())
        {
            (await StatementsAsync(2026, 9))!.Should().ContainSingle().Which.IsPaid.Should().BeTrue();
            (await StatementsAsync(2026, 11))!.Should().ContainSingle().Which.IsPaid.Should().BeFalse();
            var expenses = await Client.GetFromJsonAsync<List<ExpenseDTO>>($"{Api}/expenses/month/2026/9");
            expenses.Should().NotContain(expense => expense.PaymentStatus == "CreditCardSettled");
        }
    }

    [Fact]
    public async Task UnmarkPaid_UnpaidStatement_Returns409()
    {
        var response = await Client.PostAsync($"{Api}/card-statements/{OctoberStatementId}/unmark-paid", null);

        await ShouldBeRefused(response, HttpStatusCode.Conflict, "This statement is not paid.");
    }

    [Fact]
    [Trait("AC", "P60-F08-statement-payment-and-card-screen-08")]
    public async Task StatementsGet_WritesNothing_AndReturnsStoredStatementsOnly()
    {
        var repository = Services.GetRequiredService<ICashFlowRepository>();
        var storedBefore = repository.GetCardStatements().Count();

        var october = await StatementsAsync(2026, 10);
        var december = await StatementsAsync(2026, 12);

        using (new AssertionScope())
        {
            october!.Select(statement => (statement.Id.ToString(), statement.CreditCardName, statement.OutstandingTotal))
                .Should().Equal((OctoberStatementId, "BA Amex", 30m));
            december.Should().BeEmpty();
            repository.GetCardStatements().Count().Should().Be(storedBefore);
        }
    }

    [Fact]
    [Trait("AC", "P60-F08-statement-payment-and-card-screen-09")]
    public async Task ChargeOnPaidStatement_LockedFieldsReturn400_DescriptionCategoryTitheEditable()
    {
        var september = new DateOnly(2026, 9, 1);
        var october = new DateOnly(2026, 10, 1);
        var chargeDate = new DateOnly(2026, 9, 10);

        var added = await Client.PostAsJsonAsync($"{Api}/expenses", new ExpenseCreateDTO
        {
            Date = new DateOnly(2026, 9, 20),
            Description = "Late September",
            Value = 5m,
            CategoryId = Guid.Parse(MercadoId),
            CreditCardId = Guid.Parse(AmexId),
            InvoiceDate = september
        });
        var valueChanged = await UpdateChargeAsync(SeptemberChargeId, chargeDate, september, value: 11m);
        var dateChanged = await UpdateChargeAsync(SeptemberChargeId, chargeDate.AddDays(1), september);
        var cardChanged = await UpdateChargeAsync(SeptemberChargeId, chargeDate, september, creditCardId: VisaId);
        var movedOut = await UpdateChargeAsync(SeptemberChargeId, chargeDate, october);
        var movedIn = await UpdateChargeAsync(OctoberChargeId, new DateOnly(2026, 10, 3), september, value: 30m, description: "Taxi");
        var deleted = await Client.DeleteAsync($"{Api}/expenses/{SeptemberChargeId}");
        var septemberStatement = (await StatementsAsync(2026, 9))!.Single();
        var edited = await UpdateChargeAsync(SeptemberChargeId, chargeDate, september, description: "Airport taxi", categoryId: CasaId, countsAsTithe: false);

        foreach (var refused in new[] { added, valueChanged, dateChanged, cardChanged, movedOut, movedIn, deleted })
        {
            await ShouldBeRefused(refused, HttpStatusCode.BadRequest, PaidStatementMessage);
        }

        var saved = await edited.Content.ReadFromJsonAsync<ExpenseDTO>();
        using (new AssertionScope())
        {
            septemberStatement.OutstandingTotal.Should().Be(0m);
            edited.StatusCode.Should().Be(HttpStatusCode.OK);
            saved!.Description.Should().Be("Airport taxi");
            saved.CategoryName.Should().Be("Casa");
            saved.CountsAsTithe.Should().BeFalse();
            saved.Value.Should().Be(10m);
            saved.InvoiceDate.Should().Be(september);
            (await UnpaidChargesAsync(10)).Should().ContainSingle(expense => expense.Id == Guid.Parse(OctoberChargeId));
        }
    }

    [Fact]
    public async Task UnpaidCharge_MovesToAnEarlierUnpaidStatementThroughTheExpenseRoute()
    {
        var response = await UpdateChargeAsync(OctoberChargeId, new DateOnly(2026, 10, 3), new DateOnly(2026, 8, 1), value: 30m, description: "Taxi");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        (await response.Content.ReadFromJsonAsync<ExpenseDTO>())!.InvoiceDate.Should().Be(new DateOnly(2026, 8, 1));
    }
}
