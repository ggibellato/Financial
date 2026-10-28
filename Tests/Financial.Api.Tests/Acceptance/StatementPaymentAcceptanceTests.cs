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
    private const string DizimoId = "8f3b1c1a-2e3a-4b1a-9a7f-200000000003";
    private const string AmexId = "8f3b1c1a-2e3a-4b1a-9a7f-500000000001";
    private const string VisaId = "8f3b1c1a-2e3a-4b1a-9a7f-500000000002";
    private const string SeptemberChargeId = "8f3b1c1a-2e3a-4b1a-9a7f-600000000001";
    private const string OctoberChargeId = "8f3b1c1a-2e3a-4b1a-9a7f-600000000002";
    private const string SettledSeptemberChargeId = "8f3b1c1a-2e3a-4b1a-9a7f-600000000003";
    private const string SeptemberStatementId = "8f3b1c1a-2e3a-4b1a-9a7f-700000000001";
    private const string OctoberStatementId = "8f3b1c1a-2e3a-4b1a-9a7f-700000000002";
    private const string NovemberStatementId = "8f3b1c1a-2e3a-4b1a-9a7f-700000000003";
    private const string PaidStatementMessage = "This charge's statement is paid; unmark it first.";
    private const string PaymentTransferMessage = "This transfer pays a card statement; unmark the statement instead.";

    private const string SeededData = $$"""
        {
          "Accounts": [
            { "Id": "{{BarclaysId}}", "Name": "Barclays", "Template": "Bank", "Currency": "GBP", "IsActive": true, "OpeningBalance": { "Amount": 1000, "StartDate": "2026-01-01" }, "RoundUp": { "Enabled": false } },
            { "Id": "{{AmexId}}", "Name": "BA Amex", "Template": "CreditCard", "Currency": "GBP", "IsActive": true, "OpeningBalance": null, "RoundUp": null },
            { "Id": "{{VisaId}}", "Name": "Platinum Visa 8003", "Template": "CreditCard", "Currency": "GBP", "IsActive": true, "OpeningBalance": null, "RoundUp": null }
          ],
          "Categories": [
            { "Id": "{{MercadoId}}", "Name": "Mercado", "Active": true, "IsInvestment": false, "IsTithe": false },
            { "Id": "{{CasaId}}", "Name": "Casa", "Active": true, "IsInvestment": false, "IsTithe": false },
            { "Id": "{{DizimoId}}", "Name": "Dízimo", "Active": true, "IsInvestment": false, "IsTithe": true }
          ],
          "Expenses": [
            { "Id": "{{SeptemberChargeId}}", "Date": "2026-09-10", "Description": "September taxi", "Value": 10, "CategoryId": "{{MercadoId}}", "PaymentSourceAccountId": null, "CreditCardId": "{{AmexId}}", "ChargeDate": "2026-09-10", "InvoiceDate": "2026-09-01", "RoundUpAmount": null, "CountsAsTithe": true },
            { "Id": "{{OctoberChargeId}}", "Date": "2026-10-03", "Description": "Taxi", "Value": 30, "CategoryId": "{{MercadoId}}", "PaymentSourceAccountId": null, "CreditCardId": "{{AmexId}}", "ChargeDate": "2026-10-03", "InvoiceDate": "2026-10-01", "RoundUpAmount": null, "CountsAsTithe": true },
            { "Id": "{{SettledSeptemberChargeId}}", "Date": "2026-10-02", "Description": "Settled lunch", "Value": 7, "CategoryId": "{{MercadoId}}", "PaymentSourceAccountId": "{{BarclaysId}}", "CreditCardId": "{{AmexId}}", "ChargeDate": "2026-09-15", "InvoiceDate": "2026-09-01", "RoundUpAmount": null, "CountsAsTithe": true }
          ],
          "CardStatements": [
            { "Id": "{{SeptemberStatementId}}", "CreditCardId": "{{AmexId}}", "Year": 2026, "Month": 9, "IsPaid": true, "DueDate": "2026-10-03" },
            { "Id": "{{OctoberStatementId}}", "CreditCardId": "{{AmexId}}", "Year": 2026, "Month": 10, "IsPaid": false, "DueDate": null },
            { "Id": "{{NovemberStatementId}}", "CreditCardId": "{{AmexId}}", "Year": 2026, "Month": 11, "IsPaid": false, "DueDate": "2026-12-03" }
          ],
          "Version": 4
        }
        """;

    public StatementPaymentAcceptanceTests()
        : base(timeProvider: TestClock.At(new DateTimeOffset(2026, 10, 15, 12, 0, 0, TimeSpan.FromHours(1))))
    {
        ReplaceCashFlowDataBeforeStart(SeededData);
    }

    private Task<List<CardStatementDTO>?> StatementsAsync(int year, int month) =>
        Client.GetFromJsonAsync<List<CardStatementDTO>>($"{Api}/card-statements/{year}/{month}");

    private Task<HttpResponseMessage> MarkPaidAsync(string statementId, DateOnly? date = null) =>
        Client.PostAsJsonAsync(
            $"{Api}/card-statements/{statementId}/mark-paid", new MarkCardStatementPaidDTO { PaymentSourceBankId = Guid.Parse(BarclaysId), Date = date });

    private async Task<AccountDetailDTO> AccountAsync(string accountId) =>
        (await Client.GetFromJsonAsync<AccountDetailDTO>($"{Api}/accounts/{accountId}/2026/10"))!;

    private async Task<List<AccountTransactionDTO>> TransactionsAsync(string accountId) =>
        (await Client.GetFromJsonAsync<List<AccountTransactionDTO>>($"{Api}/accounts/{accountId}/transactions/2026/10"))!;

    private async Task<List<PayableStatementDTO>> PayableStatementsAsync() =>
        (await Client.GetFromJsonAsync<List<PayableStatementDTO>>($"{Api}/accounts/payable-statements"))!;

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
            Services.GetRequiredService<ICashFlowRepository>().GetTransfers().Should().BeEmpty();
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

    [Fact]
    [Trait("AC", "P60-F08-statement-payment-and-card-screen-01")]
    public async Task MarkPaid_WritesOneTransferForTheTotal_AndLeavesChargesUntouched()
    {
        var repository = Services.GetRequiredService<ICashFlowRepository>();

        var response = await MarkPaidAsync(OctoberStatementId, new DateOnly(2026, 10, 12));

        var statement = await response.Content.ReadFromJsonAsync<CardStatementDTO>();
        var charge = (await Client.GetFromJsonAsync<List<ExpenseDTO>>($"{Api}/expenses/month/2026/10"))!
            .Single(expense => expense.Id == Guid.Parse(OctoberChargeId));
        using (new AssertionScope())
        {
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            statement!.IsPaid.Should().BeTrue();
            statement.OutstandingTotal.Should().Be(0m);
            var payment = repository.GetTransfers().Should().ContainSingle().Subject;
            payment.SourceAccount.Id.Should().Be(Guid.Parse(BarclaysId));
            payment.DestinationAccount.Id.Should().Be(Guid.Parse(AmexId));
            payment.Amount.Should().Be(30m);
            payment.Date.Should().Be(new DateOnly(2026, 10, 12));
            payment.Note.Should().BeNull();
            repository.GetCardStatements().Single(s => s.Id == Guid.Parse(OctoberStatementId)).PaymentTransferId.Should().Be(payment.Id);
            charge.Date.Should().Be(new DateOnly(2026, 10, 3));
            charge.CategoryName.Should().Be("Mercado");
            charge.InvoiceDate.Should().Be(new DateOnly(2026, 10, 1));
            charge.PaymentSourceBankId.Should().BeNull();
            charge.PaymentStatus.Should().Be("CreditCardStatementPaid");
        }
    }

    [Fact]
    [Trait("AC", "P60-F08-statement-payment-and-card-screen-02")]
    public async Task MarkPaid_CardOwedDrops_AndBankListsOneCardPaymentRowWithItsStatement()
    {
        var cardBefore = await AccountAsync(AmexId);
        var bankBefore = await AccountAsync(BarclaysId);

        await MarkPaidAsync(OctoberStatementId);
        var bankRows = await TransactionsAsync(BarclaysId);
        var cardPayment = (await TransactionsAsync(AmexId)).Single(row => row.Kind == "CardPayment");

        using (new AssertionScope())
        {
            cardBefore.Balance.Should().Be(-40m);
            bankBefore.Balance.Should().Be(993m);
            (await AccountAsync(AmexId)).Balance.Should().Be(-10m);
            (await AccountAsync(BarclaysId)).Balance.Should().Be(963m);
            bankRows.Where(row => row.Kind is "CardPayment" or "TransferOut").Should().ContainSingle()
                .Which.Should().BeEquivalentTo(new
                {
                    Kind = "CardPayment",
                    Date = new DateOnly(2026, 10, 15),
                    Description = "BA Amex payment – Oct 2026 statement",
                    Counterpart = "BA Amex",
                    Amount = -30m,
                    CanEdit = false,
                    Statement = new { AccountId = Guid.Parse(AmexId), Year = 2026, Month = 10 }
                });
            cardPayment.Description.Should().Be("BA Amex payment – Oct 2026 statement");
            cardPayment.Statement.Should().BeEquivalentTo(new { AccountId = Guid.Parse(AmexId), Year = 2026, Month = 10 });
        }
    }

    [Fact]
    [Trait("AC", "P60-F08-statement-payment-and-card-screen-04")]
    public async Task PayFromBank_ProducesTheSameRecordsAsFromTheCard()
    {
        var bank = await AccountAsync(BarclaysId);
        var payable = await PayableStatementsAsync();

        var statement = payable.Should().ContainSingle().Subject;
        var response = await MarkPaidAsync(statement.Id.ToString(), new DateOnly(2026, 10, 14));
        var repository = Services.GetRequiredService<ICashFlowRepository>();
        var transfer = repository.GetTransfers().Should().ContainSingle().Subject;

        using (new AssertionScope())
        {
            bank.AllowedActions.Should().Contain("PayCardStatement");
            statement.Should().BeEquivalentTo(new
            {
                Id = Guid.Parse(OctoberStatementId),
                CreditCardId = Guid.Parse(AmexId),
                CreditCardName = "BA Amex",
                Year = 2026,
                Month = 10,
                Total = 30m,
                Currency = "GBP"
            });
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            transfer.Should().BeEquivalentTo(
                new { Date = new DateOnly(2026, 10, 14), Amount = 30m, Note = (string?)null, DestinationAmount = (decimal?)null },
                options => options.ExcludingMissingMembers());
            transfer.SourceAccount.Id.Should().Be(Guid.Parse(BarclaysId));
            transfer.DestinationAccount.Id.Should().Be(Guid.Parse(AmexId));
            repository.GetCardStatements().Single(stored => stored.Id == statement.Id).PaymentTransferId.Should().Be(transfer.Id);
            repository.GetExpenses().Single(charge => charge.Id == Guid.Parse(OctoberChargeId)).Date.Should().Be(new DateOnly(2026, 10, 3));
            (await PayableStatementsAsync()).Should().BeEmpty();
        }
    }

    [Fact]
    [Trait("AC", "P60-F08-statement-payment-and-card-screen-03")]
    public async Task CategoryTotalsAndTithe_CountChargesInStatementMonth_PaidOrNot()
    {
        await Client.PostAsJsonAsync($"{Api}/expenses", new ExpenseCreateDTO
        {
            Date = new DateOnly(2026, 9, 28),
            Description = "Offering",
            Value = 50m,
            CategoryId = Guid.Parse(DizimoId),
            CreditCardId = Guid.Parse(AmexId),
            InvoiceDate = new DateOnly(2026, 10, 1)
        });
        var unpaid = await StatementMonthFiguresAsync();

        await MarkPaidAsync(OctoberStatementId);
        var paid = await StatementMonthFiguresAsync();

        using (new AssertionScope())
        {
            foreach (var (october, september, tithe) in new[] { unpaid, paid })
            {
                october.Should().BeEquivalentTo(new Dictionary<string, decimal> { ["Mercado"] = 30m, ["Dízimo"] = 50m });
                september.Should().NotContainKey("Dízimo");
                tithe.Should().Be(-50m);
            }
        }
    }

    [Fact]
    [Trait("AC", "P60-F08-statement-payment-and-card-screen-06")]
    public async Task Unmark_TransferPaid_DeletesTransfer()
    {
        var repository = Services.GetRequiredService<ICashFlowRepository>();
        await MarkPaidAsync(OctoberStatementId);

        var response = await Client.PostAsync($"{Api}/card-statements/{OctoberStatementId}/unmark-paid", null);

        var statement = await response.Content.ReadFromJsonAsync<CardStatementDTO>();
        using (new AssertionScope())
        {
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            statement!.IsPaid.Should().BeFalse();
            statement.OutstandingTotal.Should().Be(30m);
            repository.GetTransfers().Should().BeEmpty();
            repository.GetCardStatements().Single(s => s.Id == Guid.Parse(OctoberStatementId)).PaymentTransferId.Should().BeNull();
            (await AccountAsync(BarclaysId)).Balance.Should().Be(993m);
        }
    }

    [Fact]
    [Trait("AC", "P60-F08-statement-payment-and-card-screen-06")]
    public async Task Unmark_LegacyPaid_RestoresTodaysReversal()
    {
        var response = await Client.PostAsync($"{Api}/card-statements/{SeptemberStatementId}/unmark-paid", null);

        var statement = await response.Content.ReadFromJsonAsync<CardStatementDTO>();
        var unpaid = await UnpaidChargesAsync(9);
        using (new AssertionScope())
        {
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            statement!.IsPaid.Should().BeFalse();
            statement.OutstandingTotal.Should().Be(17m);
            unpaid.Select(charge => (charge.Id.ToString(), charge.Date, charge.PaymentSourceBankId)).Should().BeEquivalentTo(new[]
            {
                (SeptemberChargeId, new DateOnly(2026, 9, 10), (Guid?)null),
                (SettledSeptemberChargeId, new DateOnly(2026, 9, 15), (Guid?)null)
            });
        }
    }

    [Fact]
    [Trait("AC", "P60-F08-statement-payment-and-card-screen-10")]
    public async Task PaymentTransfer_HiddenFromTransfers_AndEditOrDeleteReturns409()
    {
        var repository = Services.GetRequiredService<ICashFlowRepository>();
        await MarkPaidAsync(OctoberStatementId);
        var paymentId = repository.GetTransfers().Single().Id;

        var monthList = await Client.GetFromJsonAsync<List<TransferDTO>>($"{Api}/transfers/month/2026/10");
        var bankList = await Client.GetFromJsonAsync<List<TransferDTO>>($"{Api}/transfers/bank/{BarclaysId}");
        var refusals = new[]
        {
            await Client.PutAsJsonAsync($"{Api}/transfers/{paymentId}", new TransferUpdateDTO
            {
                Date = new DateOnly(2026, 10, 15), SourceBankId = Guid.Parse(BarclaysId), DestinationBankId = Guid.Parse(AmexId), Amount = 1m
            }),
            await Client.DeleteAsync($"{Api}/transfers/{paymentId}"),
            await Client.PutAsJsonAsync($"{Api}/accounts/transfers/{paymentId}", new AccountTransferUpdateDTO
            {
                Date = new DateOnly(2026, 10, 15), SourceAccountId = Guid.Parse(BarclaysId), DestinationAccountId = Guid.Parse(AmexId), Amount = 1m
            }),
            await Client.DeleteAsync($"{Api}/accounts/transfers/{paymentId}")
        };

        foreach (var refused in refusals)
        {
            await ShouldBeRefused(refused, HttpStatusCode.Conflict, PaymentTransferMessage);
        }

        using (new AssertionScope())
        {
            monthList.Should().BeEmpty();
            bankList.Should().BeEmpty();
            repository.GetTransfers().Should().ContainSingle().Which.Amount.Should().Be(30m);
        }
    }

    private async Task<(Dictionary<string, decimal> October, Dictionary<string, decimal> September, decimal OctoberTithe)> StatementMonthFiguresAsync()
    {
        var october = await Client.GetFromJsonAsync<List<CategoryTotalDTO>>($"{Api}/expenses/month/2026/10/category-totals");
        var september = await Client.GetFromJsonAsync<List<CategoryTotalDTO>>($"{Api}/expenses/month/2026/9/category-totals");
        var tithe = await Client.GetFromJsonAsync<TitheSummaryDTO>($"{Api}/tithe/month/2026/10");
        return (
            october!.ToDictionary(total => total.Category, total => total.TotalValue),
            september!.ToDictionary(total => total.Category, total => total.TotalValue),
            tithe!.TitheBalance);
    }

    [Fact]
    [Trait("AC", "P60-F08-statement-payment-and-card-screen-11")]
    public async Task AccountStatements_CarryDueDates()
    {
        await MarkPaidAsync(OctoberStatementId, new DateOnly(2026, 10, 12));

        var statements = (await Client.GetFromJsonAsync<List<AccountStatementDTO>>($"{Api}/accounts/{AmexId}/statements"))!;
        var bankResponse = await Client.GetAsync($"{Api}/accounts/{BarclaysId}/statements");

        using (new AssertionScope())
        {
            statements.Select(statement => statement.Id).Should().Equal(Guid.Parse(OctoberStatementId), Guid.Parse(SeptemberStatementId));
            statements[0].Should().BeEquivalentTo(new
            {
                Year = 2026, Month = 10, Total = 30m, IsPaid = true, DueDate = (DateOnly?)null,
                PaidOn = new DateOnly(2026, 10, 12), PaidFromAccountName = "Barclays"
            });
            statements[1].Should().BeEquivalentTo(new
            {
                Year = 2026, Month = 9, Total = 17m, IsPaid = true, DueDate = new DateOnly(2026, 10, 3),
                PaidOn = new DateOnly(2026, 10, 2), PaidFromAccountName = "Barclays"
            });
            await ShouldBeRefused(bankResponse, HttpStatusCode.BadRequest, "Barclays is not a card account.");
        }
    }

    [Fact]
    [Trait("AC", "P60-F08-statement-payment-and-card-screen-12")]
    public async Task CardTransactions_ListEveryChargeOfTheStatementMonth_WithPurchaseDate()
    {
        await MarkPaidAsync(OctoberStatementId, new DateOnly(2026, 10, 12));

        var september = await Client.GetFromJsonAsync<List<AccountTransactionDTO>>($"{Api}/accounts/{AmexId}/transactions/2026/9");
        var october = await Client.GetFromJsonAsync<List<AccountTransactionDTO>>($"{Api}/accounts/{AmexId}/transactions/2026/10");

        using (new AssertionScope())
        {
            september!.Select(row => (row.Id, row.Kind, row.Date, row.Counterpart, row.Amount, row.CanEdit)).Should().Equal(
                (Guid.Parse(SettledSeptemberChargeId), "CardCharge", new DateOnly(2026, 9, 15), "Barclays", -7m, false),
                (Guid.Parse(SeptemberChargeId), "CardCharge", new DateOnly(2026, 9, 10), (string?)null, -10m, false));
            october!.Select(row => (row.Kind, row.Date, row.Counterpart, row.Amount, row.CanEdit)).Should().Equal(
                ("CardPayment", new DateOnly(2026, 10, 12), "Barclays", 30m, false),
                ("CardCharge", new DateOnly(2026, 10, 3), (string?)null, -30m, false));
        }
    }

    private Task<HttpResponseMessage> MoveToStatementAsync(Guid chargeId, int year, int month) =>
        Client.PostAsJsonAsync($"{Api}/expenses/{chargeId}/move-to-statement", new MoveChargeToStatementDTO { Year = year, Month = month });

    [Fact]
    [Trait("AC", "P60-F08-statement-payment-and-card-screen-07")]
    public async Task MoveToStatement_LaterUnpaid_Works_PaidOrEarlier_Returns400()
    {
        const string laterUnpaidMessage = "Charges can only move to a later, unpaid statement.";
        var added = await Client.PostAsJsonAsync($"{Api}/expenses", new ExpenseCreateDTO
        {
            Date = new DateOnly(2026, 10, 5),
            Description = "Bakery",
            Value = 5m,
            CategoryId = Guid.Parse(MercadoId),
            CreditCardId = Guid.Parse(AmexId)
        });
        var chargeId = (await added.Content.ReadFromJsonAsync<ExpenseDTO>())!.Id;
        (await MoveToStatementAsync(Guid.Parse(OctoberChargeId), 2026, 11)).StatusCode.Should().Be(HttpStatusCode.OK);
        (await MarkPaidAsync(NovemberStatementId)).StatusCode.Should().Be(HttpStatusCode.OK);

        var intoPaid = await MoveToStatementAsync(chargeId, 2026, 11);
        var intoEarlier = await MoveToStatementAsync(chargeId, 2026, 9);
        var intoLater = await MoveToStatementAsync(chargeId, 2027, 1);

        await ShouldBeRefused(intoPaid, HttpStatusCode.BadRequest, laterUnpaidMessage);
        await ShouldBeRefused(intoEarlier, HttpStatusCode.BadRequest, laterUnpaidMessage);
        using (new AssertionScope())
        {
            intoLater.StatusCode.Should().Be(HttpStatusCode.OK);
            (await intoLater.Content.ReadFromJsonAsync<ExpenseDTO>())!.InvoiceDate.Should().Be(new DateOnly(2027, 1, 1));
            (await StatementsAsync(2027, 1))!.Should().ContainSingle(statement => statement.CreditCardId == Guid.Parse(AmexId));
        }
    }
}
