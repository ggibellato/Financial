using System.Net;
using System.Net.Http.Json;
using Financial.CashFlow.Application.DTOs;
using Financial.CashFlow.Domain.Entities.Transactions;
using FluentAssertions;
using FluentAssertions.Execution;

namespace Financial.Api.Tests.Acceptance;

[Trait("Category", "Integration")]
public class PayInPayOutAcceptanceTests : ApiEndpointTests
{
    private const string Api = "/api/v1/financial";
    private const string BarclaysId = "6a40d000-0000-0000-0000-000000000001";
    private const string ChaseSaveId = "6a40d000-0000-0000-0000-000000000002";
    private const string SaverId = "6a40d000-0000-0000-0000-000000000003";
    private const string AmexId = "6a40d000-0000-0000-0000-000000000004";
    private const string ClosedId = "6a40d000-0000-0000-0000-000000000005";
    private const string MercadoId = "6a40d000-0000-0000-0000-000000000011";
    private const string SalaryId = "6a40d000-0000-0000-0000-000000000021";

    private const string Terms = """{ "Frequency": "Daily", "IsTaxFree": false, "Rates": [{ "AnnualPercent": 4, "EffectiveFrom": "2026-01-01" }] }""";
    private const string RoundUpOnlyTerms = """{ "Frequency": "Daily", "IsTaxFree": false, "Rates": [{ "AnnualPercent": 5, "EffectiveFrom": "2026-01-01" }], "RoundUpOnly": true }""";

    private const string SeededData = $$"""
        {
          "Accounts": [
            { "Id": "{{BarclaysId}}", "Name": "Barclays", "Template": "Bank", "Currency": "GBP", "IsActive": true, "OpeningBalance": { "Amount": 1000, "StartDate": "2026-01-01" }, "RoundUp": { "Enabled": false } },
            { "Id": "{{ChaseSaveId}}", "Name": "Chase Save", "Template": "Savings", "Currency": "GBP", "IsActive": true, "OpeningBalance": { "Amount": 500, "StartDate": "2026-09-01" }, "RoundUp": null, "Pots": null, "CategoryId": null, "Savings": {{Terms}} },
            { "Id": "{{SaverId}}", "Name": "Round-up saver", "Template": "Savings", "Currency": "GBP", "IsActive": true, "OpeningBalance": { "Amount": 10, "StartDate": "2026-09-01" }, "RoundUp": null, "Pots": null, "CategoryId": null, "Savings": {{RoundUpOnlyTerms}} },
            { "Id": "{{AmexId}}", "Name": "Amex", "Template": "CreditCard", "Currency": "GBP", "IsActive": true, "OpeningBalance": null, "RoundUp": null },
            { "Id": "{{ClosedId}}", "Name": "Closed", "Template": "Bank", "Currency": "GBP", "IsActive": false, "OpeningBalance": { "Amount": 0, "StartDate": "2026-01-01" }, "RoundUp": { "Enabled": false } }
          ],
          "Categories": [
            { "Id": "{{MercadoId}}", "Name": "Mercado", "Active": true, "IsInvestment": false, "IsTithe": false }
          ],
          "IncomeSources": [
            { "Id": "{{SalaryId}}", "Name": "Salary", "IsActive": true, "Group": "Salary" }
          ],
          "Version": 5
        }
        """;

    public PayInPayOutAcceptanceTests()
        : base(timeProvider: TestClock.At(TestClock.MidOctober))
    {
        ReplaceCashFlowDataBeforeStart(SeededData);
    }

    [Fact]
    [Trait("AC", "P67-F01-pay-in-out-01")]
    public async Task PayInAndPayOut_AreCreatedEditedAndDeleted_AndTheBalanceFollows()
    {
        var payIn = await PostAsync(ChaseSaveId, "In", 40m, "Chase cashback");
        var payOut = await PostAsync(BarclaysId, "Out", 25m, "Money to Ana");
        var afterCreate = ((await DetailAsync(ChaseSaveId)).Balance, (await DetailAsync(BarclaysId)).Balance);

        var edit = await Client.PutAsJsonAsync($"{Api}/accounts/{ChaseSaveId}/payments/{payIn.Id}", new ExternalPaymentUpdateDTO
        {
            Date = new DateOnly(2026, 10, 6), Amount = 45.50m, Description = "Chase cashback (Sep)"
        });
        var afterEdit = (await DetailAsync(ChaseSaveId)).Balance;
        var delete = await Client.DeleteAsync($"{Api}/accounts/{BarclaysId}/payments/{payOut.Id}");

        using (new AssertionScope())
        {
            afterCreate.Should().Be((540m, 975m));
            edit.StatusCode.Should().Be(HttpStatusCode.OK);
            afterEdit.Should().Be(545.50m);
            delete.StatusCode.Should().Be(HttpStatusCode.NoContent);
            (await DetailAsync(BarclaysId)).Balance.Should().Be(1000m);
        }
    }

    [Theory]
    [Trait("AC", "P67-F01-pay-in-out-02")]
    [InlineData(0.0, "Gift", ExternalPayment.AmountMessage)]
    [InlineData(10.0, " ", ExternalPayment.DescriptionMessage)]
    public async Task PayIn_WithAnInvalidAmountOrDescription_Returns400(double amount, string description, string message)
    {
        var response = await Client.PostAsJsonAsync($"{Api}/accounts/{BarclaysId}/payments", Request("In", (decimal)amount, description));

        await ShouldBeProblemAsync(response, HttpStatusCode.BadRequest, message);
    }

    [Fact]
    [Trait("AC", "P67-F01-pay-in-out-03")]
    public async Task RoundUpOnly_RefusesAPayInAndAllowsAPayOut()
    {
        var payIn = await Client.PostAsJsonAsync($"{Api}/accounts/{SaverId}/payments", Request("In", 5m, "Gift"));
        var payOut = await Client.PostAsJsonAsync($"{Api}/accounts/{SaverId}/payments", Request("Out", 5m, "Moved out"));

        using (new AssertionScope())
        {
            await ShouldBeProblemAsync(payIn, HttpStatusCode.BadRequest, "Round-up saver only receives round-ups.");
            payOut.StatusCode.Should().Be(HttpStatusCode.Created);
        }
    }

    [Theory]
    [Trait("AC", "P67-F01-pay-in-out-04")]
    [InlineData(AmexId)]
    [InlineData(ClosedId)]
    public async Task PayIn_OnACardOrAnInactiveAccount_Returns400(string accountId)
    {
        var response = await Client.PostAsJsonAsync($"{Api}/accounts/{accountId}/payments", Request("In", 5m, "Gift"));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    [Trait("AC", "P67-F01-pay-in-out-05")]
    public async Task Payments_LeaveIncomeTitheAndCategoryTotalsUnchanged()
    {
        await PostAsync(BarclaysId, "In", 100m, "Money from Ana");
        await PostAsync(BarclaysId, "Out", 30m, "Money to João");

        var incomes = await Client.GetFromJsonAsync<List<IncomeDTO>>($"{Api}/incomes/month/2026/10");
        var tithe = await Client.GetFromJsonAsync<TitheSummaryDTO>($"{Api}/tithe/month/2026/10");
        var summary = await Client.GetFromJsonAsync<AccountGroupSummaryDTO>($"{Api}/accounts/groups/default/summary/2026/10");

        using (new AssertionScope())
        {
            incomes.Should().BeEmpty();
            tithe!.IncomeBase.Should().Be(0m);
            summary!.TotalIncoming.Should().Be(0m);
            summary.CategoryTotalsSum.Should().Be(0m);
        }
    }

    [Fact]
    [Trait("AC", "P67-F01-pay-in-out-06")]
    public async Task AllowedActions_IncludePayInAndPayOut_AndPayOutOnlyForRoundUpOnly()
    {
        using (new AssertionScope())
        {
            (await DetailAsync(BarclaysId)).AllowedActions.Should().Contain(["PayIn", "PayOut"]);
            (await DetailAsync(ChaseSaveId)).AllowedActions.Should().Contain(["PayIn", "PayOut"]);
            (await DetailAsync(SaverId)).AllowedActions.Should().Contain("PayOut").And.NotContain("PayIn");
            (await DetailAsync(AmexId)).AllowedActions.Should().NotContain(["PayIn", "PayOut"]);
        }
    }

    [Fact]
    [Trait("AC", "P67-F01-pay-in-out-07")]
    public async Task Payments_ShowAsSignedRowsInTheAccountAndMonthLists()
    {
        await PostAsync(BarclaysId, "In", 100m, "Money from Ana");
        await PostAsync(BarclaysId, "Out", 30m, "Money to João");

        var rows = await Client.GetFromJsonAsync<List<AccountTransactionDTO>>($"{Api}/accounts/{BarclaysId}/transactions/2026/10");
        var monthRows = await Client.GetFromJsonAsync<List<MonthTransactionDTO>>($"{Api}/accounts/transactions/2026/10");

        using (new AssertionScope())
        {
            rows!.Select(row => (row.Kind, row.Description, row.Amount, row.CanEdit)).Should().BeEquivalentTo(
                [("PayIn", "Money from Ana", 100m, true), ("PayOut", "Money to João", -30m, true)]);
            monthRows!.Select(row => row.Transaction.Kind).Should().Contain(["PayIn", "PayOut"]);
        }
    }

    [Fact]
    [Trait("AC", "P67-F03-ui-cleanups-01")]
    public async Task AccountsTab_ReturnsNoRoundUpTotalForABankWithRoundUpOff()
    {
        var summary = await Client.GetFromJsonAsync<AccountGroupSummaryDTO>($"{Api}/accounts/groups/default/summary/2026/10");

        summary!.Members.Single(member => member.Name == "Barclays").RoundUpTotal.Should().BeNull();
    }

    [Fact]
    [Trait("AC", "P67-CF-cross-feature-01")]
    public async Task CashbackPayInToChaseSave_RaisesItsNextSnapshotSuggestionButNotIncome()
    {
        await PostAsync(ChaseSaveId, "In", 12.34m, "Chase cashback");
        await Client.PostAsJsonAsync($"{Api}/investment-accounts", new InvestmentAccountCreateDTO
        {
            Name = "Chase Save",
            IsActive = true,
            IsLiability = false,
            Source = "SavingsAccountBalance",
            SavingsAccountId = Guid.Parse(ChaseSaveId)
        });

        var suggestions = await Client.GetFromJsonAsync<InvestmentSnapshotSuggestionsDTO>($"{Api}/investment-snapshots/2026/11/suggestions");
        var tithe = await Client.GetFromJsonAsync<TitheSummaryDTO>($"{Api}/tithe/month/2026/10");

        using (new AssertionScope())
        {
            suggestions!.Suggestions.Should().ContainSingle().Which.SuggestedValue.Should().Be(512.34m);
            tithe!.IncomeBase.Should().Be(0m);
        }
    }

    private static ExternalPaymentCreateDTO Request(string direction, decimal amount, string description) => new()
    {
        Direction = direction,
        Date = new DateOnly(2026, 10, 5),
        Amount = amount,
        Description = description
    };

    private async Task<ExternalPaymentDTO> PostAsync(string accountId, string direction, decimal amount, string description)
    {
        var response = await Client.PostAsJsonAsync($"{Api}/accounts/{accountId}/payments", Request(direction, amount, description));
        response.StatusCode.Should().Be(HttpStatusCode.Created, await response.Content.ReadAsStringAsync());
        return (await response.Content.ReadFromJsonAsync<ExternalPaymentDTO>())!;
    }

    private async Task<AccountDetailDTO> DetailAsync(string id) =>
        (await Client.GetFromJsonAsync<AccountDetailDTO>($"{Api}/accounts/{id}/2026/10"))!;
}
