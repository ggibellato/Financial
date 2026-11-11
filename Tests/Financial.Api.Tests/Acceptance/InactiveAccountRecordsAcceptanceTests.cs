using System.Net;
using System.Net.Http.Json;
using Financial.CashFlow.Application.DTOs;
using FluentAssertions;
using FluentAssertions.Execution;

namespace Financial.Api.Tests.Acceptance;

[Trait("Category", "Integration")]
public class InactiveAccountRecordsAcceptanceTests : ApiEndpointTests
{
    private const string InactiveMessage = "Chase is inactive.";
    private static readonly Guid BarclaysId = Guid.Parse("8f3b1c1a-2e3a-4b1a-9a7f-100000000001");
    private static readonly Guid ChaseId = Guid.Parse("8f3b1c1a-2e3a-4b1a-9a7f-100000000003");
    private static readonly Guid VisaId = Guid.Parse("8f3b1c1a-2e3a-4b1a-9a7f-500000000001");
    private static readonly Guid GleisonId = Guid.Parse("8f3b1c1a-2e3a-4b1a-9a7f-000000000001");
    private static readonly Guid InvestimentoBucketId = Guid.Parse("8f3b1c1a-2e3a-4b1a-9a7f-300000000001");
    private static readonly Guid ReservaId = Guid.Parse("8f3b1c1a-2e3a-4b1a-9a7f-900000000001");
    private static readonly Guid MercadoId = Guid.Parse("8f3b1c1a-2e3a-4b1a-9a7f-600000000008");
    private static readonly DateOnly Date = new(2026, 10, 2);

    private static ExpenseCreateDTO Expense(Guid? bankId, Guid? creditCardId = null) => new()
    {
        Date = Date,
        Description = "Shop",
        Value = 20m,
        CategoryId = MercadoId,
        PaymentSourceBankId = bankId,
        CreditCardId = creditCardId
    };

    private static ExpenseUpdateDTO ExpenseEdit(Guid bankId, string description = "Shop") => new()
    {
        Date = Date,
        Description = description,
        Value = 20m,
        CategoryId = MercadoId,
        PaymentSourceBankId = bankId
    };

    private static IncomeCreateDTO Income(Guid bankId) => new()
    {
        Date = Date,
        IncomeSourceId = GleisonId,
        NetValue = 100m,
        BankId = bankId
    };

    private static IncomeUpdateDTO IncomeEdit(Guid bankId, decimal netValue = 100m) => new()
    {
        Date = Date,
        IncomeSourceId = GleisonId,
        NetValue = netValue,
        BankId = bankId
    };

    private async Task DeactivateChaseAsync()
    {
        var response = await Client.PutAsJsonAsync($"{Api}/accounts/{ChaseId}", new AccountUpdateDTO
        {
            TemplateName = "Bank",
            Name = "Chase",
            Currency = "GBP",
            OpeningBalance = 0m,
            BalanceStartDate = new DateOnly(1, 1, 1),
            RoundUpEnabled = true,
            IsActive = false
        });
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    private async Task<T> CreateAsync<T>(string path, object request)
    {
        var response = await Client.PostAsJsonAsync($"{Api}/{path}", request);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<T>())!;
    }

    private static Task ShouldBeRefusedAsInactive(HttpResponseMessage response) =>
        ShouldBeProblemAsync(response, HttpStatusCode.BadRequest, InactiveMessage);

    [Fact]
    [Trait("AC", "P60-F06-admin-accounts-page-06")]
    public async Task PostExpense_OnAnInactiveBank_IsRejectedWith400()
    {
        await DeactivateChaseAsync();

        await ShouldBeRefusedAsInactive(await Client.PostAsJsonAsync($"{Api}/expenses", Expense(ChaseId)));
    }

    [Fact]
    [Trait("AC", "P60-F06-admin-accounts-page-06")]
    public async Task PutExpense_MovingOntoAnInactiveBank_IsRejectedWith400()
    {
        var expense = await CreateAsync<ExpenseDTO>("expenses", Expense(BarclaysId));
        await DeactivateChaseAsync();

        await ShouldBeRefusedAsInactive(await Client.PutAsJsonAsync($"{Api}/expenses/{expense.Id}", ExpenseEdit(ChaseId)));
    }

    [Fact]
    [Trait("AC", "P60-F06-admin-accounts-page-06")]
    public async Task PutAndDeleteExpense_OnItsOwnInactiveBank_Succeed()
    {
        var expense = await CreateAsync<ExpenseDTO>("expenses", Expense(ChaseId));
        await DeactivateChaseAsync();

        var edit = await Client.PutAsJsonAsync($"{Api}/expenses/{expense.Id}", ExpenseEdit(ChaseId, description: "Corrected"));
        var delete = await Client.DeleteAsync($"{Api}/expenses/{expense.Id}");

        using (new AssertionScope())
        {
            edit.StatusCode.Should().Be(HttpStatusCode.OK);
            delete.IsSuccessStatusCode.Should().BeTrue();
        }
    }

    [Fact]
    [Trait("AC", "P60-F06-admin-accounts-page-06")]
    public async Task PostIncome_OnAnInactiveBank_IsRejectedWith400()
    {
        await DeactivateChaseAsync();

        await ShouldBeRefusedAsInactive(await Client.PostAsJsonAsync($"{Api}/incomes", Income(ChaseId)));
    }

    [Fact]
    [Trait("AC", "P60-F06-admin-accounts-page-06")]
    public async Task PutIncome_MovingOntoAnInactiveBank_IsRejectedWith400()
    {
        var income = await CreateAsync<IncomeDTO>("incomes", Income(BarclaysId));
        await DeactivateChaseAsync();

        await ShouldBeRefusedAsInactive(await Client.PutAsJsonAsync($"{Api}/incomes/{income.Id}", IncomeEdit(ChaseId)));
    }

    [Fact]
    [Trait("AC", "P60-F06-admin-accounts-page-06")]
    public async Task PutAndDeleteIncome_OnItsOwnInactiveBank_Succeed()
    {
        var income = await CreateAsync<IncomeDTO>("incomes", Income(ChaseId));
        await DeactivateChaseAsync();

        var edit = await Client.PutAsJsonAsync($"{Api}/incomes/{income.Id}", IncomeEdit(ChaseId, netValue: 120m));
        var delete = await Client.DeleteAsync($"{Api}/incomes/{income.Id}");

        using (new AssertionScope())
        {
            edit.StatusCode.Should().Be(HttpStatusCode.OK);
            delete.IsSuccessStatusCode.Should().BeTrue();
        }
    }

    [Fact]
    [Trait("AC", "P60-F06-admin-accounts-page-06")]
    public async Task PostAdjustment_OnAnInactiveBank_IsRejectedWith400()
    {
        await DeactivateChaseAsync();

        var response = await Client.PostAsJsonAsync(
            $"{Api}/accounts/{ChaseId}/adjustments", new BalanceAdjustmentCreateDTO { Date = Date, TargetBalance = 10m });

        await ShouldBeRefusedAsInactive(response);
    }

    [Fact]
    [Trait("AC", "P60-F06-admin-accounts-page-06")]
    public async Task DeleteAdjustment_OnAnInactiveBank_Succeeds()
    {
        var adjustment = await CreateAsync<BalanceAdjustmentDTO>(
            $"accounts/{ChaseId}/adjustments", new BalanceAdjustmentCreateDTO { Date = Date, TargetBalance = 10m });
        await DeactivateChaseAsync();

        var delete = await Client.DeleteAsync($"{Api}/accounts/{ChaseId}/adjustments/{adjustment.Id}");

        delete.IsSuccessStatusCode.Should().BeTrue();
    }

    [Fact]
    [Trait("AC", "P60-F06-admin-accounts-page-06")]
    public async Task MarkBillPaid_FromAnInactiveBank_IsRejectedWith400()
    {
        var bill = await CreateAsync<RecurringBillDTO>(
            "mensais", new RecurringBillCreateDTO { DueDay = 5, Description = "Council Tax", Value = 120m, Area = "UK" });
        await DeactivateChaseAsync();

        var response = await Client.PostAsJsonAsync($"{Api}/mensais/{bill.Id}/paid", new RecurringBillMarkPaidDTO
        {
            Expense = new RecurringBillPaymentExpenseDTO
            {
                Date = Date, Description = "Council Tax", Value = 120m, CategoryId = MercadoId, PaymentSourceBankId = ChaseId
            }
        });

        await ShouldBeRefusedAsInactive(response);
    }

    [Fact]
    [Trait("AC", "P60-F06-admin-accounts-page-06")]
    public async Task PotsWithdrawal_ThroughAnInactiveBank_IsRejectedWith400()
    {
        await DeactivateChaseAsync();

        var response = await Client.PostAsJsonAsync($"{Api}/accounts/{ReservaId}/withdrawals", new PotWithdrawalRequestDTO
        {
            PotId = InvestimentoBucketId,
            Amount = 50m,
            Date = Date,
            Description = "Treat",
            Confirmed = true,
            BankId = ChaseId,
            CategoryId = MercadoId
        });

        await ShouldBeRefusedAsInactive(response);
    }

    [Fact]
    [Trait("AC", "P60-F06-admin-accounts-page-06")]
    public async Task MarkCardStatementPaid_FromAnInactiveBank_IsRejectedWith400()
    {
        await CreateAsync<ExpenseDTO>("expenses", Expense(bankId: null, creditCardId: VisaId));
        var statementId = (await VisaOctoberStatementAsync()).Id;
        await DeactivateChaseAsync();

        var response = await Client.PostAsJsonAsync(
            $"{Api}/card-statements/{statementId}/mark-paid", new MarkCardStatementPaidDTO { PaymentSourceBankId = ChaseId });
        var after = await VisaOctoberStatementAsync();

        await ShouldBeRefusedAsInactive(response);
        using (new AssertionScope())
        {
            statementId.Should().NotBeNull();
            after.Id.Should().Be(statementId);
            after.IsPaid.Should().BeFalse();
        }
    }

    private async Task<AccountStatementDTO> VisaOctoberStatementAsync() =>
        (await Client.GetFromJsonAsync<AccountDetailDTO>($"{Api}/accounts/{VisaId}/2026/10"))!.ViewedStatement!;

    [Fact]
    [Trait("AC", "P60-F06-admin-accounts-page-06")]
    public async Task PostAccountTransfer_ToAnInactiveBank_IsRejectedWith400()
    {
        await DeactivateChaseAsync();

        var response = await Client.PostAsJsonAsync($"{Api}/accounts/transfers", new AccountTransferCreateDTO
        {
            Date = Date, SourceAccountId = BarclaysId, DestinationAccountId = ChaseId, Amount = 10m
        });

        await ShouldBeRefusedAsInactive(response);
    }

    [Fact]
    [Trait("AC", "P60-F06-admin-accounts-page-06")]
    public async Task PostAccountTransfer_FromAnInactiveBank_IsRejectedWith400()
    {
        await DeactivateChaseAsync();

        var response = await Client.PostAsJsonAsync($"{Api}/accounts/transfers", new AccountTransferCreateDTO
        {
            Date = Date, SourceAccountId = ChaseId, DestinationAccountId = BarclaysId, Amount = 10m
        });

        await ShouldBeRefusedAsInactive(response);
    }
}
