using System.Net;
using System.Net.Http.Json;
using Financial.CashFlow.Application.DTOs;
using FluentAssertions;
using FluentAssertions.Execution;

namespace Financial.Api.Tests.Acceptance;

[Trait("Category", "Integration")]
public class MonthTransactionsViewAcceptanceTests : ApiEndpointTests
{
    private const string Api = "/api/v1/financial";
    private const string BarclaysId = "f1600000-0000-0000-0000-000000000001";
    private const string ChaseId = "f1600000-0000-0000-0000-000000000002";
    private const string DormantId = "f1600000-0000-0000-0000-000000000003";
    private const string AmexId = "f1600000-0000-0000-0000-000000000004";
    private const string ReservaId = "f1600000-0000-0000-0000-000000000005";
    private const string InvestimentoId = "f1600000-0000-0000-0000-000000000101";
    private const string MercadoId = "f1600000-0000-0000-0000-000000000201";
    private const string TransporteId = "f1600000-0000-0000-0000-000000000202";
    private const string ReservaCategoryId = "f1600000-0000-0000-0000-000000000203";
    private const string SalaryId = "f1600000-0000-0000-0000-000000000301";
    private const string SettledChargeId = "f1600000-0000-0000-0000-000000000404";
    private const string DepositExpenseId = "f1600000-0000-0000-0000-000000000406";
    private const string DepositMovementId = "f1600000-0000-0000-0000-000000000601";
    private const string OperationId = "f1600000-0000-0000-0000-000000000701";
    private const string PaymentId = "f1600000-0000-0000-0000-000000000502";

    private const string SeededData = $$"""
        {
          "Accounts": [
            { "Id": "{{BarclaysId}}", "Name": "Barclays", "Template": "Bank", "Currency": "GBP", "IsActive": true, "OpeningBalance": { "Amount": 1000, "StartDate": "2026-01-01" }, "RoundUp": { "Enabled": true }, "Pots": null },
            { "Id": "{{ChaseId}}", "Name": "Chase", "Template": "Bank", "Currency": "GBP", "IsActive": true, "OpeningBalance": { "Amount": 200, "StartDate": "2026-01-01" }, "RoundUp": { "Enabled": false }, "Pots": null },
            { "Id": "{{DormantId}}", "Name": "Dormant", "Template": "Bank", "Currency": "GBP", "IsActive": false, "OpeningBalance": { "Amount": 0, "StartDate": "2026-01-01" }, "RoundUp": { "Enabled": false }, "Pots": null },
            { "Id": "{{AmexId}}", "Name": "Amex", "Template": "CreditCard", "Currency": "GBP", "IsActive": true, "OpeningBalance": null, "RoundUp": null, "Pots": null },
            { "Id": "{{ReservaId}}", "Name": "Reserva", "Template": "Pots", "Currency": "GBP", "IsActive": true, "OpeningBalance": null, "RoundUp": null, "Pots": [
              { "Id": "{{InvestimentoId}}", "Name": "Investimento", "IsActive": true, "SplitPercentage": 100 }
            ], "CategoryId": "{{ReservaCategoryId}}" }
          ],
          "Categories": [
            { "Id": "{{MercadoId}}", "Name": "Mercado", "Active": true, "IsInvestment": false, "IsTithe": false },
            { "Id": "{{TransporteId}}", "Name": "Transporte", "Active": true, "IsInvestment": false, "IsTithe": false },
            { "Id": "{{ReservaCategoryId}}", "Name": "Reserva", "Active": true, "IsInvestment": false, "IsTithe": false }
          ],
          "IncomeSources": [
            { "Id": "{{SalaryId}}", "Name": "Salary", "IsActive": true, "Group": "Salary" }
          ],
          "Expenses": [
            { "Id": "f1600000-0000-0000-0000-000000000401", "Date": "2026-10-10", "Description": "Tesco", "Value": 42.10, "CategoryId": "{{MercadoId}}", "PaymentSourceAccountId": "{{BarclaysId}}", "CreditCardId": null, "ChargeDate": null, "InvoiceDate": null, "RoundUpAmount": 0.90, "CountsAsTithe": true },
            { "Id": "f1600000-0000-0000-0000-000000000402", "Date": "2026-09-25", "Description": "Taxi", "Value": 30, "CategoryId": "{{TransporteId}}", "PaymentSourceAccountId": null, "CreditCardId": "{{AmexId}}", "ChargeDate": "2026-09-25", "InvoiceDate": "2026-10-01", "RoundUpAmount": null, "CountsAsTithe": true },
            { "Id": "f1600000-0000-0000-0000-000000000403", "Date": "2026-10-08", "Description": "Shoes returned", "Value": -12, "CategoryId": "{{MercadoId}}", "PaymentSourceAccountId": null, "CreditCardId": "{{AmexId}}", "ChargeDate": "2026-10-08", "InvoiceDate": "2026-10-01", "RoundUpAmount": null, "CountsAsTithe": true },
            { "Id": "{{SettledChargeId}}", "Date": "2026-10-02", "Description": "Settled lunch", "Value": 7, "CategoryId": "{{MercadoId}}", "PaymentSourceAccountId": "{{BarclaysId}}", "CreditCardId": "{{AmexId}}", "ChargeDate": "2026-09-15", "InvoiceDate": "2026-09-01", "RoundUpAmount": null, "CountsAsTithe": true },
            { "Id": "f1600000-0000-0000-0000-000000000405", "Date": "2026-10-04", "Description": "Old gym", "Value": 20, "CategoryId": "{{MercadoId}}", "PaymentSourceAccountId": "{{DormantId}}", "CreditCardId": null, "ChargeDate": null, "InvoiceDate": null, "RoundUpAmount": null, "CountsAsTithe": true },
            { "Id": "{{DepositExpenseId}}", "Date": "2026-10-06", "Description": "Deposit to Reserva", "Value": 90, "CategoryId": "{{ReservaCategoryId}}", "PaymentSourceAccountId": "{{BarclaysId}}", "CreditCardId": null, "ChargeDate": null, "InvoiceDate": null, "RoundUpAmount": null, "CountsAsTithe": false, "OperationId": "{{OperationId}}" }
          ],
          "Incomes": [
            { "Id": "f1600000-0000-0000-0000-000000000801", "Date": "2026-10-01", "IncomeSourceId": "{{SalaryId}}", "GrossValue": null, "NetValue": 2000, "AccountId": "{{BarclaysId}}", "Description": "October pay" }
          ],
          "Transfers": [
            { "Id": "f1600000-0000-0000-0000-000000000501", "Date": "2026-10-05", "SourceAccountId": "{{BarclaysId}}", "DestinationAccountId": "{{ChaseId}}", "Amount": 75.25, "Note": "Top-up" },
            { "Id": "{{PaymentId}}", "Date": "2026-10-20", "SourceAccountId": "{{BarclaysId}}", "DestinationAccountId": "{{AmexId}}", "Amount": 18 }
          ],
          "CardStatements": [
            { "Id": "f1600000-0000-0000-0000-000000000901", "CreditCardId": "{{AmexId}}", "Year": 2026, "Month": 9, "IsPaid": true, "DueDate": "2026-10-03" },
            { "Id": "f1600000-0000-0000-0000-000000000902", "CreditCardId": "{{AmexId}}", "Year": 2026, "Month": 10, "IsPaid": true, "DueDate": "2026-11-03", "PaymentTransferId": "{{PaymentId}}" }
          ],
          "ReserveMovements": [
            { "Id": "{{DepositMovementId}}", "BucketId": "{{InvestimentoId}}", "Amount": 90, "Date": "2026-10-06", "Description": "October savings", "IncomeId": null, "OperationId": "{{OperationId}}" }
          ],
          "Version": 5
        }
        """;

    public MonthTransactionsViewAcceptanceTests()
        : base(timeProvider: TestClock.At(new DateTimeOffset(2026, 11, 15, 12, 0, 0, TimeSpan.Zero)))
    {
        ReplaceCashFlowDataBeforeStart(SeededData);
    }

    private async Task<List<MonthTransactionDTO>> MonthTransactionsAsync(int month) =>
        (await Client.GetFromJsonAsync<List<MonthTransactionDTO>>($"{Api}/accounts/transactions/2026/{month}"))!;

    private async Task<List<(Guid AccountId, AccountTransactionDTO Row)>> AccountScreensAsync(int month)
    {
        var accounts = (await Client.GetFromJsonAsync<List<AccountDTO>>($"{Api}/accounts"))!;
        var rows = new List<(Guid, AccountTransactionDTO)>();
        foreach (var account in accounts)
        {
            var accountRows = await Client.GetFromJsonAsync<List<AccountTransactionDTO>>($"{Api}/accounts/{account.Id}/transactions/2026/{month}");
            rows.AddRange(accountRows!.Select(row => (account.Id, row)));
        }

        return rows;
    }

    [Fact]
    [Trait("AC", "P60-F16-month-transactions-view-01")]
    public async Task MonthTransactions_ListEveryAccountsRowsAsTheAccountScreens_SettledChargeOnceOnItsCard()
    {
        var october = await MonthTransactionsAsync(10);
        var september = await MonthTransactionsAsync(9);
        var accountScreens = (await AccountScreensAsync(10))
            .Where(pair => !(pair.AccountId == Guid.Parse(BarclaysId) && pair.Row.Id == Guid.Parse(SettledChargeId)));

        using (new AssertionScope())
        {
            october.Select(row => (row.AccountId, row.Transaction)).Should().BeEquivalentTo(accountScreens);
            october.Select(row => row.Transaction.Date).Should().BeInDescendingOrder();
            october.Single(row => row.Transaction.Description == "Taxi").Should().BeEquivalentTo(
                new { AccountName = "Amex", AccountCurrency = "GBP", AccountIsActive = true, Transaction = new { Kind = "CardCharge", Date = new DateOnly(2026, 9, 25) } });
            october.Single(row => row.Transaction.Description == "Shoes returned").Transaction.Kind.Should().Be("CardRefund");
            october.Single(row => row.Transaction.Description == "Old gym").AccountIsActive.Should().BeFalse();
            september.Should().ContainSingle(row => row.Transaction.Id == Guid.Parse(SettledChargeId))
                .Which.AccountId.Should().Be(Guid.Parse(AmexId));
        }
    }

    [Fact]
    [Trait("AC", "P60-F16-month-transactions-view-02")]
    public async Task MonthTransactions_SpendPerCategory_EqualsCategoryTotals()
    {
        var rows = await MonthTransactionsAsync(10);
        var totals = (await Client.GetFromJsonAsync<List<CategoryTotalDTO>>($"{Api}/expenses/month/2026/10/category-totals"))!;

        var spend = rows
            .Where(row => row.Transaction.CategoryName is not null)
            .GroupBy(row => row.Transaction.CategoryName!)
            .ToDictionary(group => group.Key, group => -group.Sum(row => row.Transaction.Amount));

        using (new AssertionScope())
        {
            spend.Should().BeEquivalentTo(new Dictionary<string, decimal> { ["Mercado"] = 50.10m, ["Transporte"] = 30m, ["Reserva"] = 90m });
            foreach (var total in totals)
                spend.GetValueOrDefault(total.Category).Should().Be(total.TotalValue, total.Category);
            spend.Keys.Should().BeSubsetOf(totals.Select(total => total.Category));
        }
    }

    [Fact]
    [Trait("AC", "P60-F16-month-transactions-view-03")]
    public async Task MonthTransactions_RowsCarryTheirAccountAndOperation()
    {
        var rows = await MonthTransactionsAsync(10);

        var deposit = rows.Single(row => row.Transaction.Id == Guid.Parse(DepositExpenseId));
        var movement = rows.Single(row => row.Transaction.Id == Guid.Parse(DepositMovementId));

        using (new AssertionScope())
        {
            deposit.AccountId.Should().Be(Guid.Parse(BarclaysId));
            movement.AccountId.Should().Be(Guid.Parse(ReservaId));
            new[] { deposit, movement }.Select(row => row.Transaction.Operation).Should().AllSatisfy(operation =>
                operation.Should().BeEquivalentTo(new { OperationId = Guid.Parse(OperationId), PotsAccountId = Guid.Parse(ReservaId), Year = 2026, Month = 10 }));
            rows.Should().OnlyContain(row => row.AccountId != Guid.Empty && row.AccountName.Length > 0);
        }
    }

    [Theory]
    [InlineData(2026, 13)]
    [InlineData(2026, 0)]
    public async Task MonthTransactions_InvalidMonth_Returns400(int year, int month)
    {
        var response = await Client.GetAsync($"{Api}/accounts/transactions/{year}/{month}");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
