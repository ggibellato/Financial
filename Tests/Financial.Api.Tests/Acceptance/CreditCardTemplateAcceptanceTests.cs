using System.Net;
using System.Net.Http.Json;
using Financial.CashFlow.Application.DTOs;
using FluentAssertions;
using FluentAssertions.Execution;
using Microsoft.AspNetCore.Mvc;

namespace Financial.Api.Tests.Acceptance;

[Trait("Category", "Integration")]
public class CreditCardTemplateAcceptanceTests : ApiEndpointTests
{
    private const string Api = "/api/v1/financial";
    private const string BarclaysId = "8f3b1c1a-2e3a-4b1a-9a7f-100000000001";
    private const string MercadoId = "8f3b1c1a-2e3a-4b1a-9a7f-200000000001";
    private const string AmexId = "8f3b1c1a-2e3a-4b1a-9a7f-500000000001";
    private const string VisaId = "8f3b1c1a-2e3a-4b1a-9a7f-500000000002";
    private const string UnusedId = "8f3b1c1a-2e3a-4b1a-9a7f-500000000003";
    private const string LinkedId = "8f3b1c1a-2e3a-4b1a-9a7f-500000000004";
    private const string AmexOctoberStatementId = "8f3b1c1a-2e3a-4b1a-9a7f-700000000001";
    private const string AmexNovemberStatementId = "8f3b1c1a-2e3a-4b1a-9a7f-700000000002";
    private const string CardPayerMessage = "BA Amex is a card account and can't pay.";

    private const string SeededData = $$"""
        {
          "Accounts": [
            { "Id": "{{BarclaysId}}", "Name": "Barclays", "Template": "Bank", "Currency": "GBP", "IsActive": true, "OpeningBalance": { "Amount": 1000, "StartDate": "2026-01-01" }, "RoundUp": { "Enabled": false } },
            { "Id": "{{AmexId}}", "Name": "BA Amex", "Template": "CreditCard", "Currency": "GBP", "IsActive": true, "OpeningBalance": null, "RoundUp": null },
            { "Id": "{{VisaId}}", "Name": "Platinum Visa 8003", "Template": "CreditCard", "Currency": "GBP", "IsActive": true, "OpeningBalance": null, "RoundUp": null },
            { "Id": "{{UnusedId}}", "Name": "Unused", "Template": "CreditCard", "Currency": "GBP", "IsActive": true, "OpeningBalance": null, "RoundUp": null },
            { "Id": "{{LinkedId}}", "Name": "Linked", "Template": "CreditCard", "Currency": "GBP", "IsActive": true, "OpeningBalance": null, "RoundUp": null }
          ],
          "Categories": [
            { "Id": "{{MercadoId}}", "Name": "Mercado", "Active": true, "IsInvestment": false, "IsTithe": false }
          ],
          "Expenses": [
            { "Id": "8f3b1c1a-2e3a-4b1a-9a7f-600000000001", "Date": "2026-09-10", "Description": "September taxi", "Value": 10, "CategoryId": "{{MercadoId}}", "PaymentSourceAccountId": null, "CreditCardId": "{{AmexId}}", "ChargeDate": "2026-09-10", "InvoiceDate": "2026-09-01", "RoundUpAmount": null, "CountsAsTithe": true },
            { "Id": "8f3b1c1a-2e3a-4b1a-9a7f-600000000002", "Date": "2026-10-03", "Description": "Taxi", "Value": 30, "CategoryId": "{{MercadoId}}", "PaymentSourceAccountId": null, "CreditCardId": "{{AmexId}}", "ChargeDate": "2026-10-03", "InvoiceDate": "2026-10-01", "RoundUpAmount": null, "CountsAsTithe": true },
            { "Id": "8f3b1c1a-2e3a-4b1a-9a7f-600000000003", "Date": "2026-10-25", "Description": "Settled", "Value": 100, "CategoryId": "{{MercadoId}}", "PaymentSourceAccountId": "{{BarclaysId}}", "CreditCardId": "{{AmexId}}", "ChargeDate": "2026-10-05", "InvoiceDate": "2026-10-01", "RoundUpAmount": null, "CountsAsTithe": true },
            { "Id": "8f3b1c1a-2e3a-4b1a-9a7f-600000000004", "Date": "2026-10-07", "Description": "Shop", "Value": 40, "CategoryId": "{{MercadoId}}", "PaymentSourceAccountId": null, "CreditCardId": "{{VisaId}}", "ChargeDate": "2026-10-07", "InvoiceDate": "2026-10-01", "RoundUpAmount": null, "CountsAsTithe": true }
          ],
          "CardStatements": [
            { "Id": "{{AmexOctoberStatementId}}", "CreditCardId": "{{AmexId}}", "Year": 2026, "Month": 10, "IsPaid": false, "DueDate": null },
            { "Id": "{{AmexNovemberStatementId}}", "CreditCardId": "{{AmexId}}", "Year": 2026, "Month": 11, "IsPaid": false, "DueDate": "2026-11-18" }
          ],
          "InvestmentAccounts": [
            { "Id": "8f3b1c1a-2e3a-4b1a-9a7f-800000000001", "Name": "Linked balance", "IsActive": true, "IsLiability": true, "Source": "CreditCard", "CreditCardId": "{{LinkedId}}" }
          ],
          "Version": 3
        }
        """;

    public CreditCardTemplateAcceptanceTests()
        : base(timeProvider: TestClock.At(new DateTimeOffset(2026, 10, 15, 12, 0, 0, TimeSpan.FromHours(1))))
    {
        ReplaceCashFlowDataBeforeStart(SeededData);
    }

    private async Task<List<ExpenseDTO>> OctoberExpensesAsync() =>
        (await Client.GetFromJsonAsync<List<ExpenseDTO>>($"{Api}/expenses/month/2026/10"))!;

    private static async Task ShouldBeRefusedAsCardPayer(HttpResponseMessage response)
    {
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();

        using (new AssertionScope())
        {
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            problem!.Detail.Should().Be(CardPayerMessage);
        }
    }

    private static async Task ShouldBeInUse(HttpResponseMessage response, string message)
    {
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();

        using (new AssertionScope())
        {
            response.StatusCode.Should().Be(HttpStatusCode.Conflict);
            problem!.Detail.Should().Be(message);
        }
    }

    [Fact]
    [Trait("AC", "P60-F07-credit-card-template-and-card-migration-04")]
    public async Task CardAccount_HasNoActions_AndIsRefusedAsPayerWith400()
    {
        var detail = await Client.GetFromJsonAsync<AccountDetailDTO>($"{Api}/accounts/{AmexId}/2026/10");
        var expensesBefore = await OctoberExpensesAsync();
        var billResponse = await Client.PostAsJsonAsync($"{Api}/mensais", new RecurringBillCreateDTO
        {
            DueDay = 15,
            Description = "Council Tax",
            Value = 120m,
            Area = "UK"
        });
        var bill = (await billResponse.Content.ReadFromJsonAsync<RecurringBillDTO>())!;

        var expense = await Client.PostAsJsonAsync($"{Api}/expenses", new ExpenseCreateDTO
        {
            Date = new DateOnly(2026, 10, 10),
            Description = "Shop",
            Value = 20m,
            CategoryId = Guid.Parse(MercadoId),
            PaymentSourceBankId = Guid.Parse(AmexId)
        });
        var billPaid = await Client.PostAsJsonAsync($"{Api}/mensais/{bill.Id}/paid", new RecurringBillMarkPaidDTO
        {
            Expense = new RecurringBillPaymentExpenseDTO
            {
                Date = new DateOnly(2026, 10, 10),
                Description = "Council Tax",
                Value = 120m,
                CategoryId = Guid.Parse(MercadoId),
                PaymentSourceBankId = Guid.Parse(AmexId)
            }
        });
        var statementPaid = await Client.PostAsJsonAsync(
            $"{Api}/card-statements/{AmexOctoberStatementId}/mark-paid",
            new MarkCardStatementPaidDTO { PaymentSourceBankId = Guid.Parse(AmexId) });

        using (new AssertionScope())
        {
            detail!.AllowedActions.Should().BeEmpty();
            detail.Balance.Should().Be(-40m);
            await ShouldBeRefusedAsCardPayer(expense);
            await ShouldBeRefusedAsCardPayer(billPaid);
            await ShouldBeRefusedAsCardPayer(statementPaid);
            (await OctoberExpensesAsync()).Should().HaveCount(expensesBefore.Count);
            var bills = await Client.GetFromJsonAsync<List<RecurringBillDTO>>($"{Api}/mensais");
            bills.Should().ContainSingle(b => b.Id == bill.Id).Which.Status.Should().NotBe("Paid");
            var statements = await Client.GetFromJsonAsync<List<CardStatementDTO>>($"{Api}/card-statements/2026/10");
            statements.Should().ContainSingle(s => s.Id == Guid.Parse(AmexOctoberStatementId)).Which.IsPaid.Should().BeFalse();
        }
    }

    [Fact]
    [Trait("AC", "P60-F07-credit-card-template-and-card-migration-05")]
    public async Task UsedCard_DeleteReturns409_OnBothRoutes()
    {
        var chargedThroughAccounts = await Client.DeleteAsync($"{Api}/accounts/{AmexId}");
        var linkedThroughAccounts = await Client.DeleteAsync($"{Api}/accounts/{LinkedId}");
        var linkedThroughCreditCards = await Client.DeleteAsync($"{Api}/credit-cards/{LinkedId}");
        var chargedThroughCreditCards = await Client.DeleteAsync($"{Api}/credit-cards/{VisaId}");
        var unused = await Client.DeleteAsync($"{Api}/accounts/{UnusedId}");

        using (new AssertionScope())
        {
            await ShouldBeInUse(chargedThroughAccounts, "BA Amex has transactions and cannot be deleted. Deactivate it instead.");
            await ShouldBeInUse(linkedThroughAccounts, "Linked has transactions and cannot be deleted. Deactivate it instead.");
            await ShouldBeInUse(linkedThroughCreditCards, "Cannot delete a credit card that is still referenced by a statement or expense.");
            await ShouldBeInUse(chargedThroughCreditCards, "Cannot delete a credit card that is still referenced by a statement or expense.");
            unused.StatusCode.Should().Be(HttpStatusCode.NoContent);
            var cards = await Client.GetFromJsonAsync<List<CreditCardDTO>>($"{Api}/credit-cards");
            cards!.Select(card => card.Name).Should().Equal("BA Amex", "Platinum Visa 8003", "Linked");
        }
    }

    [Fact]
    [Trait("AC", "P60-F07-credit-card-template-and-card-migration-11")]
    public async Task CreateCardThroughAccounts_ListedOnCreditCards()
    {
        var response = await Client.PostAsJsonAsync($"{Api}/accounts", new AccountCreateDTO
        {
            TemplateName = "CreditCard",
            Name = "Nubank",
            Currency = "GBP"
        });
        var created = await response.Content.ReadFromJsonAsync<AccountDTO>();

        var cards = await Client.GetFromJsonAsync<List<CreditCardDTO>>($"{Api}/credit-cards");

        using (new AssertionScope())
        {
            response.StatusCode.Should().Be(HttpStatusCode.Created);
            created!.TemplateName.Should().Be("CreditCard");
            var nubank = cards.Should().ContainSingle(card => card.Name == "Nubank").Which;
            nubank.Id.Should().Be(created.Id);
            nubank.IsActive.Should().BeTrue();
            nubank.HasReferences.Should().BeFalse();
        }
    }

    [Fact]
    [Trait("AC", "P60-F07-credit-card-template-and-card-migration-12")]
    public async Task CreditCardsAndCardStatements_ReturnTheSameValues()
    {
        var cards = await Client.GetFromJsonAsync<List<CreditCardDTO>>($"{Api}/credit-cards");
        var statements = await Client.GetFromJsonAsync<List<CardStatementDTO>>($"{Api}/card-statements/2026/10");

        using (new AssertionScope())
        {
            cards!.Select(card => (card.Id.ToString(), card.Name, card.IsActive, card.NextInvoiceDueDate, card.LatestInvoiceDate, card.HasReferences))
                .Should().Equal(
                    (AmexId, "BA Amex", true, new DateOnly(2026, 11, 18), new DateOnly(2026, 10, 1), true),
                    (VisaId, "Platinum Visa 8003", true, (DateOnly?)null, new DateOnly(2026, 10, 1), true),
                    (UnusedId, "Unused", true, (DateOnly?)null, (DateOnly?)null, false),
                    (LinkedId, "Linked", true, (DateOnly?)null, (DateOnly?)null, true));
            statements!.Select(s => (s.CreditCardName, s.Year, s.Month, s.IsPaid, s.OutstandingTotal, s.AccumulatedOutstandingTotal))
                .Should().BeEquivalentTo(new[]
                {
                    ("BA Amex", 2026, 10, false, 30m, 40m),
                    ("Platinum Visa 8003", 2026, 10, false, 40m, 40m),
                    ("Unused", 2026, 10, false, 0m, 0m),
                    ("Linked", 2026, 10, false, 0m, 0m)
                });
            statements.Single(s => s.CreditCardName == "BA Amex").Id.Should().Be(Guid.Parse(AmexOctoberStatementId));
        }
    }
}
