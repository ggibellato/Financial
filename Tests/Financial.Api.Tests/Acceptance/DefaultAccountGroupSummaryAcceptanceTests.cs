using System.Net;
using System.Net.Http.Json;
using Financial.CashFlow.Application.DTOs;
using FluentAssertions;
using FluentAssertions.Execution;

namespace Financial.Api.Tests.Acceptance;

[Trait("Category", "Integration")]
public class DefaultAccountGroupSummaryAcceptanceTests : ApiEndpointTests
{
    private const string BarclaysId = "8f3b1c1a-2e3a-4b1a-9a7f-100000000001";
    private const string ChaseId = "8f3b1c1a-2e3a-4b1a-9a7f-100000000002";
    private const string ItauId = "8f3b1c1a-2e3a-4b1a-9a7f-100000000004";
    private const string ClosedId = "8f3b1c1a-2e3a-4b1a-9a7f-100000000005";
    private const string ClosedSavingsId = "8f3b1c1a-2e3a-4b1a-9a7f-100000000006";
    private const string GroceriesId = "8f3b1c1a-2e3a-4b1a-9a7f-200000000001";
    private const string HomeId = "8f3b1c1a-2e3a-4b1a-9a7f-200000000002";
    private const string AmexId = "8f3b1c1a-2e3a-4b1a-9a7f-300000000001";
    private const string SalaryId = "8f3b1c1a-2e3a-4b1a-9a7f-400000000001";
    private const string DividendsId = "8f3b1c1a-2e3a-4b1a-9a7f-400000000002";

    private const string SeededData = $$"""
        {
          "Accounts": [
            { "Id": "{{ItauId}}", "Name": "Itaú", "Template": "Bank", "Currency": "BRL", "IsActive": true, "OpeningBalance": { "Amount": 500, "StartDate": "2026-01-01" }, "RoundUp": { "Enabled": true } },
            { "Id": "{{ChaseId}}", "Name": "Chase", "Template": "Bank", "Currency": "GBP", "IsActive": true, "OpeningBalance": { "Amount": 200, "StartDate": "2026-01-01" }, "RoundUp": { "Enabled": true } },
            { "Id": "{{BarclaysId}}", "Name": "Barclays", "Template": "Bank", "Currency": "GBP", "IsActive": true, "OpeningBalance": { "Amount": 1000, "StartDate": "2026-01-01" }, "RoundUp": { "Enabled": false } },
            { "Id": "{{ClosedId}}", "Name": "Closed", "Template": "Bank", "Currency": "GBP", "IsActive": false, "OpeningBalance": { "Amount": 0, "StartDate": "2026-01-01" }, "RoundUp": { "Enabled": false } },
            { "Id": "{{ClosedSavingsId}}", "Name": "Closed Savings", "Template": "Bank", "Currency": "GBP", "IsActive": false, "OpeningBalance": { "Amount": 120, "StartDate": "2026-01-01" }, "RoundUp": { "Enabled": false } },
            { "Id": "{{AmexId}}", "Name": "Amex", "Template": "CreditCard", "Currency": "GBP", "IsActive": true, "OpeningBalance": null, "RoundUp": null }
          ],
          "Categories": [
            { "Id": "{{GroceriesId}}", "Name": "Mercado", "Active": true, "IsInvestment": false, "IsTithe": false },
            { "Id": "{{HomeId}}", "Name": "Casa", "Active": true, "IsInvestment": false, "IsTithe": false }
          ],
          "IncomeSources": [
            { "Id": "{{SalaryId}}", "Name": "Salary", "IsActive": true, "Group": "Salary" },
            { "Id": "{{DividendsId}}", "Name": "Dividends", "IsActive": true, "Group": "NonReportable" }
          ],
          "Expenses": [
            { "Id": "8f3b1c1a-2e3a-4b1a-9a7f-500000000001", "Date": "2026-08-20", "Description": "Closing", "Value": 40, "CategoryId": "{{HomeId}}", "PaymentSourceAccountId": "{{ClosedId}}", "CreditCardId": null, "ChargeDate": null, "InvoiceDate": null, "RoundUpAmount": null, "CountsAsTithe": true },
            { "Id": "8f3b1c1a-2e3a-4b1a-9a7f-500000000002", "Date": "2026-09-10", "Description": "Shop", "Value": 50, "CategoryId": "{{GroceriesId}}", "PaymentSourceAccountId": "{{BarclaysId}}", "CreditCardId": null, "ChargeDate": null, "InvoiceDate": null, "RoundUpAmount": null, "CountsAsTithe": true },
            { "Id": "8f3b1c1a-2e3a-4b1a-9a7f-500000000003", "Date": "2026-09-03", "Description": "Coffee", "Value": 2.30, "CategoryId": "{{GroceriesId}}", "PaymentSourceAccountId": "{{ChaseId}}", "CreditCardId": null, "ChargeDate": null, "InvoiceDate": null, "RoundUpAmount": 0.70, "CountsAsTithe": true },
            { "Id": "8f3b1c1a-2e3a-4b1a-9a7f-500000000004", "Date": "2026-09-28", "Description": "Lunch", "Value": 4.80, "CategoryId": "{{GroceriesId}}", "PaymentSourceAccountId": "{{ChaseId}}", "CreditCardId": null, "ChargeDate": null, "InvoiceDate": null, "RoundUpAmount": 0.20, "CountsAsTithe": true },
            { "Id": "8f3b1c1a-2e3a-4b1a-9a7f-500000000005", "Date": "2026-09-04", "Description": "Padaria", "Value": 9.75, "CategoryId": "{{GroceriesId}}", "PaymentSourceAccountId": "{{ItauId}}", "CreditCardId": null, "ChargeDate": null, "InvoiceDate": null, "RoundUpAmount": 0.25, "CountsAsTithe": true },
            { "Id": "8f3b1c1a-2e3a-4b1a-9a7f-500000000006", "Date": "2026-08-25", "Description": "Card charge", "Value": 30, "CategoryId": "{{HomeId}}", "PaymentSourceAccountId": null, "CreditCardId": "{{AmexId}}", "ChargeDate": "2026-08-25", "InvoiceDate": "2026-09-01", "RoundUpAmount": null, "CountsAsTithe": true },
            { "Id": "8f3b1c1a-2e3a-4b1a-9a7f-500000000007", "Date": "2026-10-02", "Description": "Later", "Value": 20, "CategoryId": "{{GroceriesId}}", "PaymentSourceAccountId": "{{ChaseId}}", "CreditCardId": null, "ChargeDate": null, "InvoiceDate": null, "RoundUpAmount": 0.40, "CountsAsTithe": true }
          ],
          "Incomes": [
            { "Id": "8f3b1c1a-2e3a-4b1a-9a7f-600000000001", "Date": "2026-08-05", "IncomeSourceId": "{{SalaryId}}", "GrossValue": null, "NetValue": 40, "AccountId": "{{ClosedId}}", "Description": null },
            { "Id": "8f3b1c1a-2e3a-4b1a-9a7f-600000000002", "Date": "2026-09-25", "IncomeSourceId": "{{DividendsId}}", "GrossValue": null, "NetValue": 12.34, "AccountId": "{{BarclaysId}}", "Description": null },
            { "Id": "8f3b1c1a-2e3a-4b1a-9a7f-600000000003", "Date": "2026-09-01", "IncomeSourceId": "{{SalaryId}}", "GrossValue": 2500, "NetValue": 2000, "AccountId": "{{BarclaysId}}", "Description": null },
            { "Id": "8f3b1c1a-2e3a-4b1a-9a7f-600000000004", "Date": "2026-09-15", "IncomeSourceId": "{{SalaryId}}", "GrossValue": null, "NetValue": 100, "AccountId": "{{ChaseId}}", "Description": null }
          ],
          "Version": 4
        }
        """;

    public DefaultAccountGroupSummaryAcceptanceTests()
        : base(timeProvider: TestClock.At(new DateTimeOffset(2026, 10, 15, 12, 0, 0, TimeSpan.FromHours(1))))
    {
        ReplaceCashFlowDataBeforeStart(SeededData);
    }

    [Fact]
    [Trait("AC", "P60-F04-default-account-group-and-summary-query-01")]
    public async Task GetDefaultGroupSummary_SeededBanks_ListsActiveBanksByNamePlusInactiveWithMoney()
    {
        var summary = await SummaryAsync(9);

        BankMembers(summary).Select(member => member.Name).Should().Equal("Barclays", "Chase", "Closed Savings", "Itaú");
    }

    [Fact]
    [Trait("AC", "P60-F04-default-account-group-and-summary-query-01")]
    public async Task GetDefaultGroupSummary_InactiveBankWithRecordInMonth_IncludedThatMonthOnly()
    {
        var august = await SummaryAsync(8);
        var september = await SummaryAsync(9);

        using (new AssertionScope())
        {
            august.Members.Should().Contain(member => member.Name == "Closed" && member.Balance == 0m);
            september.Members.Should().NotContain(member => member.Name == "Closed");
        }
    }

    [Fact]
    [Trait("AC", "P60-F04-default-account-group-and-summary-query-02")]
    public async Task GetDefaultGroupSummary_BankCreatedThroughBanks_JoinsWithNoOtherChange()
    {
        var created = await Client.PostAsJsonAsync("/api/v1/financial/banks", new BankCreateDTO { Name = "Halifax", RoundUpEnabled = false });

        var summary = await SummaryAsync(9);

        using (new AssertionScope())
        {
            created.StatusCode.Should().Be(HttpStatusCode.OK);
            BankMembers(summary).Select(member => member.Name).Should().Equal("Barclays", "Chase", "Closed Savings", "Halifax", "Itaú");
            summary.Members.Single(member => member.Name == "Halifax").TemplateName.Should().Be("Bank");
        }
    }

    [Theory]
    [Trait("AC", "P60-F04-default-account-group-and-summary-query-03")]
    [InlineData(8)]
    [InlineData(9)]
    public async Task GetDefaultGroupSummary_AnyMonth_BalancesAndRoundUpsEqualTodaysEndpoints(int month)
    {
        var summary = await SummaryAsync(month);
        var balances = await Client.GetFromJsonAsync<List<BankBalanceDTO>>($"/api/v1/financial/banks/month/2026/{month}/balances");
        var expenses = await Client.GetFromJsonAsync<List<ExpenseDTO>>($"/api/v1/financial/expenses/month/2026/{month}");

        using (new AssertionScope())
        {
            foreach (var member in BankMembers(summary))
            {
                member.Balance.Should().Be(balances!.Single(balance => balance.Bank == member.Name).Balance, member.Name);
                member.RoundUpTotal.Should().Be(
                    expenses!.Where(expense => expense.PaymentSourceBankId == member.AccountId).Sum(expense => expense.RoundUpAmount ?? 0m), member.Name);
            }
        }
    }

    [Fact]
    [Trait("AC", "P60-F04-default-account-group-and-summary-query-03")]
    public async Task GetDefaultGroupSummary_September_ReturnsTheSeededBalancesAndRoundUps()
    {
        var summary = await SummaryAsync(9);

        BankMembers(summary).Select(member => (member.Name, member.Balance, member.RoundUpTotal)).Should().Equal(
            ("Barclays", 2962.34m, 0m),
            ("Chase", 292.00m, 0.90m),
            ("Closed Savings", 120m, 0m),
            ("Itaú", 490.00m, 0.25m));
    }

    [Fact]
    [Trait("AC", "P60-F04-default-account-group-and-summary-query-04")]
    public async Task GetDefaultGroupSummary_MixedCurrencies_TotalsPerCurrencyNeverCombined()
    {
        var summary = await SummaryAsync(9);

        summary.Totals.Select(total => (total.Currency, total.Balance, total.RoundUpTotal)).Should().Equal(
            ("GBP", 3374.34m, 0.90m),
            ("BRL", 490.00m, 0.25m));
    }

    [Fact]
    [Trait("AC", "P60-F04-default-account-group-and-summary-query-05")]
    public async Task GetDefaultGroupSummary_SeededMonth_CategoryIncomeAndIncomingEqualTodaysFigures()
    {
        var summary = await SummaryAsync(9);
        var categoryTotals = await Client.GetFromJsonAsync<List<CategoryTotalDTO>>("/api/v1/financial/expenses/month/2026/9/category-totals");
        var incomes = await Client.GetFromJsonAsync<List<IncomeDTO>>("/api/v1/financial/incomes/month/2026/9");

        using (new AssertionScope())
        {
            summary.CategoryTotalsSum.Should().Be(categoryTotals!.Sum(category => category.TotalValue)).And.Be(96.85m);
            summary.IncomeTotals.Select(income => (income.IncomeSourceId, income.Name, income.NetValue, income.GrossValue)).Should().Equal(
                (Guid.Parse(DividendsId), "Dividends", 12.34m, (decimal?)null),
                (Guid.Parse(SalaryId), "Salary", 2100m, 2500m));
            summary.TotalIncoming.Should().Be(incomes!.Sum(income => income.NetValue)).And.Be(2112.34m);
        }
    }

    [Fact]
    public async Task GetDefaultGroupSummary_InvalidMonth_Returns400()
    {
        var response = await Client.GetAsync("/api/v1/financial/accounts/groups/default/summary/2026/13");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    private static IEnumerable<AccountGroupMemberDTO> BankMembers(AccountGroupSummaryDTO summary) =>
        summary.Members.Where(member => member.Statement is null);

    private async Task<AccountGroupSummaryDTO> SummaryAsync(int month) =>
        (await Client.GetFromJsonAsync<AccountGroupSummaryDTO>($"/api/v1/financial/accounts/groups/default/summary/2026/{month}"))!;
}
