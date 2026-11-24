using System.Net;
using System.Net.Http.Json;
using Financial.CashFlow.Application.DTOs;
using Financial.CashFlow.Domain.Entities;
using FluentAssertions;
using FluentAssertions.Execution;

namespace Financial.Api.Tests.Acceptance;

[Trait("Category", "Integration")]
public class RoundUpDestinationsAcceptanceTests : ApiEndpointTests
{
    private const string Api = "/api/v1/financial";
    private const string ChaseId = "40c6d000-0000-0000-0000-000000000001";
    private const string BarclaysId = "40c6d000-0000-0000-0000-000000000002";
    private const string SaverId = "40c6d000-0000-0000-0000-000000000003";
    private const string CashbackSaverId = "40c6d000-0000-0000-0000-000000000004";
    private const string ChaseSaveId = "40c6d000-0000-0000-0000-000000000005";
    private const string MercadoId = "40c6d000-0000-0000-0000-000000000011";
    private const string InterestId = "40c6d000-0000-0000-0000-000000000021";
    private const string BonusId = "40c6d000-0000-0000-0000-000000000022";

    private const string RoundUpOnlyTerms =
        """{ "Frequency": "Monthly", "PaymentDay": 1, "IsTaxFree": false, "Rates": [{ "AnnualPercent": 5, "EffectiveFrom": "2026-01-01" }], "RoundUpOnly": true }""";

    private const string OrdinaryTerms =
        """{ "Frequency": "Monthly", "PaymentDay": 1, "IsTaxFree": false, "Rates": [{ "AnnualPercent": 4.5, "EffectiveFrom": "2026-01-01" }] }""";

    private const string SeededData = $$"""
        {
          "Accounts": [
            { "Id": "{{ChaseId}}", "Name": "Chase", "Template": "Bank", "Currency": "GBP", "IsActive": true, "OpeningBalance": { "Amount": 100, "StartDate": "2026-01-01" }, "RoundUp": { "Enabled": true }, "Cashback": { "RatePercent": 2 } },
            { "Id": "{{BarclaysId}}", "Name": "Barclays", "Template": "Bank", "Currency": "GBP", "IsActive": true, "OpeningBalance": { "Amount": 1000, "StartDate": "2026-01-01" }, "RoundUp": { "Enabled": false } },
            { "Id": "{{SaverId}}", "Name": "Round-up saver", "Template": "Savings", "Currency": "GBP", "IsActive": true, "OpeningBalance": { "Amount": 10, "StartDate": "2026-10-01" }, "RoundUp": null, "Pots": null, "CategoryId": null, "Savings": {{RoundUpOnlyTerms}} },
            { "Id": "{{CashbackSaverId}}", "Name": "Chase cashback", "Template": "Savings", "Currency": "GBP", "IsActive": true, "OpeningBalance": { "Amount": 0, "StartDate": "2026-10-01" }, "RoundUp": null, "Pots": null, "CategoryId": null, "Savings": {{OrdinaryTerms}} },
            { "Id": "{{ChaseSaveId}}", "Name": "Chase Save", "Template": "Savings", "Currency": "GBP", "IsActive": true, "OpeningBalance": { "Amount": 0, "StartDate": "2026-01-01" }, "RoundUp": null, "Pots": null, "CategoryId": null, "Savings": {{OrdinaryTerms}} }
          ],
          "Categories": [
            { "Id": "{{MercadoId}}", "Name": "Mercado", "Active": true, "IsInvestment": false, "IsTithe": false }
          ],
          "IncomeSources": [
            { "Id": "{{InterestId}}", "Name": "Interest", "IsActive": true, "Group": "DividendoJuros" },
            { "Id": "{{BonusId}}", "Name": "Bonus", "IsActive": true, "Group": "NonReportable" }
          ],
          "Version": 5
        }
        """;

    public RoundUpDestinationsAcceptanceTests()
        : base(timeProvider: TestClock.At(TestClock.MidOctober))
    {
        ReplaceCashFlowDataBeforeStart(SeededData);
    }

    [Fact]
    [Trait("AC", "P66-F01-roundup-only-01")]
    public async Task Savings_RoundUpOnly_IsReturnedAndCanBeSwitchedOff()
    {
        var before = await AccountAsync(SaverId);
        var detailBefore = await DetailAsync(SaverId);
        var response = await PutSavingsAsync(SaverId, "Round-up saver", roundUpOnly: false);
        var after = await AccountAsync(SaverId);

        using (new AssertionScope())
        {
            before.Savings!.RoundUpOnly.Should().BeTrue();
            detailBefore.Savings!.RoundUpOnly.Should().BeTrue();
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            after.Savings!.RoundUpOnly.Should().BeFalse();
            (await AccountAsync(ChaseSaveId)).Savings!.RoundUpOnly.Should().BeFalse();
        }
    }

    [Fact]
    [Trait("AC", "P66-F01-roundup-only-02")]
    public async Task Transfer_IntoARoundUpOnlyAccount_Returns400()
    {
        var response = await PostTransferAsync(BarclaysId, SaverId, 50m);

        await ShouldBeProblemAsync(response, HttpStatusCode.BadRequest, "Round-up saver only receives round-ups.");
    }

    [Fact]
    [Trait("AC", "P66-F01-roundup-only-03")]
    public async Task RoundUpOnly_RefusesOtherIncomeAndAllowsInterestTransfersOutAndAdjustments()
    {
        var bonus = await PostIncomeAsync(BonusId, SaverId, 10m);
        var interest = await PostIncomeAsync(InterestId, SaverId, 1.20m);
        var transferOut = await PostTransferAsync(SaverId, ChaseSaveId, 5m);

        using (new AssertionScope())
        {
            await ShouldBeProblemAsync(bonus, HttpStatusCode.BadRequest, "Round-up saver only receives round-ups and its interest.");
            interest.StatusCode.Should().Be(HttpStatusCode.OK);
            transferOut.StatusCode.Should().Be(HttpStatusCode.OK);
            (await DetailAsync(SaverId)).Balance.Should().Be(6.20m);
        }
    }

    [Fact]
    [Trait("AC", "P66-F01-roundup-only-04")]
    public async Task RoundUpOnly_AllowedActions_ExcludeTransferFromAndAddIncome()
    {
        var detail = await DetailAsync(SaverId);

        detail.AllowedActions.Should().Contain(["AddInterest", "TransferTo", "Reconcile"]).And.NotContain(["TransferFrom", "AddIncome", "ReceiveCardCredit"]);
    }

    [Fact]
    [Trait("AC", "P66-F01-roundup-only-05")]
    public async Task SwitchingRoundUpOnlyOn_WithATransferIn_Returns400()
    {
        await PostTransferAsync(BarclaysId, ChaseSaveId, 50m);

        var response = await PutSavingsAsync(ChaseSaveId, "Chase Save", roundUpOnly: true);

        await ShouldBeProblemAsync(response, HttpStatusCode.BadRequest, "Chase Save already has money in that isn't a round-up or interest.");
    }

    [Fact]
    [Trait("AC", "P66-F02-destinations-01")]
    public async Task Destinations_AreSavedReturnedAndKeptByAnUpdateWithoutThem()
    {
        var before = await AccountAsync(BarclaysId);
        var set = await PutChaseAsync(Destination("Account", SaverId), Destination("InvestedAutomatically"));
        var kept = await PutChaseAsync(roundUp: null, cashback: null);
        var chase = await AccountAsync(ChaseId);

        using (new AssertionScope())
        {
            before.RoundUpDestination!.Kind.Should().Be("None");
            set.StatusCode.Should().Be(HttpStatusCode.OK);
            kept.StatusCode.Should().Be(HttpStatusCode.OK);
            chase.RoundUpDestination.Should().BeEquivalentTo(new MoneyDestinationDTO { Kind = "Account", AccountId = Guid.Parse(SaverId), AccountName = "Round-up saver" });
            chase.CashbackDestination!.Kind.Should().Be("InvestedAutomatically");
            (await DetailAsync(ChaseId)).RoundUpDestination!.AccountName.Should().Be("Round-up saver");
        }
    }

    [Fact]
    [Trait("AC", "P66-F03-destinations-web-02")]
    public async Task AccountDetail_LeavesOutTheDestinationOfASwitchedOffRoundUp()
    {
        await Client.PutAsJsonAsync($"{Api}/accounts/{BarclaysId}", new AccountUpdateDTO
        {
            TemplateName = "Bank",
            Name = "Barclays",
            Currency = "GBP",
            OpeningBalance = 1000m,
            BalanceStartDate = new DateOnly(2026, 1, 1),
            RoundUpEnabled = false,
            IsActive = true,
            RoundUpDestination = Destination("InvestedAutomatically")
        });

        using (new AssertionScope())
        {
            (await AccountAsync(BarclaysId)).RoundUpDestination!.Kind.Should().Be("InvestedAutomatically");
            (await DetailAsync(BarclaysId)).RoundUpDestination.Should().BeNull();
        }
    }

    [Theory]
    [Trait("AC", "P66-F02-destinations-02")]
    [InlineData(ChaseSaveId, null)]
    [InlineData(null, SaverId)]
    public async Task Destinations_ToTheWrongKindOfAccount_Return400(string? roundUpTarget, string? cashbackTarget)
    {
        var response = await PutChaseAsync(
            roundUpTarget is null ? null : Destination("Account", roundUpTarget),
            cashbackTarget is null ? null : Destination("Account", cashbackTarget));

        await ShouldBeProblemAsync(
            response,
            HttpStatusCode.BadRequest,
            roundUpTarget is null ? Account.CashbackDestinationMessage : Account.RoundUpDestinationMessage);
    }

    [Fact]
    [Trait("AC", "P66-F02-destinations-03")]
    [Trait("AC", "P66-F02-destinations-04")]
    public async Task RoutedRoundUpsAndCashback_ReachTheirDestinationsAndShowAsReadOnlyRows()
    {
        await PutChaseAsync(Destination("Account", SaverId), Destination("Account", CashbackSaverId));
        await PostExpenseAsync(new DateOnly(2026, 9, 28), 4.80m, roundUp: 0.20m, cashback: 0.09m);
        await PostExpenseAsync(new DateOnly(2026, 10, 3), 12.40m, roundUp: 0.60m, cashback: 0.24m);

        var saverRows = await RowsAsync(SaverId);
        var cashbackRows = await RowsAsync(CashbackSaverId);

        using (new AssertionScope())
        {
            (await DetailAsync(SaverId)).Balance.Should().Be(10.60m);
            (await DetailAsync(CashbackSaverId)).Balance.Should().Be(0.24m);
            (await DetailAsync(ChaseId)).Balance.Should().Be(82.00m);
            saverRows.Should().ContainSingle().Which.Should().BeEquivalentTo(
                new { Kind = "RoundUp", Date = new DateOnly(2026, 10, 3), Description = "Lunch", Counterpart = "Chase", Amount = 0.60m, CanEdit = false });
            cashbackRows.Should().ContainSingle().Which.Should().BeEquivalentTo(new { Kind = "Cashback", Amount = 0.24m, CanEdit = false });
        }
    }

    [Fact]
    [Trait("AC", "P66-F02-destinations-04")]
    public async Task RoutedRows_StayOffTheMonthWideListAndBeforeTheDestinationsStartDate()
    {
        await PutChaseAsync(Destination("Account", SaverId), null);
        await PostExpenseAsync(new DateOnly(2026, 9, 28), 4.80m, roundUp: 0.20m, cashback: null);
        await PostExpenseAsync(new DateOnly(2026, 10, 3), 12.40m, roundUp: 0.60m, cashback: null);

        var monthRows = await Client.GetFromJsonAsync<List<MonthTransactionDTO>>($"{Api}/accounts/transactions/2026/10");
        var septemberRows = await Client.GetFromJsonAsync<List<AccountTransactionDTO>>($"{Api}/accounts/{SaverId}/transactions/2026/9");

        using (new AssertionScope())
        {
            monthRows!.Select(row => row.Transaction.Kind).Should().NotContain(["RoundUp", "Cashback"]);
            septemberRows.Should().BeEmpty();
        }
    }

    [Theory]
    [Trait("AC", "P66-F02-destinations-02")]
    [InlineData("9")]
    [InlineData("1")]
    [InlineData("Somewhere")]
    public async Task Destinations_WithAnUndefinedKind_Return400(string kind)
    {
        var response = await PutChaseAsync(new MoneyDestinationDTO { Kind = kind, AccountId = Guid.Parse(SaverId) }, null);

        await ShouldBeProblemAsync(response, HttpStatusCode.BadRequest, "Choose where the money goes.");
    }

    [Fact]
    [Trait("AC", "P66-F02-destinations-05")]
    public async Task SettingTheDestinationToNone_RemovesTheRoutedAmounts()
    {
        await PutChaseAsync(Destination("Account", SaverId), null);
        await PostExpenseAsync(new DateOnly(2026, 10, 3), 12.40m, roundUp: 0.60m, cashback: null);

        await PutChaseAsync(Destination("None"), null);

        using (new AssertionScope())
        {
            (await DetailAsync(SaverId)).Balance.Should().Be(10m);
            (await RowsAsync(SaverId)).Should().BeEmpty();
        }
    }

    [Fact]
    [Trait("AC", "P66-F02-destinations-06")]
    public async Task DeactivatingOrDeletingADestination_Returns400NamingTheBank()
    {
        await PutChaseAsync(Destination("Account", SaverId), Destination("Account", CashbackSaverId));

        var deactivate = await PutSavingsAsync(SaverId, "Round-up saver", roundUpOnly: true, isActive: false);
        var delete = await Client.DeleteAsync($"{Api}/accounts/{CashbackSaverId}");

        using (new AssertionScope())
        {
            await ShouldBeProblemAsync(deactivate, HttpStatusCode.BadRequest, "Round-up saver receives Chase's round-ups.");
            await ShouldBeProblemAsync(delete, HttpStatusCode.BadRequest, "Chase cashback receives Chase's cashback.");
        }
    }

    [Fact]
    [Trait("AC", "P66-F02-destinations-07")]
    public async Task SnapshotSuggestion_ForADestinationSavingsAccount_IncludesTheRoutedAmounts()
    {
        await PutChaseAsync(Destination("Account", SaverId), null);
        await PostExpenseAsync(new DateOnly(2026, 10, 3), 12.40m, roundUp: 0.60m, cashback: null);
        await Client.PostAsJsonAsync($"{Api}/investment-accounts", new InvestmentAccountCreateDTO
        {
            Name = "Chase round-ups",
            IsActive = true,
            IsLiability = false,
            Source = "SavingsAccountBalance",
            SavingsAccountId = Guid.Parse(SaverId)
        });

        var suggestions = await Client.GetFromJsonAsync<InvestmentSnapshotSuggestionsDTO>($"{Api}/investment-snapshots/2026/11/suggestions");

        suggestions!.Suggestions.Should().ContainSingle().Which.SuggestedValue.Should().Be(10.60m);
    }

    [Fact]
    [Trait("AC", "P66-CF-cross-feature-01")]
    public async Task RoundUpSaver_GrowsWithRoundUpsTakesInterestRefusesATransferInAndSendsItsBalanceOut()
    {
        await PutChaseAsync(Destination("Account", SaverId), null);
        await PostExpenseAsync(new DateOnly(2026, 10, 3), 12.40m, roundUp: 0.60m, cashback: null);
        await PostExpenseAsync(new DateOnly(2026, 10, 4), 3.10m, roundUp: 0.90m, cashback: null);
        var interest = await PostIncomeAsync(InterestId, SaverId, 0.50m);
        var transferIn = await PostTransferAsync(BarclaysId, SaverId, 5m);
        var moveOut = await PostTransferAsync(SaverId, ChaseSaveId, 12m);

        using (new AssertionScope())
        {
            interest.StatusCode.Should().Be(HttpStatusCode.OK);
            transferIn.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            moveOut.StatusCode.Should().Be(HttpStatusCode.OK);
            (await DetailAsync(SaverId)).Balance.Should().Be(0m);
            (await DetailAsync(ChaseSaveId)).Balance.Should().Be(12m);
        }
    }

    [Fact]
    [Trait("AC", "P66-CF-cross-feature-02")]
    public async Task CashbackToSavingsAndRoundUpsInvested_LeaveTheRoundUpsOutOfEveryBalance()
    {
        await PutChaseAsync(Destination("InvestedAutomatically"), Destination("Account", CashbackSaverId));
        await PostExpenseAsync(new DateOnly(2026, 10, 3), 12.40m, roundUp: 0.60m, cashback: 0.24m);

        using (new AssertionScope())
        {
            (await DetailAsync(CashbackSaverId)).Balance.Should().Be(0.24m);
            (await DetailAsync(SaverId)).Balance.Should().Be(10m);
            (await DetailAsync(ChaseId)).Balance.Should().Be(87.00m);
        }
    }

    private static MoneyDestinationDTO Destination(string kind, string? accountId = null) =>
        new() { Kind = kind, AccountId = accountId is null ? null : Guid.Parse(accountId) };

    private Task<HttpResponseMessage> PutChaseAsync(MoneyDestinationDTO? roundUp, MoneyDestinationDTO? cashback) =>
        Client.PutAsJsonAsync($"{Api}/accounts/{ChaseId}", new AccountUpdateDTO
        {
            TemplateName = "Bank",
            Name = "Chase",
            Currency = "GBP",
            OpeningBalance = 100m,
            BalanceStartDate = new DateOnly(2026, 1, 1),
            RoundUpEnabled = true,
            IsActive = true,
            RoundUpDestination = roundUp,
            CashbackDestination = cashback
        });

    private Task<HttpResponseMessage> PutSavingsAsync(string id, string name, bool roundUpOnly, bool isActive = true) =>
        Client.PutAsJsonAsync($"{Api}/accounts/{id}", new AccountUpdateDTO
        {
            TemplateName = "Savings",
            Name = name,
            Currency = "GBP",
            OpeningBalance = id == SaverId ? 10m : 0m,
            BalanceStartDate = id == ChaseSaveId ? new DateOnly(2026, 1, 1) : new DateOnly(2026, 10, 1),
            IsActive = isActive,
            Savings = new SavingsTermsDTO
            {
                InterestFrequency = "Monthly",
                PaymentDay = 1,
                IsTaxFree = false,
                Rates = [new InterestRateDTO { AnnualPercent = 5m, EffectiveFrom = new DateOnly(2026, 1, 1) }],
                RoundUpOnly = roundUpOnly
            }
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

    private Task<HttpResponseMessage> PostIncomeAsync(string sourceId, string accountId, decimal netValue) =>
        Client.PostAsJsonAsync($"{Api}/incomes", new IncomeCreateDTO
        {
            Date = new DateOnly(2026, 10, 5),
            IncomeSourceId = Guid.Parse(sourceId),
            NetValue = netValue,
            BankId = Guid.Parse(accountId)
        });

    private async Task PostExpenseAsync(DateOnly date, decimal value, decimal? roundUp, decimal? cashback)
    {
        var response = await Client.PostAsJsonAsync($"{Api}/expenses", new ExpenseCreateDTO
        {
            Date = date,
            Description = "Lunch",
            Value = value,
            CategoryId = Guid.Parse(MercadoId),
            PaymentSourceBankId = Guid.Parse(ChaseId),
            RoundUpAmount = roundUp,
            CashbackAmount = cashback
        });
        response.IsSuccessStatusCode.Should().BeTrue(await response.Content.ReadAsStringAsync());
    }

    private async Task<List<AccountTransactionDTO>> RowsAsync(string id) =>
        (await Client.GetFromJsonAsync<List<AccountTransactionDTO>>($"{Api}/accounts/{id}/transactions/2026/10"))!;

    private async Task<AccountDTO> AccountAsync(string id) =>
        (await Client.GetFromJsonAsync<List<AccountDTO>>($"{Api}/accounts"))!.Single(account => account.Id == Guid.Parse(id));

    private async Task<AccountDetailDTO> DetailAsync(string id) =>
        (await Client.GetFromJsonAsync<AccountDetailDTO>($"{Api}/accounts/{id}/2026/10"))!;
}
