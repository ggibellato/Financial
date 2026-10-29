using System.Net;
using System.Net.Http.Json;
using Financial.CashFlow.Application.DTOs;
using FluentAssertions;
using FluentAssertions.Execution;
using Microsoft.AspNetCore.Mvc;

namespace Financial.Api.Tests.Acceptance;

[Trait("Category", "Integration")]
public class AdminAccountsAcceptanceTests : ApiEndpointTests
{
    private const string BarclaysId = "8f3b1c1a-2e3a-4b1a-9a7f-100000000001";
    private const string ChaseId = "8f3b1c1a-2e3a-4b1a-9a7f-100000000002";
    private const string SpareId = "8f3b1c1a-2e3a-4b1a-9a7f-100000000003";
    private const string GroceriesId = "8f3b1c1a-2e3a-4b1a-9a7f-200000000001";
    private const string SalaryId = "8f3b1c1a-2e3a-4b1a-9a7f-400000000001";
    private const string AccountsUrl = "/api/v1/financial/accounts";

    private const string SeededData = $$"""
        {
          "Accounts": [
            { "Id": "{{BarclaysId}}", "Name": "Barclays", "Template": "Bank", "Currency": "GBP", "IsActive": true, "OpeningBalance": { "Amount": 1000, "StartDate": "2026-01-01" }, "RoundUp": { "Enabled": false } },
            { "Id": "{{ChaseId}}", "Name": "Chase", "Template": "Bank", "Currency": "GBP", "IsActive": true, "OpeningBalance": { "Amount": 200, "StartDate": "2026-01-01" }, "RoundUp": { "Enabled": true } },
            { "Id": "{{SpareId}}", "Name": "Spare", "Template": "Bank", "Currency": "GBP", "IsActive": true, "OpeningBalance": { "Amount": 0, "StartDate": "2026-01-01" }, "RoundUp": { "Enabled": false } }
          ],
          "Categories": [
            { "Id": "{{GroceriesId}}", "Name": "Mercado", "Active": true, "IsInvestment": false, "IsTithe": false }
          ],
          "IncomeSources": [
            { "Id": "{{SalaryId}}", "Name": "Salary", "IsActive": true, "Group": "Salary" }
          ],
          "Expenses": [
            { "Id": "8f3b1c1a-2e3a-4b1a-9a7f-500000000001", "Date": "2026-09-10", "Description": "Shop", "Value": 50, "CategoryId": "{{GroceriesId}}", "PaymentSourceAccountId": "{{BarclaysId}}", "CreditCardId": null, "ChargeDate": null, "InvoiceDate": null, "RoundUpAmount": null, "CountsAsTithe": true }
          ],
          "Incomes": [
            { "Id": "8f3b1c1a-2e3a-4b1a-9a7f-600000000001", "Date": "2026-09-01", "IncomeSourceId": "{{SalaryId}}", "GrossValue": null, "NetValue": 2000, "AccountId": "{{BarclaysId}}", "Description": "September pay" }
          ],
          "Version": 4
        }
        """;

    public AdminAccountsAcceptanceTests()
        : base(timeProvider: TestClock.At(new DateTimeOffset(2026, 10, 15, 12, 0, 0, TimeSpan.FromHours(1))))
    {
        ReplaceCashFlowDataBeforeStart(SeededData);
    }

    private static AccountCreateDTO NewBank(string name, string currency = "GBP") => new()
    {
        TemplateName = "Bank",
        Name = name,
        Currency = currency,
        OpeningBalance = 250m,
        BalanceStartDate = new DateOnly(2026, 10, 1),
        RoundUpEnabled = true
    };

    private static AccountUpdateDTO EditBarclays(string currency = "GBP", bool isActive = true) => new()
    {
        TemplateName = "Bank",
        Name = "Barclays",
        Currency = currency,
        OpeningBalance = 1000m,
        BalanceStartDate = new DateOnly(2026, 1, 1),
        RoundUpEnabled = false,
        IsActive = isActive
    };

    [Fact]
    [Trait("AC", "P60-F06-admin-accounts-page-01")]
    public async Task PostAccount_GbpBank_AppearsInTheAccountListAndTheDefaultGroup()
    {
        var response = await Client.PostAsJsonAsync(AccountsUrl, NewBank("Monzo"));
        var created = await response.Content.ReadFromJsonAsync<AccountDTO>();
        var accounts = await Client.GetFromJsonAsync<List<AccountDTO>>(AccountsUrl);
        var summary = await Client.GetFromJsonAsync<AccountGroupSummaryDTO>($"{AccountsUrl}/groups/default/summary/2026/10");

        using (new AssertionScope())
        {
            response.StatusCode.Should().Be(HttpStatusCode.Created);
            created!.Name.Should().Be("Monzo");
            created.Currency.Should().Be("GBP");
            created.HasRecords.Should().BeFalse();
            accounts.Should().ContainSingle(account => account.Id == created.Id)
                .Which.Should().BeEquivalentTo(new { OpeningBalance = 250m, BalanceStartDate = new DateOnly(2026, 10, 1), RoundUpEnabled = true });
            summary!.Members.Should().ContainSingle(member => member.AccountId == created.Id).Which.Balance.Should().Be(250m);
        }
    }

    [Fact]
    [Trait("AC", "P60-F06-admin-accounts-page-01")]
    public async Task PostAccount_BankInBrl_IsRejectedWith400()
    {
        var response = await Client.PostAsJsonAsync(AccountsUrl, NewBank("Itaú", currency: "BRL"));
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        var accounts = await Client.GetFromJsonAsync<List<AccountDTO>>(AccountsUrl);

        using (new AssertionScope())
        {
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            problem!.Detail.Should().Be("Bank accounts must be GBP.");
            accounts.Should().HaveCount(3);
        }
    }

    [Fact]
    public async Task GetTemplates_ListsBankCreditCardAndPotsWithGbpAndTheirCapabilities()
    {
        var templates = await Client.GetFromJsonAsync<List<AccountTemplateDTO>>($"{AccountsUrl}/templates");

        using (new AssertionScope())
        {
            templates!.Select(template => template.Name).Should().Equal("Bank", "CreditCard", "Pots");
            templates![0].Currencies.Should().Equal("GBP");
            templates![0].Capabilities.Should().Contain(["HasOpeningBalance", "SupportsRoundUp"]);
            templates![1].Currencies.Should().Equal("GBP");
            templates![1].Capabilities.Should().Equal("HasCategories", "HasStatements", "ReceivesPayments");
            templates![2].Currencies.Should().Equal("GBP");
            templates![2].Capabilities.Should().Equal("HasPots", "ReceivesIncomeSplit", "CanWithdraw");
        }
    }

    [Fact]
    public async Task PostAccount_EmptyPots_IsListedWithNoActionsAndStaysOutOfTheDefaultGroup()
    {
        var response = await Client.PostAsJsonAsync(AccountsUrl, new AccountCreateDTO
        {
            TemplateName = "Pots",
            Name = "Holiday pots",
            Currency = "GBP",
            CategoryId = Guid.Parse(GroceriesId)
        });
        var created = await response.Content.ReadFromJsonAsync<AccountDTO>();
        var accounts = await Client.GetFromJsonAsync<List<AccountDTO>>(AccountsUrl);
        var detail = await Client.GetFromJsonAsync<AccountDetailDTO>($"{AccountsUrl}/{created!.Id}/2026/10");
        var summary = await Client.GetFromJsonAsync<AccountGroupSummaryDTO>($"{AccountsUrl}/groups/default/summary/2026/10");

        using (new AssertionScope())
        {
            response.StatusCode.Should().Be(HttpStatusCode.Created);
            accounts.Should().ContainSingle(account => account.Id == created.Id)
                .Which.Should().BeEquivalentTo(new
                {
                    TemplateName = "Pots",
                    Currency = "GBP",
                    IsActive = true,
                    HasRecords = false,
                    OpeningBalance = (decimal?)null,
                    RoundUpEnabled = (bool?)null,
                    CategoryId = (Guid?)Guid.Parse(GroceriesId),
                    Capabilities = new[] { "HasPots", "ReceivesIncomeSplit", "CanWithdraw" }
                });
            detail!.Balance.Should().Be(0m);
            detail.AllowedActions.Should().BeEmpty();
            summary!.Members.Should().NotContain(member => member.AccountId == created.Id);
        }
    }

    [Fact]
    public async Task PostAccount_PotsWithoutACategory_Returns400AndCreatesNothing()
    {
        var response = await Client.PostAsJsonAsync(AccountsUrl, new AccountCreateDTO { TemplateName = "Pots", Name = "Holiday pots", Currency = "GBP" });
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        var accounts = await Client.GetFromJsonAsync<List<AccountDTO>>(AccountsUrl);

        using (new AssertionScope())
        {
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            problem!.Detail.Should().Be("Choose the category for this account's bank expenses.");
            accounts.Should().NotContain(account => account.Name == "Holiday pots");
        }
    }

    [Fact]
    [Trait("AC", "P60-F06-admin-accounts-page-02")]
    public async Task PostAccount_DuplicateNameInAnotherCase_IsRejectedWith409()
    {
        var response = await Client.PostAsJsonAsync(AccountsUrl, NewBank("barclays"));
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();

        using (new AssertionScope())
        {
            response.StatusCode.Should().Be(HttpStatusCode.Conflict);
            problem!.Detail.Should().Be("An account named \"barclays\" already exists.");
        }
    }

    [Fact]
    [Trait("AC", "P60-F06-admin-accounts-page-03")]
    public async Task PutAccount_CurrencyChangeOnAnAccountWithRecords_IsRejectedWith400()
    {
        var response = await Client.PutAsJsonAsync($"{AccountsUrl}/{BarclaysId}", EditBarclays(currency: "BRL"));
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        var accounts = await Client.GetFromJsonAsync<List<AccountDTO>>(AccountsUrl);

        using (new AssertionScope())
        {
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            problem!.Detail.Should().Be("Template and currency can't change once the account has transactions.");
            accounts.Should().ContainSingle(account => account.Name == "Barclays").Which.Currency.Should().Be("GBP");
        }
    }

    [Fact]
    [Trait("AC", "P60-F06-admin-accounts-page-04")]
    public async Task DeleteAccount_WithRecords_IsRejectedWith409AndTheDeactivateSuggestion()
    {
        var response = await Client.DeleteAsync($"{AccountsUrl}/{BarclaysId}");
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        var accounts = await Client.GetFromJsonAsync<List<AccountDTO>>(AccountsUrl);

        using (new AssertionScope())
        {
            response.StatusCode.Should().Be(HttpStatusCode.Conflict);
            problem!.Detail.Should().Be("Barclays has transactions and cannot be deleted. Deactivate it instead.");
            accounts.Should().ContainSingle(account => account.Name == "Barclays").Which.HasRecords.Should().BeTrue();
        }
    }

    [Fact]
    [Trait("AC", "P60-F06-admin-accounts-page-04")]
    public async Task DeleteAccount_Unused_RemovesIt()
    {
        var response = await Client.DeleteAsync($"{AccountsUrl}/{SpareId}");
        var accounts = await Client.GetFromJsonAsync<List<AccountDTO>>(AccountsUrl);

        using (new AssertionScope())
        {
            response.StatusCode.Should().Be(HttpStatusCode.NoContent);
            accounts!.Select(account => account.Name).Should().Equal("Barclays", "Chase");
        }
    }

    [Fact]
    [Trait("AC", "P60-F06-admin-accounts-page-02")]
    public async Task PutAccount_RenameToAnotherAccountInAnotherCase_IsRejectedWith409()
    {
        var response = await Client.PutAsJsonAsync($"{AccountsUrl}/{ChaseId}", new AccountUpdateDTO
        {
            TemplateName = "Bank", Name = "BARCLAYS", Currency = "GBP", OpeningBalance = 200m,
            BalanceStartDate = new DateOnly(2026, 1, 1), RoundUpEnabled = true, IsActive = true
        });

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task PutAccount_UnknownId_Returns404()
    {
        var response = await Client.PutAsJsonAsync($"{AccountsUrl}/{Guid.NewGuid()}", EditBarclays());

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteAccount_UnknownId_Returns404()
    {
        var response = await Client.DeleteAsync($"{AccountsUrl}/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    [Trait("AC", "P60-F06-admin-accounts-page-05")]
    public async Task PutAccount_Deactivated_LeavesTheDestinationListsAndKeepsItsFigures()
    {
        var detailBefore = await Client.GetFromJsonAsync<AccountDetailDTO>($"{AccountsUrl}/{BarclaysId}/2026/9");
        var summaryBefore = await Client.GetFromJsonAsync<AccountGroupSummaryDTO>($"{AccountsUrl}/groups/default/summary/2026/9");

        var response = await Client.PutAsJsonAsync($"{AccountsUrl}/{BarclaysId}", EditBarclays(isActive: false));

        var destinations = await Client.GetFromJsonAsync<List<TransferAccountOptionDTO>>($"{AccountsUrl}/{ChaseId}/transfer-destinations");
        var detailAfter = await Client.GetFromJsonAsync<AccountDetailDTO>($"{AccountsUrl}/{BarclaysId}/2026/9");
        var summaryAfter = await Client.GetFromJsonAsync<AccountGroupSummaryDTO>($"{AccountsUrl}/groups/default/summary/2026/9");

        using (new AssertionScope())
        {
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            (await response.Content.ReadFromJsonAsync<AccountDTO>())!.IsActive.Should().BeFalse();
            destinations!.Select(option => option.Name).Should().Equal("Spare");
            detailAfter!.Balance.Should().Be(detailBefore!.Balance).And.Be(2950m);
            detailAfter.IsActive.Should().BeFalse();
            summaryAfter!.Members.Select(member => (member.Name, member.Balance))
                .Should().Equal(summaryBefore!.Members.Select(member => (member.Name, member.Balance)));
            summaryAfter.Totals.Should().BeEquivalentTo(summaryBefore.Totals);
        }
    }
}
