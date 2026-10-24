using System.Net;
using System.Net.Http.Json;
using Financial.CashFlow.Application.DTOs;
using FluentAssertions;
using FluentAssertions.Execution;
using Microsoft.AspNetCore.Mvc;

namespace Financial.Api.Tests.Acceptance;

[Trait("Category", "Integration")]
public class InactiveAccountRecordsAcceptanceTests : ApiEndpointTests
{
    private const string Api = "/api/v1/financial";
    private const string InactiveMessage = "Chase is inactive.";
    private static readonly Guid BarclaysId = Guid.Parse("8f3b1c1a-2e3a-4b1a-9a7f-100000000001");
    private static readonly Guid ChaseId = Guid.Parse("8f3b1c1a-2e3a-4b1a-9a7f-100000000003");
    private static readonly Guid VisaId = Guid.Parse("8f3b1c1a-2e3a-4b1a-9a7f-500000000001");
    private static readonly Guid GleisonId = Guid.Parse("8f3b1c1a-2e3a-4b1a-9a7f-000000000001");
    private static readonly Guid InvestimentoBucketId = Guid.Parse("8f3b1c1a-2e3a-4b1a-9a7f-300000000001");
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

    private static async Task ShouldBeRefusedAsInactive(HttpResponseMessage response)
    {
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();

        using (new AssertionScope())
        {
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            problem!.Detail.Should().Be(InactiveMessage);
        }
    }

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
    public async Task ExpenseOnAnInactiveBank_SameAccountEditAndDelete_StillWork()
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
    public async Task IncomeOnAnInactiveBank_SameAccountEditAndDelete_StillWork()
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
            $"{Api}/banks/{ChaseId}/adjustments", new BalanceAdjustmentCreateDTO { Date = Date, TargetBalance = 10m });

        await ShouldBeRefusedAsInactive(response);
    }

    [Fact]
    [Trait("AC", "P60-F06-admin-accounts-page-06")]
    public async Task AdjustmentOnAnInactiveBank_Delete_StillWorks()
    {
        var adjustment = await CreateAsync<BalanceAdjustmentDTO>(
            $"banks/{ChaseId}/adjustments", new BalanceAdjustmentCreateDTO { Date = Date, TargetBalance = 10m });
        await DeactivateChaseAsync();

        var delete = await Client.DeleteAsync($"{Api}/banks/{ChaseId}/adjustments/{adjustment.Id}");

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
    public async Task ReserveWithdrawal_ThroughAnInactiveBank_IsRejectedWith400()
    {
        await DeactivateChaseAsync();

        var response = await Client.PostAsJsonAsync($"{Api}/reserve/withdrawals", new WithdrawalRequestDTO
        {
            BucketId = InvestimentoBucketId,
            Amount = 50m,
            Date = Date,
            Description = "Treat",
            Confirmed = true,
            PaymentSourceBankId = ChaseId,
            ExpenseCategoryId = MercadoId
        });

        await ShouldBeRefusedAsInactive(response);
    }

    [Fact]
    [Trait("AC", "P60-F06-admin-accounts-page-06")]
    public async Task MarkCardStatementPaid_FromAnInactiveBank_IsRejectedWith400()
    {
        await CreateAsync<ExpenseDTO>("expenses", Expense(bankId: null, creditCardId: VisaId));
        var statements = await Client.GetFromJsonAsync<List<CardStatementDTO>>($"{Api}/card-statements/2026/10");
        var statement = statements!.First(s => s.CreditCardName == "BarclaysPlatinumVisa8003");
        await DeactivateChaseAsync();

        var response = await Client.PostAsJsonAsync(
            $"{Api}/card-statements/{statement.Id}/mark-paid", new MarkCardStatementPaidDTO { PaymentSourceBankId = ChaseId });

        await ShouldBeRefusedAsInactive(response);
    }

    [Fact]
    [Trait("AC", "P60-F06-admin-accounts-page-06")]
    public async Task PostTransfer_ToAnInactiveBank_IsRejectedWith400()
    {
        await DeactivateChaseAsync();

        var response = await Client.PostAsJsonAsync($"{Api}/transfers", new TransferCreateDTO
        {
            Date = Date, SourceBankId = BarclaysId, DestinationBankId = ChaseId, Amount = 10m
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
