using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;
using Financial.CashFlow.Application.DTOs;
using FluentAssertions;
using FluentAssertions.Execution;

namespace Financial.Api.Tests.Acceptance;

[Trait("Category", "Integration")]
public class TitheViewAcceptanceTests : ApiEndpointTests
{
    private const string BarclaysId = "f3a00000-0000-0000-0000-000000000001";
    private const string DizimoId = "f3a00000-0000-0000-0000-000000000201";
    private const string SalaryId = "f3a00000-0000-0000-0000-000000000301";
    private const string DividendsId = "f3a00000-0000-0000-0000-000000000302";

    private const string SeededData = $$"""
        {
          "Accounts": [
            { "Id": "{{BarclaysId}}", "Name": "Barclays", "Template": "Bank", "Currency": "GBP", "IsActive": true, "OpeningBalance": { "Amount": 1000, "StartDate": "2026-01-01" }, "RoundUp": { "Enabled": false }, "Pots": null }
          ],
          "Categories": [
            { "Id": "{{DizimoId}}", "Name": "Dizimo", "Active": true, "IsInvestment": false, "IsTithe": true }
          ],
          "IncomeSources": [
            { "Id": "{{SalaryId}}", "Name": "Salary", "IsActive": true, "Group": "Salary" },
            { "Id": "{{DividendsId}}", "Name": "DividendoJuros", "IsActive": true, "Group": "DividendoJuros" }
          ],
          "Expenses": [
            { "Id": "f3a00000-0000-0000-0000-000000000401", "Date": "2026-08-10", "Description": "August tithe", "Value": 150, "CategoryId": "{{DizimoId}}", "PaymentSourceAccountId": "{{BarclaysId}}", "CreditCardId": null, "ChargeDate": null, "InvoiceDate": null, "RoundUpAmount": null, "CountsAsTithe": true },
            { "Id": "f3a00000-0000-0000-0000-000000000402", "Date": "2026-10-10", "Description": "October tithe", "Value": 400, "CategoryId": "{{DizimoId}}", "PaymentSourceAccountId": "{{BarclaysId}}", "CreditCardId": null, "ChargeDate": null, "InvoiceDate": null, "RoundUpAmount": null, "CountsAsTithe": true },
            { "Id": "f3a00000-0000-0000-0000-000000000403", "Date": "2026-10-12", "Description": "Offer", "Value": 25, "CategoryId": "{{DizimoId}}", "PaymentSourceAccountId": "{{BarclaysId}}", "CreditCardId": null, "ChargeDate": null, "InvoiceDate": null, "RoundUpAmount": null, "CountsAsTithe": false }
          ],
          "Incomes": [
            { "Id": "f3a00000-0000-0000-0000-000000000801", "Date": "2026-07-01", "IncomeSourceId": "{{SalaryId}}", "GrossValue": null, "NetValue": 500, "AccountId": "{{BarclaysId}}", "Description": "July pay" },
            { "Id": "f3a00000-0000-0000-0000-000000000802", "Date": "2026-08-01", "IncomeSourceId": "{{SalaryId}}", "GrossValue": null, "NetValue": 2000, "AccountId": "{{BarclaysId}}", "Description": "August pay" },
            { "Id": "f3a00000-0000-0000-0000-000000000803", "Date": "2026-09-01", "IncomeSourceId": "{{SalaryId}}", "GrossValue": null, "NetValue": 1000, "AccountId": "{{BarclaysId}}", "Description": "September pay" },
            { "Id": "f3a00000-0000-0000-0000-000000000804", "Date": "2026-10-01", "IncomeSourceId": "{{SalaryId}}", "GrossValue": null, "NetValue": 2800, "AccountId": "{{BarclaysId}}", "Description": "October pay" },
            { "Id": "f3a00000-0000-0000-0000-000000000805", "Date": "2026-10-15", "IncomeSourceId": "{{DividendsId}}", "GrossValue": null, "NetValue": 200, "AccountId": null, "Description": "XP dividends" }
          ],
          "TitheCarryForwardEffectiveFrom": "2026-08-01",
          "Version": 5
        }
        """;

    public TitheViewAcceptanceTests()
        : base(timeProvider: TestClock.At(new DateTimeOffset(2026, 11, 15, 12, 0, 0, TimeSpan.Zero)))
    {
        ReplaceCashFlowDataBeforeStart(SeededData);
    }

    private Task<TitheSummaryDTO?> MonthAsync(int month) =>
        Client.GetFromJsonAsync<TitheSummaryDTO>($"{Api}/tithe/month/2026/{month}");

    private Task<List<TitheMonthDTO>?> YearAsync() =>
        Client.GetFromJsonAsync<List<TitheMonthDTO>>($"{Api}/tithe/year/2026");

    private async Task<List<TitheMonthDTO>> YearEqualToTheMonthRouteAsync()
    {
        var year = JsonNode.Parse(await Client.GetStringAsync($"{Api}/tithe/year/2026"))!.AsArray();

        year.Should().HaveCount(12);
        for (var month = 1; month <= 12; month++)
        {
            var item = year[month - 1]!;
            var monthRoute = JsonNode.Parse(await Client.GetStringAsync($"{Api}/tithe/month/2026/{month}"));
            item["year"]!.GetValue<int>().Should().Be(2026);
            item["month"]!.GetValue<int>().Should().Be(month);
            JsonNode.DeepEquals(item["summary"], monthRoute).Should().BeTrue($"month {month} must equal the month route");
        }

        return year.Deserialize<List<TitheMonthDTO>>(JsonSerializerOptions.Web)!;
    }

    [Fact]
    [Trait("AC", "P61-F03-tithe-tab-and-history-01")]
    public async Task TitheMonth_ReturnsBaseTithePaidCarryInAndBalance()
    {
        var october = await MonthAsync(10);

        using (new AssertionScope())
        {
            october!.IncomeBase.Should().Be(3000m);
            october.CalculatedTithe.Should().Be(300m);
            october.TithePaid.Should().Be(400m);
            october.CarryForward!.Amount.Should().Be(150m);
            october.CarryForward.Included.Should().BeTrue();
            october.CarryForward.FromYear.Should().Be(2026);
            october.CarryForward.FromMonth.Should().Be(9);
            october.TitheBalance.Should().Be(50m);
        }
    }

    [Fact]
    [Trait("AC", "P61-F03-tithe-tab-and-history-04")]
    public async Task TitheYear_ReturnsTwelveItemsEqualToTheMonthRoute()
    {
        var year = await YearEqualToTheMonthRouteAsync();

        using (new AssertionScope())
        {
            year.Select(item => item.Month).Should().Equal(Enumerable.Range(1, 12));
            year[6].Summary.CarryForward.Should().BeNull();
            year[6].Summary.TitheBalance.Should().Be(50m);
            year[7].Summary.CarryForward.Should().BeNull("the anchor month never carries in");
            year[7].Summary.TitheBalance.Should().Be(50m);
            year[8].Summary.CarryForward!.Amount.Should().Be(50m);
            year[8].Summary.TitheBalance.Should().Be(150m);
            year[9].Summary.CarryForward!.Amount.Should().Be(150m);
            year[9].Summary.TitheBalance.Should().Be(50m);
            year[10].Summary.CarryForward!.Amount.Should().Be(50m);
            year[11].Summary.TitheBalance.Should().Be(50m);
        }
    }

    [Theory]
    [InlineData(0)]
    [InlineData(10000)]
    [Trait("AC", "P61-F03-tithe-tab-and-history-04")]
    public async Task TitheYear_InvalidYear_Returns400(int year)
    {
        var response = await Client.GetAsync($"{Api}/tithe/year/{year}");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    [Trait("AC", "P61-F03-tithe-tab-and-history-02")]
    public async Task TitheYear_AfterCarryForwardUpdate_NextMonthChanges()
    {
        var response = await Client.PutAsJsonAsync(
            $"{Api}/tithe/month/2026/9/carry-forward", new TitheCarryForwardUpdateDTO { Included = false });
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var year = await YearEqualToTheMonthRouteAsync();

        using (new AssertionScope())
        {
            year[8].Summary.CarryForward!.Included.Should().BeFalse();
            year[8].Summary.TitheBalance.Should().Be(100m);
            year[9].Summary.CarryForward!.Amount.Should().Be(100m);
            year[9].Summary.TitheBalance.Should().Be(0m);
            year[10].Summary.CarryForward.Should().BeNull();
            year[10].Summary.TitheBalance.Should().Be(0m);
        }
    }

    [Fact]
    [Trait("AC", "P61-cross-03")]
    public async Task TitheMonth_BanklessIncome_RaisesBaseAndTithe()
    {
        var before = await MonthAsync(10);

        var created = await Client.PostAsJsonAsync($"{Api}/incomes", new IncomeCreateDTO
        {
            Date = new DateOnly(2026, 10, 20),
            IncomeSourceId = Guid.Parse(DividendsId),
            NetValue = 100m,
            BankId = null,
            Description = "Interest"
        });
        created.StatusCode.Should().Be(HttpStatusCode.OK);

        var after = await MonthAsync(10);
        var octoberRow = (await YearAsync())![9].Summary;

        using (new AssertionScope())
        {
            after!.IncomeBase.Should().Be(before!.IncomeBase + 100m);
            after.CalculatedTithe.Should().Be(before.CalculatedTithe + 10m);
            octoberRow.Should().BeEquivalentTo(after);
        }
    }
}
