using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Financial.CashFlow.Application.DTOs;
using Financial.CashFlow.Infrastructure.Persistence;
using FluentAssertions;
using FluentAssertions.Execution;

namespace Financial.Api.Tests.Acceptance;

[Trait("Category", "Integration")]
public class AccountModelBankMigrationAcceptanceTests : ApiEndpointTests
{
    private const string BarclaysId = "8f3b1c1a-2e3a-4b1a-9a7f-100000000001";
    private const string Trading212Id = "8f3b1c1a-2e3a-4b1a-9a7f-100000000002";
    private const string ChaseId = "8f3b1c1a-2e3a-4b1a-9a7f-100000000003";
    private const string MercadoCategoryId = "8f3b1c1a-2e3a-4b1a-9a7f-600000000008";

    [Fact]
    [Trait("AC", "P60-F02-account-model-and-bank-migration-07")]
    public async Task RecordsBeforeStartDate_CountInCategoryTotalsNotInBalance()
    {
        await SetOpeningBalanceAsync(BarclaysId, 500m, new DateOnly(2026, 9, 1));
        await AddBankExpenseAsync(new DateOnly(2026, 8, 15), "Before the start date", 40m, BarclaysId);
        await AddBankExpenseAsync(new DateOnly(2026, 9, 2), "After the start date", 15m, BarclaysId);

        var augustTotals = await GetAsync<List<CategoryTotalDTO>>("/api/v1/financial/expenses/month/2026/8/category-totals");
        var augustRows = await GetAsync<List<AccountTransactionDTO>>($"/api/v1/financial/accounts/{BarclaysId}/transactions/2026/8");
        var august = await GetAsync<AccountDetailDTO>($"/api/v1/financial/accounts/{BarclaysId}/2026/8");
        var september = await GetAsync<AccountDetailDTO>($"/api/v1/financial/accounts/{BarclaysId}/2026/9");

        using (new AssertionScope())
        {
            augustTotals.Should().ContainSingle(t => t.Category == "Mercado").Which.TotalValue.Should().Be(40m);
            augustRows.Should().ContainSingle(row => row.Description == "Before the start date");
            august.Balance.Should().Be(500m);
            september.Balance.Should().Be(485m);
        }
    }

    [Fact]
    [Trait("AC", "P60-F02-account-model-and-bank-migration-10")]
    public async Task Expenses_ShapeUnchanged()
    {
        await SetOpeningBalanceAsync(ChaseId, 100.50m, new DateOnly(2026, 9, 1));
        var expenseId = await AddBankExpenseAsync(new DateOnly(2026, 9, 3), "Coffee", 2.30m, ChaseId);

        var expense = await ExpenseOfRowAsync(ChaseId, 9, expenseId);
        var chase = await GetAsync<AccountDetailDTO>($"/api/v1/financial/accounts/{ChaseId}/2026/9");

        using (new AssertionScope())
        {
            expense.Should().Be(
                $$"""{"id":"{{expenseId}}","date":"2026-09-03","description":"Coffee","value":2.30,"categoryId":"{{MercadoCategoryId}}","categoryName":"Mercado","paymentSourceBankId":"{{ChaseId}}","paymentSourceBankName":"Chase","creditCardId":null,"creditCardName":null,"chargeDate":null,"invoiceDate":null,"paymentStatus":"ImmediatePayment","roundUpAmount":null,"suggestedRoundUpAmount":0.70,"countsAsTithe":true}""");
            chase.Balance.Should().Be(98.20m);
        }
    }

    [Fact]
    [Trait("AC", "P60-F02-account-model-and-bank-migration-05")]
    public void ApiHost_Version1File_FailsToStart()
    {
        ReplaceCashFlowDataBeforeStart(
            $$"""{"Banks":[{"Id":"{{BarclaysId}}","Name":"Barclays","RoundUpEnabled":false,"OpeningBalance":0,"OpeningBalanceDate":"0001-01-01"}],"Version":1}""");

        var act = () => Services;

        act.Should().Throw<InvalidDataException>()
            .WithMessage("data-cashflow.json is version 1; this build needs version 5. Run the migrate command first.");
    }

    [Fact]
    [Trait("AC", "P60-F02-account-model-and-bank-migration-05")]
    public async Task ApiHost_CurrentVersionFile_Starts()
    {
        var response = await Client.GetAsync("/api/v1/financial/accounts");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Theory]
    [Trait("AC", "P60-F02-account-model-and-bank-migration-11")]
    [InlineData("TestData", "data-cashflow.test.json")]
    [InlineData("ExampleData", "data-cashflow.example.json")]
    public void SeedAndExampleData_AreTheCurrentVersion(string folder, string fileName)
    {
        var json = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, folder, fileName));

        var act = () => new CashFlowSerializerAdapter().Deserialize(json);

        using (new AssertionScope())
        {
            JsonDocument.Parse(json).RootElement.GetProperty("Version").GetInt32().Should().Be(5);
            act.Should().NotThrow();
        }
    }

    private async Task SetOpeningBalanceAsync(string bankId, decimal amount, DateOnly startDate)
    {
        var bank = (await GetAsync<List<AccountDTO>>("/api/v1/financial/accounts")).Single(account => account.Id == Guid.Parse(bankId));
        var response = await Client.PutAsJsonAsync($"/api/v1/financial/accounts/{bankId}", new AccountUpdateDTO
        {
            TemplateName = bank.TemplateName,
            Name = bank.Name,
            Currency = bank.Currency,
            OpeningBalance = amount,
            BalanceStartDate = startDate,
            RoundUpEnabled = bank.RoundUpEnabled,
            IsActive = bank.IsActive
        });
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    private async Task<Guid> AddBankExpenseAsync(DateOnly date, string description, decimal value, string bankId)
    {
        var response = await Client.PostAsJsonAsync("/api/v1/financial/expenses", new ExpenseCreateDTO
        {
            Date = date,
            Description = description,
            Value = value,
            CategoryId = Guid.Parse(MercadoCategoryId),
            PaymentSourceBankId = Guid.Parse(bankId)
        });
        response.IsSuccessStatusCode.Should().BeTrue();
        return (await response.Content.ReadFromJsonAsync<ExpenseDTO>())!.Id;
    }

    private async Task<T> GetAsync<T>(string route)
    {
        var response = await Client.GetAsync(route);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        return (await response.Content.ReadFromJsonAsync<T>())!;
    }

    private async Task<string> ExpenseOfRowAsync(string accountId, int month, Guid expenseId)
    {
        var response = await Client.GetAsync($"/api/v1/financial/accounts/{accountId}/transactions/2026/{month}");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var row = document.RootElement.EnumerateArray().Single(row => row.GetProperty("id").GetGuid() == expenseId);
        return JsonSerializer.Serialize(row.GetProperty("expense"));
    }
}
