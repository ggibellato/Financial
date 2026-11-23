using System.Net;
using System.Net.Http.Json;
using Financial.CashFlow.Application.DTOs;
using FluentAssertions;
using FluentAssertions.Execution;

namespace Financial.Api.Tests.Acceptance;

[Trait("Category", "Integration")]
public class BankCashbackAcceptanceTests : ApiEndpointTests
{
    private const string Api = "/api/v1/financial";
    private const string ChaseId = "ca5b0000-0000-0000-0000-000000000001";
    private const string BarclaysId = "ca5b0000-0000-0000-0000-000000000002";
    private const string AmexId = "ca5b0000-0000-0000-0000-000000000003";
    private const string MaeId = "ca5b0000-0000-0000-0000-000000000004";
    private const string MercadoId = "ca5b0000-0000-0000-0000-000000000011";
    private const string SalaryId = "ca5b0000-0000-0000-0000-000000000021";

    private const string SeededData = $$"""
        {
          "Accounts": [
            { "Id": "{{ChaseId}}", "Name": "Chase", "Template": "Bank", "Currency": "GBP", "IsActive": true, "OpeningBalance": { "Amount": 100, "StartDate": "2026-01-01" }, "RoundUp": { "Enabled": true }, "Cashback": { "RatePercent": 2 } },
            { "Id": "{{BarclaysId}}", "Name": "Barclays", "Template": "Bank", "Currency": "GBP", "IsActive": true, "OpeningBalance": { "Amount": 1000, "StartDate": "2026-01-01" }, "RoundUp": { "Enabled": false } },
            { "Id": "{{AmexId}}", "Name": "Amex", "Template": "CreditCard", "Currency": "GBP", "IsActive": true, "OpeningBalance": null, "RoundUp": null },
            { "Id": "{{MaeId}}", "Name": "Mãe", "Template": "Settlement", "Currency": "GBP", "IsActive": true, "OpeningBalance": null, "RoundUp": null, "Currencies": ["GBP"], "OtherParticipant": "Mãe" }
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

    public BankCashbackAcceptanceTests()
        : base(timeProvider: TestClock.At(TestClock.MidOctober))
    {
        ReplaceCashFlowDataBeforeStart(SeededData);
    }

    [Fact]
    [Trait("AC", "P65-F01-cashback-setting-01")]
    public async Task PostAccount_BankWithCashback_IsCreatedAndReadBackWithItsRate()
    {
        var withCashback = await Client.PostAsJsonAsync($"{Api}/accounts", NewBank("Trading 212", cashbackEnabled: true, ratePercent: 1.5m));
        var withoutCashback = await Client.PostAsJsonAsync($"{Api}/accounts", NewBank("Monzo", cashbackEnabled: false, ratePercent: null));

        using (new AssertionScope())
        {
            withCashback.StatusCode.Should().Be(HttpStatusCode.Created);
            var trading212 = await AccountAsync((await withCashback.Content.ReadFromJsonAsync<AccountDTO>())!.Id);
            trading212.CashbackEnabled.Should().BeTrue();
            trading212.CashbackRatePercent.Should().Be(1.5m);
            var monzo = await AccountAsync((await withoutCashback.Content.ReadFromJsonAsync<AccountDTO>())!.Id);
            monzo.CashbackEnabled.Should().BeFalse();
            monzo.CashbackRatePercent.Should().BeNull();
        }
    }

    [Theory]
    [Trait("AC", "P65-F01-cashback-setting-02")]
    [InlineData(true, null, "Enter the cashback rate.")]
    [InlineData(true, 0.0, "Cashback rate must be more than 0% and at most 100%.")]
    [InlineData(true, 2.125, "Cashback rate can have at most 2 decimal places.")]
    [InlineData(false, 2.0, "A cashback rate can only be set while cashback is on.")]
    public async Task PostAccount_BankWithAnInvalidCashbackSetting_Returns400WithTheRuleMessage(bool enabled, double? ratePercent, string message)
    {
        var response = await Client.PostAsJsonAsync($"{Api}/accounts", NewBank("Monzo", enabled, (decimal?)ratePercent));

        using (new AssertionScope())
        {
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            (await response.Content.ReadAsStringAsync()).Should().Contain(message);
        }
    }

    [Fact]
    [Trait("AC", "P65-F01-cashback-setting-03")]
    public async Task PutAccount_SettlementWithCashback_Returns400()
    {
        var response = await Client.PutAsJsonAsync($"{Api}/accounts/{MaeId}", new AccountUpdateDTO
        {
            TemplateName = "Settlement",
            Name = "Mãe",
            Currency = "GBP",
            Currencies = ["GBP"],
            OtherParticipant = "Mãe",
            IsActive = true,
            CashbackEnabled = true,
            CashbackRatePercent = 2m
        });

        using (new AssertionScope())
        {
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            (await response.Content.ReadAsStringAsync()).Should().Contain("Mãe does not support cashback.");
        }
    }

    [Fact]
    [Trait("AC", "P65-F01-cashback-setting-04")]
    public async Task PutAccount_ChaseWithExpenses_ChangesTheRateThenSwitchesOffAndKeepsTheStoredCashback()
    {
        var expense = await PostExpenseAsync(ChaseId, 12.50m, cashback: 0.25m);

        var newRate = await PutChaseAsync(cashbackEnabled: true, ratePercent: 3m);
        var rateAfterChange = (await AccountAsync(Guid.Parse(ChaseId))).CashbackRatePercent;
        var switchedOff = await PutChaseAsync(cashbackEnabled: false, ratePercent: null);
        var stored = (await Client.GetFromJsonAsync<List<AccountTransactionDTO>>($"{Api}/accounts/{ChaseId}/transactions/2026/10"))!
            .Single(row => row.Id == expense.Id).Expense;

        using (new AssertionScope())
        {
            newRate.StatusCode.Should().Be(HttpStatusCode.OK);
            rateAfterChange.Should().Be(3m);
            switchedOff.StatusCode.Should().Be(HttpStatusCode.OK);
            (await AccountAsync(Guid.Parse(ChaseId))).CashbackEnabled.Should().BeFalse();
            stored!.CashbackAmount.Should().Be(0.25m);
            stored.SuggestedCashbackAmount.Should().BeNull();
        }
    }

    [Fact]
    public async Task PutAccount_WithoutCashbackFields_KeepsTheSetting()
    {
        var response = await PutChaseAsync(cashbackEnabled: null, ratePercent: null);

        using (new AssertionScope())
        {
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            (await AccountAsync(Guid.Parse(ChaseId))).CashbackRatePercent.Should().Be(2m);
        }
    }

    [Fact]
    [Trait("AC", "P65-F02-cashback-expenses-01")]
    public async Task PostExpense_FromACashbackBank_SavesTheCashbackAndSuggestsTheRate()
    {
        var expense = await PostExpenseAsync(ChaseId, 12.99m, cashback: 0.20m);

        using (new AssertionScope())
        {
            expense.CashbackAmount.Should().Be(0.20m);
            expense.SuggestedCashbackAmount.Should().Be(0.25m);
        }
    }

    [Theory]
    [Trait("AC", "P65-F02-cashback-expenses-02")]
    [InlineData(-0.01)]
    [InlineData(12.51)]
    [InlineData(0.255)]
    public async Task PostExpense_CashbackOutsideRange_Returns400(double cashback)
    {
        var response = await Client.PostAsJsonAsync($"{Api}/expenses", Expense(ChaseId, null, 12.50m, (decimal)cashback));

        using (new AssertionScope())
        {
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            (await response.Content.ReadAsStringAsync()).Should().Contain("Cashback must be between 0 and the expense value, with at most 2 decimal places.");
        }
    }

    [Theory]
    [Trait("AC", "P65-F02-cashback-expenses-03")]
    [InlineData(BarclaysId, null)]
    [InlineData(null, AmexId)]
    public async Task PostExpense_CashbackFromABankWithCashbackOffOrByCard_Returns400(string? bankId, string? cardId)
    {
        var response = await Client.PostAsJsonAsync($"{Api}/expenses", Expense(bankId, cardId, 12.50m, 0.25m));

        using (new AssertionScope())
        {
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            (await response.Content.ReadAsStringAsync()).Should().Contain("Cashback only applies to an expense paid from a bank with cashback on.");
        }
    }

    [Fact]
    [Trait("AC", "P65-F02-cashback-expenses-03")]
    public async Task PutExpense_UnchangedCashbackAfterCashbackIsSwitchedOff_Saves()
    {
        var expense = await PostExpenseAsync(ChaseId, 12.50m, cashback: 0.25m);
        await PutChaseAsync(cashbackEnabled: false, ratePercent: null);

        var response = await Client.PutAsJsonAsync($"{Api}/expenses/{expense.Id}", new ExpenseUpdateDTO
        {
            Date = new DateOnly(2026, 10, 3),
            Description = "Dinner",
            Value = 12.50m,
            CategoryId = Guid.Parse(MercadoId),
            PaymentSourceBankId = Guid.Parse(ChaseId),
            CashbackAmount = 0.25m
        });

        using (new AssertionScope())
        {
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            (await response.Content.ReadFromJsonAsync<ExpenseDTO>())!.CashbackAmount.Should().Be(0.25m);
        }
    }

    [Fact]
    [Trait("AC", "P65-F02-cashback-expenses-04")]
    public async Task Cashback_ChangesNoBalanceAndIsNotIncome()
    {
        await PostExpenseAsync(ChaseId, 12.50m, cashback: 0.25m);

        var tithe = await Client.GetFromJsonAsync<TitheSummaryDTO>($"{Api}/tithe/month/2026/10");
        var incomes = await Client.GetFromJsonAsync<List<IncomeDTO>>($"{Api}/incomes/month/2026/10");

        using (new AssertionScope())
        {
            (await DetailAsync(ChaseId)).Balance.Should().Be(87.50m);
            tithe!.IncomeBase.Should().Be(0m);
            incomes.Should().BeEmpty();
        }
    }

    [Fact]
    [Trait("AC", "P65-F02-cashback-expenses-05")]
    [Trait("AC", "P65-F02-cashback-expenses-06")]
    public async Task TransactionsAndAccountsTab_ShowTheCashbackNextToTheRoundUp()
    {
        await PostExpenseAsync(ChaseId, 12.50m, cashback: 0.25m, roundUp: 0.50m);
        await PostExpenseAsync(ChaseId, 4.80m, cashback: 0.09m, roundUp: 0.20m);
        await PostExpenseAsync(BarclaysId, 20m, cashback: null);

        var rows = await Client.GetFromJsonAsync<List<AccountTransactionDTO>>($"{Api}/accounts/{ChaseId}/transactions/2026/10");
        var summary = await Client.GetFromJsonAsync<AccountGroupSummaryDTO>($"{Api}/accounts/groups/default/summary/2026/10");

        using (new AssertionScope())
        {
            rows!.Select(row => (row.Cashback, row.RoundUp)).Should().BeEquivalentTo([(0.25m, -0.50m), (0.09m, -0.20m)]);
            var chase = summary!.Members.Single(member => member.Name == "Chase");
            chase.CashbackTotal.Should().Be(0.34m);
            chase.RoundUpTotal.Should().Be(0.70m);
            summary.Members.Single(member => member.Name == "Barclays").CashbackTotal.Should().Be(0m);
            summary.Members.Single(member => member.Name == "Amex").CashbackTotal.Should().BeNull();
            summary.Totals.Single(total => total.Currency == "GBP").CashbackTotal.Should().Be(0.34m);
        }
    }

    [Fact]
    [Trait("AC", "P65-CF-cross-feature-01")]
    public async Task NewBankWithTwoPercentCashback_RecordsTheSuggestedCashbackWithoutTouchingBalanceOrIncome()
    {
        var created = await Client.PostAsJsonAsync($"{Api}/accounts", NewBank("Trading 212", cashbackEnabled: true, ratePercent: 2m));
        var bankId = (await created.Content.ReadFromJsonAsync<AccountDTO>())!.Id.ToString();
        var suggested = (await PostExpenseAsync(bankId, 12.50m, cashback: null)).SuggestedCashbackAmount;
        await PostExpenseAsync(bankId, 12.50m, cashback: suggested);

        var summary = await Client.GetFromJsonAsync<AccountGroupSummaryDTO>($"{Api}/accounts/groups/default/summary/2026/10");

        using (new AssertionScope())
        {
            suggested.Should().Be(0.25m);
            summary!.Members.Single(member => member.Name == "Trading 212").CashbackTotal.Should().Be(0.25m);
            (await DetailAsync(bankId)).Balance.Should().Be(-25m);
            summary.TotalIncoming.Should().Be(0m);
        }
    }

    private static AccountCreateDTO NewBank(string name, bool cashbackEnabled, decimal? ratePercent) => new()
    {
        TemplateName = "Bank",
        Name = name,
        Currency = "GBP",
        OpeningBalance = 0m,
        BalanceStartDate = new DateOnly(2026, 10, 1),
        RoundUpEnabled = false,
        CashbackEnabled = cashbackEnabled,
        CashbackRatePercent = ratePercent
    };

    private static ExpenseCreateDTO Expense(string? bankId, string? cardId, decimal value, decimal? cashback, decimal? roundUp = null) => new()
    {
        Date = new DateOnly(2026, 10, 3),
        Description = "Lunch",
        Value = value,
        CategoryId = Guid.Parse(MercadoId),
        PaymentSourceBankId = bankId is null ? null : Guid.Parse(bankId),
        CreditCardId = cardId is null ? null : Guid.Parse(cardId),
        RoundUpAmount = roundUp,
        CashbackAmount = cashback
    };

    private async Task<ExpenseDTO> PostExpenseAsync(string bankId, decimal value, decimal? cashback, decimal? roundUp = null)
    {
        var response = await Client.PostAsJsonAsync($"{Api}/expenses", Expense(bankId, null, value, cashback, roundUp));
        response.IsSuccessStatusCode.Should().BeTrue(await response.Content.ReadAsStringAsync());
        return (await response.Content.ReadFromJsonAsync<ExpenseDTO>())!;
    }

    private Task<HttpResponseMessage> PutChaseAsync(bool? cashbackEnabled, decimal? ratePercent) =>
        Client.PutAsJsonAsync($"{Api}/accounts/{ChaseId}", new AccountUpdateDTO
        {
            TemplateName = "Bank",
            Name = "Chase",
            Currency = "GBP",
            OpeningBalance = 100m,
            BalanceStartDate = new DateOnly(2026, 1, 1),
            RoundUpEnabled = true,
            IsActive = true,
            CashbackEnabled = cashbackEnabled,
            CashbackRatePercent = ratePercent
        });

    private async Task<AccountDTO> AccountAsync(Guid id) =>
        (await Client.GetFromJsonAsync<List<AccountDTO>>($"{Api}/accounts"))!.Single(account => account.Id == id);

    private async Task<AccountDetailDTO> DetailAsync(string id) =>
        (await Client.GetFromJsonAsync<AccountDetailDTO>($"{Api}/accounts/{id}/2026/10"))!;
}
