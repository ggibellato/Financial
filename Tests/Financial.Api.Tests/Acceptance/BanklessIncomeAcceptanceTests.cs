using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using Financial.CashFlow.Application.DTOs;
using FluentAssertions;
using FluentAssertions.Execution;

namespace Financial.Api.Tests.Acceptance;

[Trait("Category", "Integration")]
public class BanklessIncomeAcceptanceTests : ApiEndpointTests
{
    private const string BarclaysId = "f2a00000-0000-0000-0000-000000000001";
    private const string ReservaId = "f2a00000-0000-0000-0000-000000000002";
    private const string InvestimentoId = "f2a00000-0000-0000-0000-000000000101";
    private const string MercadoId = "f2a00000-0000-0000-0000-000000000201";
    private const string ReservaCategoryId = "f2a00000-0000-0000-0000-000000000202";
    private const string SalaryId = "f2a00000-0000-0000-0000-000000000301";
    private const string DividendsId = "f2a00000-0000-0000-0000-000000000302";
    private const string RamsayId = "f2a00000-0000-0000-0000-000000000303";
    private const string BanklessIncomeId = "f2a00000-0000-0000-0000-000000000801";
    private static readonly Guid Barclays = Guid.Parse(BarclaysId);
    private const string MonthPath = "/accounts/transactions/2026/10";

    private const string SeededData = $$"""
        {
          "Accounts": [
            { "Id": "{{BarclaysId}}", "Name": "Barclays", "Template": "Bank", "Currency": "GBP", "IsActive": true, "OpeningBalance": { "Amount": 1000, "StartDate": "2026-01-01" }, "RoundUp": { "Enabled": false }, "Pots": null },
            { "Id": "{{ReservaId}}", "Name": "Reserva", "Template": "Pots", "Currency": "GBP", "IsActive": true, "OpeningBalance": null, "RoundUp": null, "Pots": [
              { "Id": "{{InvestimentoId}}", "Name": "Investimento", "IsActive": true, "SplitPercentage": 100 }
            ], "CategoryId": "{{ReservaCategoryId}}" }
          ],
          "Categories": [
            { "Id": "{{MercadoId}}", "Name": "Mercado", "Active": true, "IsInvestment": false, "IsTithe": false },
            { "Id": "{{ReservaCategoryId}}", "Name": "Reserva", "Active": true, "IsInvestment": false, "IsTithe": false }
          ],
          "IncomeSources": [
            { "Id": "{{SalaryId}}", "Name": "Salary", "IsActive": true, "Group": "Salary" },
            { "Id": "{{DividendsId}}", "Name": "DividendoJuros", "IsActive": true, "Group": "DividendoJuros" },
            { "Id": "{{RamsayId}}", "Name": "Ramsay", "IsActive": true, "Group": "Salary", "PotsAccountId": "{{ReservaId}}" }
          ],
          "Expenses": [
            { "Id": "f2a00000-0000-0000-0000-000000000401", "Date": "2026-10-10", "Description": "Tesco", "Value": 42.10, "CategoryId": "{{MercadoId}}", "PaymentSourceAccountId": "{{BarclaysId}}", "CreditCardId": null, "ChargeDate": null, "InvoiceDate": null, "RoundUpAmount": null, "CountsAsTithe": true }
          ],
          "Incomes": [
            { "Id": "f2a00000-0000-0000-0000-000000000800", "Date": "2026-10-01", "IncomeSourceId": "{{SalaryId}}", "GrossValue": null, "NetValue": 2000, "AccountId": "{{BarclaysId}}", "Description": "October pay" },
            { "Id": "{{BanklessIncomeId}}", "Date": "2026-10-15", "IncomeSourceId": "{{DividendsId}}", "GrossValue": null, "NetValue": 85.20, "AccountId": null, "Description": "XP dividends" }
          ],
          "Version": 5
        }
        """;

    public BanklessIncomeAcceptanceTests()
        : base(timeProvider: TestClock.At(new DateTimeOffset(2026, 11, 15, 12, 0, 0, TimeSpan.Zero)))
    {
        ReplaceCashFlowDataBeforeStart(SeededData);
    }

    private sealed record MonthFigures(decimal BarclaysBalance, decimal TotalIncoming, decimal CalculatedTithe);

    private async Task<MonthFigures> MonthFiguresAsync()
    {
        var barclays = await Client.GetFromJsonAsync<AccountDetailDTO>($"{Api}/accounts/{BarclaysId}/2026/10");
        var summary = await Client.GetFromJsonAsync<AccountGroupSummaryDTO>($"{Api}/accounts/groups/default/summary/2026/10");
        var tithe = await Client.GetFromJsonAsync<TitheSummaryDTO>($"{Api}/tithe/month/2026/10");
        return new MonthFigures(barclays!.Balance, summary!.TotalIncoming, tithe!.CalculatedTithe);
    }

    private static IncomeCreateDTO NewIncome(Guid? bankId) => new()
    {
        Date = new DateOnly(2026, 10, 30),
        IncomeSourceId = Guid.Parse(DividendsId),
        NetValue = 42.10m,
        BankId = bankId,
        Description = "Interest"
    };

    private static IncomeUpdateDTO Edit(IncomeDTO income, Guid? bankId) => new()
    {
        Date = income.Date,
        IncomeSourceId = income.IncomeSourceId,
        NetValue = income.NetValue,
        BankId = bankId,
        Description = income.Description
    };

    private async Task<IncomeDTO> CreateAsync(IncomeCreateDTO request)
    {
        var response = await Client.PostAsJsonAsync($"{Api}/incomes", request);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        return (await response.Content.ReadFromJsonAsync<IncomeDTO>())!;
    }

    private JsonNode SavedIncome(Guid id) =>
        JsonNode.Parse(SavedCashFlowData())!["Incomes"]!.AsArray().Single(income => income!["Id"]!.GetValue<Guid>() == id)!;

    [Fact]
    [Trait("AC", "P61-F02-income-tab-and-bankless-income-02")]
    [Trait("AC", "P61-F02-income-tab-and-bankless-income-10")]
    public async Task CreateIncome_NoBank_CountsInTotalsAndTitheAndMovesNoBalance()
    {
        var before = await MonthFiguresAsync();

        var income = await CreateAsync(NewIncome(bankId: null));
        var after = await MonthFiguresAsync();
        var saved = SavedIncome(income.Id).AsObject();

        using (new AssertionScope())
        {
            income.BankId.Should().BeNull();
            after.BarclaysBalance.Should().Be(before.BarclaysBalance);
            after.TotalIncoming.Should().Be(before.TotalIncoming + 42.10m);
            after.CalculatedTithe.Should().Be(before.CalculatedTithe + 4.21m);
            saved.ContainsKey("AccountId").Should().BeTrue();
            saved["AccountId"].Should().BeNull();
        }
    }

    [Fact]
    [Trait("AC", "P61-F02-income-tab-and-bankless-income-02")]
    public async Task CreateIncome_WithBank_MovesItsBalance()
    {
        var before = await MonthFiguresAsync();

        await CreateAsync(NewIncome(Barclays));
        var after = await MonthFiguresAsync();

        after.BarclaysBalance.Should().Be(before.BarclaysBalance + 42.10m);
    }

    [Fact]
    [Trait("AC", "P61-F02-income-tab-and-bankless-income-04")]
    public async Task UpdateIncome_BankToNoBankAndBack_MovesTheBalanceOnlyWhileBanked()
    {
        var income = await CreateAsync(NewIncome(Barclays));
        var banked = await MonthFiguresAsync();

        (await Client.PutAsJsonAsync($"{Api}/incomes/{income.Id}", Edit(income, bankId: null))).StatusCode.Should().Be(HttpStatusCode.OK);
        var bankless = await MonthFiguresAsync();
        (await Client.PutAsJsonAsync($"{Api}/incomes/{income.Id}", Edit(income, Barclays))).StatusCode.Should().Be(HttpStatusCode.OK);
        var rebanked = await MonthFiguresAsync();

        using (new AssertionScope())
        {
            bankless.BarclaysBalance.Should().Be(banked.BarclaysBalance - 42.10m);
            bankless.TotalIncoming.Should().Be(banked.TotalIncoming);
            rebanked.Should().Be(banked);
        }
    }

    [Fact]
    [Trait("AC", "P61-F02-income-tab-and-bankless-income-05")]
    public async Task CreateIncome_NoBankWithPotsSplit_Returns400()
    {
        var incomesBefore = SavedCashFlowData();

        var response = await Client.PostAsJsonAsync($"{Api}/incomes", new IncomeCreateDTO
        {
            Date = new DateOnly(2026, 10, 30),
            IncomeSourceId = Guid.Parse(RamsayId),
            NetValue = 100m,
            Description = "Salary",
            PotsAccountId = Guid.Parse(ReservaId),
            FundingLines = []
        });

        using (new AssertionScope())
        {
            await ShouldBeProblemAsync(response, HttpStatusCode.BadRequest, "Choose the bank this income landed in.");
            SavedCashFlowData().Should().Be(incomesBefore);
        }
    }

    [Fact]
    [Trait("AC", "P61-F02-income-tab-and-bankless-income-07")]
    public async Task MonthTransactions_BanklessIncome_OnceAsNoBankWithoutCategory_SpendUnchanged()
    {
        var rows = (await Client.GetFromJsonAsync<List<MonthTransactionDTO>>($"{Api}{MonthPath}"))!;
        var totals = (await Client.GetFromJsonAsync<List<CategoryTotalDTO>>($"{Api}/expenses/month/2026/10/category-totals"))!;

        using (new AssertionScope())
        {
            rows.Should().ContainSingle(row => row.Transaction.Id == Guid.Parse(BanklessIncomeId)).Which.Should().BeEquivalentTo(new
            {
                AccountId = (Guid?)null,
                AccountName = (string?)null,
                AccountCurrency = (string?)null,
                AccountIsActive = true,
                Transaction = new { Kind = "Income", Amount = 85.20m, Currency = (string?)null, CategoryName = (string?)null, Counterpart = "DividendoJuros" }
            });
            rows.Where(row => row.Transaction.CategoryName is not null).Sum(row => -row.Transaction.Amount)
                .Should().Be(totals.Sum(total => total.TotalValue));
        }
    }

    [Fact]
    [Trait("AC", "P61-F02-income-tab-and-bankless-income-01")]
    [Trait("AC", "P61-F02-income-tab-and-bankless-income-07")]
    public async Task GetIncomesByMonth_CountEqualsTotalIncomingSources()
    {
        var incomes = (await Client.GetFromJsonAsync<List<IncomeDTO>>($"{Api}/incomes/month/2026/10"))!;
        var rows = (await Client.GetFromJsonAsync<List<MonthTransactionDTO>>($"{Api}{MonthPath}"))!;
        var summary = (await Client.GetFromJsonAsync<AccountGroupSummaryDTO>($"{Api}/accounts/groups/default/summary/2026/10"))!;

        using (new AssertionScope())
        {
            incomes.Should().HaveCount(2);
            rows.Count(row => row.Transaction.Kind == "Income").Should().Be(incomes.Count);
            incomes.Sum(income => income.NetValue).Should().Be(summary.TotalIncoming);
        }
    }
}
