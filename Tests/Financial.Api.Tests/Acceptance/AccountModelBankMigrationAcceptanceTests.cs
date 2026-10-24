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
        var augustExpenses = await GetAsync<List<ExpenseDTO>>("/api/v1/financial/expenses/month/2026/8");
        var augustBalances = await GetAsync<List<BankBalanceDTO>>("/api/v1/financial/banks/month/2026/8/balances");
        var septemberBalances = await GetAsync<List<BankBalanceDTO>>("/api/v1/financial/banks/month/2026/9/balances");

        using (new AssertionScope())
        {
            augustTotals.Should().ContainSingle(t => t.Category == "Mercado").Which.TotalValue.Should().Be(40m);
            augustExpenses.Should().ContainSingle(e => e.Description == "Before the start date");
            augustBalances.Should().ContainSingle(b => b.Bank == "Barclays").Which.Balance.Should().Be(500m);
            septemberBalances.Should().ContainSingle(b => b.Bank == "Barclays").Which.Balance.Should().Be(485m);
        }
    }

    [Fact]
    [Trait("AC", "P60-F02-account-model-and-bank-migration-10")]
    public async Task BanksTransfersExpenses_ShapesUnchanged()
    {
        await SetOpeningBalanceAsync(ChaseId, 100.50m, new DateOnly(2026, 9, 1));
        var expenseId = await AddBankExpenseAsync(new DateOnly(2026, 9, 3), "Coffee", 2.30m, ChaseId);
        var transferId = await AddTransferAsync(new DateOnly(2026, 9, 4), BarclaysId, Trading212Id, 25m);

        var banks = await GetRawAsync("/api/v1/financial/banks");
        var expenses = await GetRawAsync("/api/v1/financial/expenses/month/2026/9");
        var transfers = await GetRawAsync("/api/v1/financial/transfers/month/2026/9");
        var balances = await GetRawAsync("/api/v1/financial/banks/month/2026/9/balances");

        using (new AssertionScope())
        {
            banks.Should().Be(
                $$"""[{"id":"{{BarclaysId}}","name":"Barclays","roundUpEnabled":false,"openingBalance":0,"openingBalanceDate":"0001-01-01","hasReferences":true,"isActive":true},{"id":"{{Trading212Id}}","name":"Trading212","roundUpEnabled":true,"openingBalance":0,"openingBalanceDate":"0001-01-01","hasReferences":true,"isActive":true},{"id":"{{ChaseId}}","name":"Chase","roundUpEnabled":true,"openingBalance":100.50,"openingBalanceDate":"2026-09-01","hasReferences":true,"isActive":true}]""");
            expenses.Should().Be(
                $$"""[{"id":"{{expenseId}}","date":"2026-09-03","description":"Coffee","value":2.30,"categoryId":"{{MercadoCategoryId}}","categoryName":"Mercado","paymentSourceBankId":"{{ChaseId}}","paymentSourceBankName":"Chase","creditCardId":null,"creditCardName":null,"chargeDate":null,"invoiceDate":null,"paymentStatus":"ImmediatePayment","roundUpAmount":null,"suggestedRoundUpAmount":0.70,"countsAsTithe":true}]""");
            transfers.Should().Be(
                $$"""[{"id":"{{transferId}}","date":"2026-09-04","sourceBankId":"{{BarclaysId}}","sourceBankName":"Barclays","destinationBankId":"{{Trading212Id}}","destinationBankName":"Trading212","amount":25,"note":null}]""");
            balances.Should().Be(
                """[{"bank":"Barclays","balance":-25},{"bank":"Trading212","balance":25},{"bank":"Chase","balance":98.20}]""");
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
            .WithMessage("data-cashflow.json is version 1; this build needs version 2. Run the migrate command first.");
    }

    [Fact]
    [Trait("AC", "P60-F02-account-model-and-bank-migration-05")]
    public async Task ApiHost_Version2File_Starts()
    {
        var response = await Client.GetAsync("/api/v1/financial/banks");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Theory]
    [Trait("AC", "P60-F02-account-model-and-bank-migration-11")]
    [InlineData("TestData", "data-cashflow.test.json")]
    [InlineData("ExampleData", "data-cashflow.example.json")]
    public void SeedAndExampleData_AreVersion2(string folder, string fileName)
    {
        var json = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, folder, fileName));

        var act = () => new CashFlowSerializerAdapter().Deserialize(json);

        using (new AssertionScope())
        {
            JsonDocument.Parse(json).RootElement.GetProperty("Version").GetInt32().Should().Be(2);
            act.Should().NotThrow();
        }
    }

    private async Task SetOpeningBalanceAsync(string bankId, decimal amount, DateOnly startDate)
    {
        var response = await Client.PutAsJsonAsync(
            $"/api/v1/financial/banks/{bankId}/opening-balance",
            new BankOpeningBalanceUpdateDTO { OpeningBalance = amount, OpeningBalanceDate = startDate });
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

    private async Task<Guid> AddTransferAsync(DateOnly date, string sourceId, string destinationId, decimal amount)
    {
        var response = await Client.PostAsJsonAsync("/api/v1/financial/transfers", new TransferCreateDTO
        {
            Date = date,
            SourceBankId = Guid.Parse(sourceId),
            DestinationBankId = Guid.Parse(destinationId),
            Amount = amount
        });
        response.IsSuccessStatusCode.Should().BeTrue();
        return (await response.Content.ReadFromJsonAsync<TransferDTO>())!.Id;
    }

    private async Task<T> GetAsync<T>(string route)
    {
        var response = await Client.GetAsync(route);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        return (await response.Content.ReadFromJsonAsync<T>())!;
    }

    private async Task<string> GetRawAsync(string route)
    {
        var response = await Client.GetAsync(route);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return JsonSerializer.Serialize(document.RootElement);
    }
}
