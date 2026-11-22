using System.Net;
using System.Net.Http.Json;
using Financial.CashFlow.Application.DTOs;
using FluentAssertions;
using FluentAssertions.Execution;

namespace Financial.Api.Tests.Acceptance;

[Trait("Category", "Integration")]
public class SavingsAccountAcceptanceTests : ApiEndpointTests
{
    private const string Api = "/api/v1/financial";
    private const string BarclaysId = "5a71e600-0000-0000-0000-000000000001";
    private const string ChaseSaveId = "5a71e600-0000-0000-0000-000000000002";
    private const string InterestId = "5a71e600-0000-0000-0000-000000000011";
    private const string BonusId = "5a71e600-0000-0000-0000-000000000012";

    private const string SeededData = $$"""
        {
          "Accounts": [
            { "Id": "{{BarclaysId}}", "Name": "Barclays", "Template": "Bank", "Currency": "GBP", "IsActive": true, "OpeningBalance": { "Amount": 1000, "StartDate": "2026-01-01" }, "RoundUp": { "Enabled": false } },
            { "Id": "{{ChaseSaveId}}", "Name": "Chase Save", "Template": "Savings", "Currency": "GBP", "IsActive": true, "OpeningBalance": { "Amount": 500, "StartDate": "2026-09-01" }, "RoundUp": null, "Pots": null, "CategoryId": null, "Savings": { "Frequency": "Monthly", "PaymentDay": 5, "IsTaxFree": false, "Rates": [{ "AnnualPercent": 4.5, "EffectiveFrom": "2026-09-01" }, { "AnnualPercent": 3.75, "EffectiveFrom": "2027-01-01" }] } }
          ],
          "IncomeSources": [
            { "Id": "{{InterestId}}", "Name": "Interest", "IsActive": true, "Group": "DividendoJuros" },
            { "Id": "{{BonusId}}", "Name": "Bonus", "IsActive": true, "Group": "NonReportable" }
          ],
          "Version": 5
        }
        """;

    public SavingsAccountAcceptanceTests()
        : base(timeProvider: TestClock.At(TestClock.MidOctober))
    {
        ReplaceCashFlowDataBeforeStart(SeededData);
    }

    [Fact]
    [Trait("AC", "P64-F01-savings-template-01")]
    public async Task PostAccount_SavingsWithTerms_IsCreatedAndReadBackWithItsTerms()
    {
        var response = await Client.PostAsJsonAsync($"{Api}/accounts", NewSavings(SavingsTerms("Daily", paymentDay: null, isTaxFree: true)));
        var created = await response.Content.ReadFromJsonAsync<AccountDTO>();
        var listed = (await Client.GetFromJsonAsync<List<AccountDTO>>($"{Api}/accounts"))!.Single(account => account.Id == created!.Id);

        using (new AssertionScope())
        {
            response.StatusCode.Should().Be(HttpStatusCode.Created);
            listed.TemplateName.Should().Be("Savings");
            listed.OpeningBalance.Should().Be(250m);
            listed.Savings.Should().BeEquivalentTo(SavingsTerms("Daily", paymentDay: null, isTaxFree: true));
        }
    }

    [Theory]
    [Trait("AC", "P64-F01-savings-template-04")]
    [Trait("AC", "P64-F01-savings-template-05")]
    [InlineData("Weekly", 5, 4.5, "Choose how often interest is paid.")]
    [InlineData("Monthly", null, 4.5, "Interest payment day must be between 1 and 31.")]
    [InlineData("Daily", 5, 4.5, "Interest paid daily has no payment day.")]
    [InlineData("Monthly", 5, 100.5, "Interest rate must be between 0% and 100%.")]
    [InlineData("Monthly", 5, 4.125, "Interest rate can have at most 2 decimal places.")]
    public async Task PostAccount_SavingsWithInvalidTerms_Returns400WithTheFieldMessage(
        string frequency, int? paymentDay, double rate, string message)
    {
        var terms = SavingsTerms(frequency, paymentDay, isTaxFree: false, (decimal)rate);

        var response = await Client.PostAsJsonAsync($"{Api}/accounts", NewSavings(terms));

        using (new AssertionScope())
        {
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            (await response.Content.ReadAsStringAsync()).Should().Contain(message);
        }
    }

    [Fact]
    [Trait("AC", "P64-F01-savings-template-11")]
    public async Task PostAccount_BankWithSavingsTerms_Returns400()
    {
        var response = await Client.PostAsJsonAsync($"{Api}/accounts", new AccountCreateDTO
        {
            TemplateName = "Bank",
            Name = "Monzo",
            Currency = "GBP",
            OpeningBalance = 0m,
            BalanceStartDate = new DateOnly(2026, 10, 1),
            RoundUpEnabled = false,
            Savings = SavingsTerms("Monthly", 5, isTaxFree: false)
        });

        using (new AssertionScope())
        {
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            (await response.Content.ReadAsStringAsync()).Should().Contain("Bank accounts don't earn interest.");
        }
    }

    [Fact]
    [Trait("AC", "P64-F01-savings-template-07")]
    public async Task PutAccount_SavingsWithoutRates_Returns400AndKeepsTheRates()
    {
        var response = await PutChaseSaveAsync(new SavingsTermsDTO { InterestFrequency = "Monthly", PaymentDay = 5, IsTaxFree = false, Rates = [] });
        var account = (await Client.GetFromJsonAsync<List<AccountDTO>>($"{Api}/accounts"))!.Single(candidate => candidate.Id == Guid.Parse(ChaseSaveId));

        using (new AssertionScope())
        {
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            (await response.Content.ReadAsStringAsync()).Should().Contain("A savings account needs at least one rate.");
            account.Savings!.Rates.Should().HaveCount(2);
        }
    }

    [Fact]
    [Trait("AC", "P64-F01-savings-template-09")]
    public async Task PutAccount_SavingsWithTransactions_ChangesFrequencyAndTaxFreeAndKeepsTheBalance()
    {
        await PostTransferAsync(BarclaysId, ChaseSaveId, 100m);

        var response = await PutChaseSaveAsync(SavingsTerms("Daily", paymentDay: null, isTaxFree: true));
        var detail = await DetailAsync(ChaseSaveId);

        using (new AssertionScope())
        {
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            detail.Savings!.InterestFrequency.Should().Be("Daily");
            detail.Savings.PaymentDay.Should().BeNull();
            detail.Savings.IsTaxFree.Should().BeTrue();
            detail.Balance.Should().Be(600m);
        }
    }

    [Fact]
    [Trait("AC", "P64-F01-savings-template-06")]
    public async Task GetAccount_Savings_ShowsTheCurrentAndUpcomingRate()
    {
        var detail = await DetailAsync(ChaseSaveId);

        using (new AssertionScope())
        {
            detail.Savings!.InterestFrequency.Should().Be("Monthly");
            detail.Savings.PaymentDay.Should().Be(5);
            detail.Savings.IsTaxFree.Should().BeFalse();
            detail.Savings.CurrentRate.Should().BeEquivalentTo(new InterestRateDTO { AnnualPercent = 4.5m, EffectiveFrom = new DateOnly(2026, 9, 1) });
            detail.Savings.UpcomingRate.Should().BeEquivalentTo(new InterestRateDTO { AnnualPercent = 3.75m, EffectiveFrom = new DateOnly(2027, 1, 1) });
        }
    }

    [Fact]
    [Trait("AC", "P64-F01-savings-template-10")]
    public async Task Savings_TakesTransfersInAndOutOnTopOfItsOpeningBalance()
    {
        var transferIn = await PostTransferAsync(BarclaysId, ChaseSaveId, 300m);
        var transferOut = await PostTransferAsync(ChaseSaveId, BarclaysId, 120m);

        using (new AssertionScope())
        {
            transferIn.StatusCode.Should().Be(HttpStatusCode.OK);
            transferOut.StatusCode.Should().Be(HttpStatusCode.OK);
            (await DetailAsync(ChaseSaveId)).Balance.Should().Be(680m);
            (await DetailAsync(BarclaysId)).Balance.Should().Be(820m);
            (await DetailAsync(ChaseSaveId)).AllowedActions.Should().Contain(["AddIncome", "TransferTo", "TransferFrom", "Reconcile"]).And.NotContain(["AddExpense", "PayRecurringBill"]);
        }
    }

    [Fact]
    [Trait("AC", "P64-F02-interest-entries-01")]
    [Trait("AC", "P64-F02-interest-entries-04")]
    public async Task Income_FromADividendoJurosSource_RaisesTheBalanceAndIsMarkedAsInterest()
    {
        await PostIncomeAsync(InterestId, 2.10m);
        await PostIncomeAsync(BonusId, 10m);

        var rows = await Client.GetFromJsonAsync<List<AccountTransactionDTO>>($"{Api}/accounts/{ChaseSaveId}/transactions/2026/10");
        var incomes = await Client.GetFromJsonAsync<List<IncomeDTO>>($"{Api}/incomes/month/2026/10");

        using (new AssertionScope())
        {
            (await DetailAsync(ChaseSaveId)).Balance.Should().Be(512.10m);
            rows!.Select(row => (row.Counterpart, row.IsInterest)).Should().BeEquivalentTo([("Interest", true), ("Bonus", false)]);
            incomes!.Select(income => income.NetValue).Should().BeEquivalentTo([2.10m, 10m]);
        }
    }

    [Fact]
    [Trait("AC", "P64-F02-interest-entries-02")]
    public async Task Interest_CountsInTheMonthsTitheIncomeBase()
    {
        await PostIncomeAsync(InterestId, 2.10m);

        var tithe = await Client.GetFromJsonAsync<TitheSummaryDTO>($"{Api}/tithe/month/2026/10");

        tithe!.IncomeBase.Should().Be(2.10m);
    }

    [Fact]
    [Trait("AC", "P64-F02-interest-entries-03")]
    public async Task Interest_EditedAndDeletedThroughTheIncomeEndpoints_MovesTheBalance()
    {
        var interest = await PostIncomeAsync(InterestId, 2.10m);

        var edit = await Client.PutAsJsonAsync($"{Api}/incomes/{interest.Id}", new IncomeUpdateDTO
        {
            Date = new DateOnly(2026, 10, 5),
            IncomeSourceId = Guid.Parse(InterestId),
            NetValue = 3.40m,
            BankId = Guid.Parse(ChaseSaveId)
        });
        var balanceAfterEdit = (await DetailAsync(ChaseSaveId)).Balance;
        var delete = await Client.DeleteAsync($"{Api}/incomes/{interest.Id}");

        using (new AssertionScope())
        {
            edit.StatusCode.Should().Be(HttpStatusCode.OK);
            balanceAfterEdit.Should().Be(503.40m);
            delete.IsSuccessStatusCode.Should().BeTrue();
            (await DetailAsync(ChaseSaveId)).Balance.Should().Be(500m);
        }
    }

    [Fact]
    [Trait("AC", "P64-F03-savings-snapshot-01")]
    [Trait("AC", "P64-F03-savings-snapshot-02")]
    public async Task Suggestions_SavingsSourcedAccount_UseTheBalanceAtTheEndOfThePreviousMonth()
    {
        await PostTransferAsync(BarclaysId, ChaseSaveId, 300m);
        var investmentAccount = await Client.PostAsJsonAsync($"{Api}/investment-accounts", new InvestmentAccountCreateDTO
        {
            Name = "Chase savings",
            IsActive = true,
            IsLiability = false,
            Source = "SavingsAccountBalance",
            SavingsAccountId = Guid.Parse(ChaseSaveId)
        });

        var suggestions = await Client.GetFromJsonAsync<InvestmentSnapshotSuggestionsDTO>($"{Api}/investment-snapshots/2026/11/suggestions");

        using (new AssertionScope())
        {
            investmentAccount.StatusCode.Should().Be(HttpStatusCode.OK);
            var suggestion = suggestions!.Suggestions.Should().ContainSingle().Subject;
            suggestion.SuggestedValue.Should().Be(800m);
            suggestion.SourceDescription.Should().Be("Chase Save balance — as of Oct 2026");
        }
    }

    [Fact]
    [Trait("AC", "P64-F03-savings-snapshot-01")]
    public async Task PostInvestmentAccount_SavingsSourceWithABank_Returns400()
    {
        var response = await Client.PostAsJsonAsync($"{Api}/investment-accounts", new InvestmentAccountCreateDTO
        {
            Name = "Barclays savings",
            IsActive = true,
            IsLiability = false,
            Source = "SavingsAccountBalance",
            SavingsAccountId = Guid.Parse(BarclaysId)
        });

        using (new AssertionScope())
        {
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            (await response.Content.ReadAsStringAsync()).Should().Contain("Barclays is not a Savings account.");
        }
    }

    private static AccountCreateDTO NewSavings(SavingsTermsDTO terms) => new()
    {
        TemplateName = "Savings",
        Name = "Blue Rewards Saver",
        Currency = "GBP",
        OpeningBalance = 250m,
        BalanceStartDate = new DateOnly(2026, 10, 1),
        Savings = terms
    };

    private static SavingsTermsDTO SavingsTerms(string frequency, int? paymentDay, bool isTaxFree, decimal rate = 4.5m) => new()
    {
        InterestFrequency = frequency,
        PaymentDay = paymentDay,
        IsTaxFree = isTaxFree,
        Rates = [new InterestRateDTO { AnnualPercent = rate, EffectiveFrom = new DateOnly(2026, 9, 1) }]
    };

    private Task<HttpResponseMessage> PutChaseSaveAsync(SavingsTermsDTO terms) =>
        Client.PutAsJsonAsync($"{Api}/accounts/{ChaseSaveId}", new AccountUpdateDTO
        {
            TemplateName = "Savings",
            Name = "Chase Save",
            Currency = "GBP",
            OpeningBalance = 500m,
            BalanceStartDate = new DateOnly(2026, 9, 1),
            IsActive = true,
            Savings = terms
        });

    private Task<HttpResponseMessage> PostTransferAsync(string sourceId, string destinationId, decimal amount) =>
        Client.PostAsJsonAsync($"{Api}/accounts/transfers", new AccountTransferCreateDTO
        {
            Date = new DateOnly(2026, 10, 10),
            SourceAccountId = Guid.Parse(sourceId),
            DestinationAccountId = Guid.Parse(destinationId),
            Amount = amount,
            Note = "Move"
        });

    private async Task<IncomeDTO> PostIncomeAsync(string sourceId, decimal netValue)
    {
        var response = await Client.PostAsJsonAsync($"{Api}/incomes", new IncomeCreateDTO
        {
            Date = new DateOnly(2026, 10, 5),
            IncomeSourceId = Guid.Parse(sourceId),
            NetValue = netValue,
            BankId = Guid.Parse(ChaseSaveId)
        });
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        return (await response.Content.ReadFromJsonAsync<IncomeDTO>())!;
    }

    private async Task<AccountDetailDTO> DetailAsync(string id) =>
        (await Client.GetFromJsonAsync<AccountDetailDTO>($"{Api}/accounts/{id}/2026/10"))!;
}
