using System.Net;
using System.Net.Http.Json;
using Financial.CashFlow.Application.DTOs;
using Financial.CashFlow.Application.Interfaces;
using Financial.CashFlow.Application.Models;
using Financial.TestUtilities;
using FluentAssertions;
using FluentAssertions.Execution;
using Microsoft.Extensions.DependencyInjection;

namespace Financial.Api.Tests.Acceptance;

[Trait("Category", "Integration")]
public class StatementDueDateAcceptanceTests : ApiEndpointTests
{
    private const string BarclaysId = "8f3b1c1a-2e3a-4b1a-9a7f-100000000001";
    private const string MercadoId = "8f3b1c1a-2e3a-4b1a-9a7f-200000000001";
    private const string AmexId = "8f3b1c1a-2e3a-4b1a-9a7f-500000000001";
    private const string OldVisaId = "8f3b1c1a-2e3a-4b1a-9a7f-500000000002";
    private const string UnknownId = "8f3b1c1a-2e3a-4b1a-9a7f-999999999999";
    private static readonly TimeSpan SyncTimeout = TimeSpan.FromSeconds(10);

    private const string SeededData = $$"""
        {
          "Accounts": [
            { "Id": "{{BarclaysId}}", "Name": "Barclays", "Template": "Bank", "Currency": "GBP", "IsActive": true, "OpeningBalance": { "Amount": 1000, "StartDate": "2026-01-01" }, "RoundUp": { "Enabled": false } },
            { "Id": "{{AmexId}}", "Name": "BA Amex", "Template": "CreditCard", "Currency": "GBP", "IsActive": true, "OpeningBalance": null, "RoundUp": null },
            { "Id": "{{OldVisaId}}", "Name": "Old Visa", "Template": "CreditCard", "Currency": "GBP", "IsActive": false, "OpeningBalance": null, "RoundUp": null }
          ],
          "Categories": [
            { "Id": "{{MercadoId}}", "Name": "Mercado", "Active": true, "IsInvestment": false, "IsTithe": false }
          ],
          "Expenses": [
            { "Id": "8f3b1c1a-2e3a-4b1a-9a7f-600000000001", "Date": "2026-09-10", "Description": "September", "Value": 10, "CategoryId": "{{MercadoId}}", "PaymentSourceAccountId": null, "CreditCardId": "{{AmexId}}", "ChargeDate": "2026-09-10", "InvoiceDate": "2026-09-01", "RoundUpAmount": null, "CountsAsTithe": true },
            { "Id": "8f3b1c1a-2e3a-4b1a-9a7f-600000000002", "Date": "2026-10-03", "Description": "October", "Value": 30, "CategoryId": "{{MercadoId}}", "PaymentSourceAccountId": null, "CreditCardId": "{{AmexId}}", "ChargeDate": "2026-10-03", "InvoiceDate": "2026-10-01", "RoundUpAmount": null, "CountsAsTithe": true },
            { "Id": "8f3b1c1a-2e3a-4b1a-9a7f-600000000003", "Date": "2026-10-05", "Description": "November", "Value": 40, "CategoryId": "{{MercadoId}}", "PaymentSourceAccountId": null, "CreditCardId": "{{AmexId}}", "ChargeDate": "2026-10-05", "InvoiceDate": "2026-11-01", "RoundUpAmount": null, "CountsAsTithe": true },
            { "Id": "8f3b1c1a-2e3a-4b1a-9a7f-600000000004", "Date": "2026-10-06", "Description": "December", "Value": 50, "CategoryId": "{{MercadoId}}", "PaymentSourceAccountId": null, "CreditCardId": "{{AmexId}}", "ChargeDate": "2026-10-06", "InvoiceDate": "2026-12-01", "RoundUpAmount": null, "CountsAsTithe": true },
            { "Id": "8f3b1c1a-2e3a-4b1a-9a7f-600000000005", "Date": "2026-10-07", "Description": "Inactive", "Value": 5, "CategoryId": "{{MercadoId}}", "PaymentSourceAccountId": null, "CreditCardId": "{{OldVisaId}}", "ChargeDate": "2026-10-07", "InvoiceDate": "2026-10-01", "RoundUpAmount": null, "CountsAsTithe": true }
          ],
          "CardStatements": [
            { "Id": "8f3b1c1a-2e3a-4b1a-9a7f-700000000001", "CreditCardId": "{{AmexId}}", "Year": 2026, "Month": 9, "IsPaid": true, "DueDate": "2026-09-20" },
            { "Id": "8f3b1c1a-2e3a-4b1a-9a7f-700000000002", "CreditCardId": "{{AmexId}}", "Year": 2026, "Month": 10, "IsPaid": false, "DueDate": null },
            { "Id": "8f3b1c1a-2e3a-4b1a-9a7f-700000000003", "CreditCardId": "{{AmexId}}", "Year": 2026, "Month": 11, "IsPaid": false, "DueDate": null }
          ],
          "Version": 5
        }
        """;

    private readonly FakeCalendarProvider _provider;

    public StatementDueDateAcceptanceTests() : this(new FakeCalendarProvider())
    {
    }

    private StatementDueDateAcceptanceTests(FakeCalendarProvider provider)
        : base(timeProvider: TestClock.At(TestClock.MidOctober), calendarProvider: provider)
    {
        _provider = provider;
        ReplaceCashFlowDataBeforeStart(SeededData);
    }

    private void Connect() =>
        Services.GetRequiredService<ICalendarConnectionStore>().Save(new CalendarConnection(
            "user@gmail.com", "cal-1", "access-token", "refresh-token", TestClock.MidOctober.AddHours(1), TestClock.MidOctober.AddDays(-1), new Dictionary<Guid, string>()));

    private Task<HttpResponseMessage> PutDueDateAsync(string accountId, int year, int month, DateOnly? dueDate) =>
        Client.PutAsJsonAsync($"{Api}/accounts/{accountId}/statements/{year}/{month}/due-date", new StatementDueDateUpdateDTO { DueDate = dueDate });

    private async Task SetAndWaitForTheCalendarAsync(int month, DateOnly? dueDate)
    {
        var response = await PutDueDateAsync(AmexId, 2026, month, dueDate);
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await _provider.EventWrites.WaitAsync(SyncTimeout)).Should().BeTrue("every save triggers a calendar sync");
    }

    private async Task<Dictionary<int, DateOnly?>> DueDatesAsync()
    {
        var statements = await Client.GetFromJsonAsync<List<AccountStatementDTO>>($"{Api}/accounts/{AmexId}/statements");
        return statements!.ToDictionary(statement => statement.Month, statement => statement.DueDate);
    }

    [Fact]
    public async Task GetAccounts_AfterSettingAStatementDueDate_ShowsItOnTheCardOnly()
    {
        (await PutDueDateAsync(AmexId, 2026, 11, new DateOnly(2026, 11, 18))).StatusCode.Should().Be(HttpStatusCode.NoContent);

        var accounts = await Client.GetFromJsonAsync<List<AccountDTO>>($"{Api}/accounts");

        using (new AssertionScope())
        {
            accounts!.Single(account => account.Id == Guid.Parse(AmexId)).DueDate.Should().Be(new DateOnly(2026, 11, 18));
            accounts!.Single(account => account.Id == Guid.Parse(BarclaysId)).DueDate.Should().BeNull();
        }
    }

    [Fact]
    [Trait("AC", "P60-F13-consolidated-navigation-and-retirement-03")]
    public async Task SetAndClear_OneStatement_LeavesTheOthersAndSyncsTheCalendarEachTime()
    {
        Connect();

        await SetAndWaitForTheCalendarAsync(10, new DateOnly(2026, 10, 25));
        await SetAndWaitForTheCalendarAsync(11, new DateOnly(2026, 11, 20));
        await SetAndWaitForTheCalendarAsync(11, null);

        var dueDates = await DueDatesAsync();
        using (new AssertionScope())
        {
            dueDates[9].Should().Be(new DateOnly(2026, 9, 20));
            dueDates[10].Should().Be(new DateOnly(2026, 10, 25));
            dueDates[11].Should().BeNull();
            _provider.CreatedEvents.Should().ContainSingle().Which.Date.Should().Be(new DateOnly(2026, 10, 25));
            _provider.UpdatedEvents.Select(calendarEvent => calendarEvent.Date).Should().Equal(new DateOnly(2026, 11, 20), new DateOnly(2026, 10, 25));
        }
    }

    [Fact]
    [Trait("AC", "P60-F13-consolidated-navigation-and-retirement-03")]
    public async Task Set_MonthWithoutAStatement_CreatesItWithTheDate()
    {
        (await DueDatesAsync())[12].Should().BeNull();

        var response = await PutDueDateAsync(AmexId, 2026, 12, new DateOnly(2026, 12, 18));

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        var statements = await Client.GetFromJsonAsync<List<AccountStatementDTO>>($"{Api}/accounts/{AmexId}/statements");
        var december = statements!.Single(statement => statement.Month == 12);
        december.Id.Should().NotBeNull();
        december.DueDate.Should().Be(new DateOnly(2026, 12, 18));
    }

    [Fact]
    public async Task Set_EarlierMonth_ClearsTheLaterStatementsDates()
    {
        await PutDueDateAsync(AmexId, 2026, 11, new DateOnly(2026, 11, 20));

        await PutDueDateAsync(AmexId, 2026, 10, new DateOnly(2026, 10, 25));

        var dueDates = await DueDatesAsync();
        dueDates[10].Should().Be(new DateOnly(2026, 10, 25));
        dueDates[11].Should().BeNull();
    }

    [Fact]
    public async Task Clear_MonthWithNoStatement_Returns204AndCreatesNothing()
    {
        var response = await PutDueDateAsync(AmexId, 2026, 12, null);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        var statements = await Client.GetFromJsonAsync<List<AccountStatementDTO>>($"{Api}/accounts/{AmexId}/statements");
        statements!.Single(statement => statement.Month == 12).Id.Should().BeNull();
    }

    public static TheoryData<string, int, int, string?, HttpStatusCode, string> Refusals => new()
    {
        { UnknownId, 2026, 10, null, HttpStatusCode.NotFound, $"Account '{UnknownId}' was not found." },
        { BarclaysId, 2026, 10, null, HttpStatusCode.BadRequest, "Barclays is not a card account." },
        { OldVisaId, 2026, 10, null, HttpStatusCode.BadRequest, "Old Visa is inactive." },
        { AmexId, 2027, 1, null, HttpStatusCode.NotFound, "Card statement '2027-01' was not found." },
        { AmexId, 2026, 9, "2026-09-25", HttpStatusCode.BadRequest, "The September 2026 statement is paid, so its due date can't change." },
        { AmexId, 2026, 10, "2026-11-01", HttpStatusCode.BadRequest, "The due date must be in October 2026." }
    };

    [Theory]
    [MemberData(nameof(Refusals))]
    public async Task Refused_ReturnsTheStatusAndMessage(string accountId, int year, int month, string? dueDate, HttpStatusCode status, string message)
    {
        var response = await PutDueDateAsync(accountId, year, month, dueDate is null ? null : DateOnly.Parse(dueDate, System.Globalization.CultureInfo.InvariantCulture));

        await ShouldBeProblemAsync(response, status, message);
    }

    [Theory]
    [InlineData("null")]
    [InlineData("{}")]
    public async Task MissingBodyOrField_Returns400AndChangesNothing(string body)
    {
        await PutDueDateAsync(AmexId, 2026, 10, new DateOnly(2026, 10, 25));

        var response = await Client.PutAsync(
            $"{Api}/accounts/{AmexId}/statements/2026/10/due-date",
            new StringContent(body, System.Text.Encoding.UTF8, "application/json"));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await DueDatesAsync())[10].Should().Be(new DateOnly(2026, 10, 25));
    }
}
