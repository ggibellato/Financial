using System.Net;
using System.Net.Http.Json;
using Financial.CashFlow.Application.DTOs;
using FluentAssertions;
using FluentAssertions.Execution;

namespace Financial.Api.Tests.Acceptance;

[Trait("Category", "Integration")]
public class AccountEndpointsAcceptanceTests : ApiEndpointTests
{
    private const string BarclaysId = "8f3b1c1a-2e3a-4b1a-9a7f-100000000001";
    private const string ChaseId = "8f3b1c1a-2e3a-4b1a-9a7f-100000000002";
    private const string ClosedId = "8f3b1c1a-2e3a-4b1a-9a7f-100000000003";
    private const string GroceriesId = "8f3b1c1a-2e3a-4b1a-9a7f-200000000001";
    private const string AmexId = "8f3b1c1a-2e3a-4b1a-9a7f-300000000001";
    private const string SalaryId = "8f3b1c1a-2e3a-4b1a-9a7f-400000000001";
    private const string TaxiId = "8f3b1c1a-2e3a-4b1a-9a7f-500000000004";

    private const string SeededData = $$"""
        {
          "Accounts": [
            { "Id": "{{ChaseId}}", "Name": "Chase", "Template": "Bank", "Currency": "GBP", "IsActive": true, "OpeningBalance": { "Amount": 200, "StartDate": "2026-01-01" }, "RoundUp": { "Enabled": false } },
            { "Id": "{{ClosedId}}", "Name": "Closed", "Template": "Bank", "Currency": "GBP", "IsActive": false, "OpeningBalance": { "Amount": 0, "StartDate": "2026-01-01" }, "RoundUp": { "Enabled": false } },
            { "Id": "{{BarclaysId}}", "Name": "Barclays", "Template": "Bank", "Currency": "GBP", "IsActive": true, "OpeningBalance": { "Amount": 1000, "StartDate": "2026-01-01" }, "RoundUp": { "Enabled": true } }
          ],
          "Categories": [
            { "Id": "{{GroceriesId}}", "Name": "Mercado", "Active": true, "IsInvestment": false, "IsTithe": false }
          ],
          "CreditCards": [
            { "Id": "{{AmexId}}", "Name": "Amex", "IsActive": true }
          ],
          "IncomeSources": [
            { "Id": "{{SalaryId}}", "Name": "Salary", "IsActive": true, "Group": "Salary", "AutoSplitToReserve": false }
          ],
          "Expenses": [
            { "Id": "8f3b1c1a-2e3a-4b1a-9a7f-500000000001", "Date": "2026-09-10", "Description": "Shop", "Value": 50, "CategoryId": "{{GroceriesId}}", "PaymentSourceAccountId": "{{BarclaysId}}", "CreditCardId": null, "ChargeDate": null, "InvoiceDate": null, "RoundUpAmount": null, "CountsAsTithe": true },
            { "Id": "8f3b1c1a-2e3a-4b1a-9a7f-500000000002", "Date": "2026-10-03", "Description": "Corner shop", "Value": 5, "CategoryId": "{{GroceriesId}}", "PaymentSourceAccountId": "{{BarclaysId}}", "CreditCardId": null, "ChargeDate": null, "InvoiceDate": null, "RoundUpAmount": null, "CountsAsTithe": true },
            { "Id": "8f3b1c1a-2e3a-4b1a-9a7f-500000000003", "Date": "2026-10-10", "Description": "Tesco", "Value": 42.10, "CategoryId": "{{GroceriesId}}", "PaymentSourceAccountId": "{{BarclaysId}}", "CreditCardId": null, "ChargeDate": null, "InvoiceDate": null, "RoundUpAmount": 0.90, "CountsAsTithe": true },
            { "Id": "{{TaxiId}}", "Date": "2026-10-12", "Description": "Taxi", "Value": 30, "CategoryId": "{{GroceriesId}}", "PaymentSourceAccountId": "{{BarclaysId}}", "CreditCardId": "{{AmexId}}", "ChargeDate": "2026-09-20", "InvoiceDate": "2026-10-01", "RoundUpAmount": null, "CountsAsTithe": true }
          ],
          "Incomes": [
            { "Id": "8f3b1c1a-2e3a-4b1a-9a7f-600000000001", "Date": "2026-10-01", "IncomeSourceId": "{{SalaryId}}", "GrossValue": null, "NetValue": 2000, "AccountId": "{{BarclaysId}}", "Description": "October pay", "SplitToReserve": false }
          ],
          "Transfers": [
            { "Id": "8f3b1c1a-2e3a-4b1a-9a7f-700000000001", "Date": "2026-10-05", "SourceAccountId": "{{BarclaysId}}", "DestinationAccountId": "{{ChaseId}}", "Amount": 75.25, "Note": "Top-up" }
          ],
          "BalanceAdjustments": [
            { "Id": "8f3b1c1a-2e3a-4b1a-9a7f-800000000001", "Date": "2026-10-14", "AccountId": "{{BarclaysId}}", "TargetBalance": 2800.25, "Delta": 3.50, "Note": "Statement" }
          ],
          "Version": 2
        }
        """;

    public AccountEndpointsAcceptanceTests()
        : base(timeProvider: TestClock.At(new DateTimeOffset(2026, 10, 15, 12, 0, 0, TimeSpan.FromHours(1))))
    {
        ReplaceCashFlowDataBeforeStart(SeededData);
    }

    [Fact]
    public async Task GetAccounts_SeededAccounts_ListsEveryAccountByName()
    {
        var accounts = await Client.GetFromJsonAsync<List<AccountDTO>>("/api/v1/financial/accounts");

        accounts!.Select(account => (account.Name, account.TemplateName, account.Currency, account.IsActive)).Should().Equal(
            ("Amex", "CreditCard", "GBP", true),
            ("Barclays", "Bank", "GBP", true),
            ("Chase", "Bank", "GBP", true),
            ("Closed", "Bank", "GBP", false));
    }

    [Theory]
    [Trait("AC", "P60-F05-bank-account-screen-03")]
    [InlineData(9)]
    [InlineData(10)]
    public async Task GetAccount_AnyMonth_BalanceEqualsGroupSummary(int month)
    {
        var summary = await Client.GetFromJsonAsync<AccountGroupSummaryDTO>($"/api/v1/financial/accounts/groups/default/summary/2026/{month}");

        using (new AssertionScope())
        {
            foreach (var member in summary!.Members)
                (await AccountAsync(member.AccountId.ToString(), month)).Balance.Should().Be(member.Balance, member.Name);
        }
    }

    [Fact]
    [Trait("AC", "P60-F05-bank-account-screen-03")]
    public async Task GetAccount_Barclays_ReturnsTheHeaderFields()
    {
        var detail = await AccountAsync(BarclaysId, 10);

        using (new AssertionScope())
        {
            detail.Name.Should().Be("Barclays");
            detail.TemplateName.Should().Be("Bank");
            detail.Currency.Should().Be("GBP");
            detail.Year.Should().Be(2026);
            detail.Month.Should().Be(10);
            detail.Balance.Should().Be(2800.25m);
        }
    }

    [Fact]
    [Trait("AC", "P60-F05-bank-account-screen-04")]
    public async Task GetTransactions_SeededMonth_ListsEveryRecordSigned()
    {
        var rows = await TransactionsAsync(BarclaysId, 10);

        using (new AssertionScope())
        {
            rows.Select(row => (row.Kind, row.Date, row.Description, row.Amount, row.RoundUp, row.CanEdit)).Should().Equal(
                ("Adjustment", new DateOnly(2026, 10, 14), "Statement", 3.50m, (decimal?)null, true),
                ("CardCharge", new DateOnly(2026, 10, 12), "Taxi", -30m, null, false),
                ("Expense", new DateOnly(2026, 10, 10), "Tesco", -42.10m, -0.90m, true),
                ("TransferOut", new DateOnly(2026, 10, 5), "Top-up", -75.25m, null, true),
                ("Expense", new DateOnly(2026, 10, 3), "Corner shop", -5m, null, true),
                ("Income", new DateOnly(2026, 10, 1), "October pay", 2000m, null, true));
            rows.Single(row => row.Kind == "CardCharge").Counterpart.Should().Be("Amex");
            rows.Single(row => row.Kind == "CardCharge").Expense!.Id.Should().Be(Guid.Parse(TaxiId));
            rows.Single(row => row.Kind == "TransferOut").Counterpart.Should().Be("Chase");
            rows.Single(row => row.Kind == "Income").Counterpart.Should().Be("Salary");
            rows.Single(row => row.Description == "Tesco").CategoryName.Should().Be("Mercado");
        }
    }

    [Fact]
    [Trait("AC", "P60-F05-bank-account-screen-04")]
    public async Task GetTransactions_MonthTotal_EqualsTheChangeInBalance()
    {
        var rows = await TransactionsAsync(BarclaysId, 10);
        var september = await AccountAsync(BarclaysId, 9);
        var october = await AccountAsync(BarclaysId, 10);

        rows.Sum(row => row.Amount + (row.RoundUp ?? 0m)).Should().Be(october.Balance - september.Balance);
    }

    [Fact]
    [Trait("AC", "P60-F05-bank-account-screen-05")]
    public async Task GetAccount_Bank_ReturnsSixAllowedActions()
    {
        var detail = await AccountAsync(BarclaysId, 10);

        detail.AllowedActions.Should().Equal("AddExpense", "AddIncome", "TransferTo", "TransferFrom", "PayRecurringBill", "Reconcile");
    }

    [Fact]
    [Trait("AC", "P60-F05-bank-account-screen-05")]
    public async Task GetAccount_InactiveBank_ReturnsNoActions()
    {
        var detail = await AccountAsync(ClosedId, 10);

        using (new AssertionScope())
        {
            detail.IsActive.Should().BeFalse();
            detail.AllowedActions.Should().BeEmpty();
        }
    }

    [Fact]
    [Trait("AC", "P60-F05-bank-account-screen-04")]
    public async Task GetTransactions_TransferBetweenAccounts_AppearsOnBothAndMovesBothBalances()
    {
        var barclaysBefore = await AccountAsync(BarclaysId, 10);
        var chaseBefore = await AccountAsync(ChaseId, 10);

        var response = await Client.PostAsJsonAsync("/api/v1/financial/accounts/transfers", new AccountTransferCreateDTO
        {
            Date = new DateOnly(2026, 10, 12),
            SourceAccountId = Guid.Parse(ChaseId),
            DestinationAccountId = Guid.Parse(BarclaysId),
            Amount = 60m,
            Note = "Back"
        });
        var transfer = await response.Content.ReadFromJsonAsync<AccountTransferDTO>();

        var barclaysRows = await TransactionsAsync(BarclaysId, 10);
        var chaseRows = await TransactionsAsync(ChaseId, 10);
        var barclaysAfter = await AccountAsync(BarclaysId, 10);
        var chaseAfter = await AccountAsync(ChaseId, 10);
        using (new AssertionScope())
        {
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            barclaysRows.Should().ContainSingle(row => row.Id == transfer!.Id)
                .Which.Should().Match<AccountTransactionDTO>(row => row.Kind == "TransferIn" && row.Amount == 60m && row.Counterpart == "Chase");
            chaseRows.Should().ContainSingle(row => row.Id == transfer!.Id)
                .Which.Should().Match<AccountTransactionDTO>(row => row.Kind == "TransferOut" && row.Amount == -60m && row.Counterpart == "Barclays");
            (barclaysAfter.Balance - barclaysBefore.Balance).Should().Be(60m);
            (chaseAfter.Balance - chaseBefore.Balance).Should().Be(-60m);
        }
    }

    [Theory]
    [InlineData("/api/v1/financial/accounts/{0}/2026/10")]
    [InlineData("/api/v1/financial/accounts/{0}/transactions/2026/10")]
    public async Task GetAccountRoutes_UnknownId_Return404(string route)
    {
        var response = await Client.GetAsync(string.Format(route, Guid.NewGuid()));

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Theory]
    [InlineData("/api/v1/financial/accounts/{0}/2026/13")]
    [InlineData("/api/v1/financial/accounts/{0}/transactions/2026/0")]
    public async Task GetAccountRoutes_MonthOutOfRange_Return400(string route)
    {
        var response = await Client.GetAsync(string.Format(route, BarclaysId));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    private async Task<AccountDetailDTO> AccountAsync(string id, int month) =>
        (await Client.GetFromJsonAsync<AccountDetailDTO>($"/api/v1/financial/accounts/{id}/2026/{month}"))!;

    private async Task<List<AccountTransactionDTO>> TransactionsAsync(string id, int month) =>
        (await Client.GetFromJsonAsync<List<AccountTransactionDTO>>($"/api/v1/financial/accounts/{id}/transactions/2026/{month}"))!;
}
