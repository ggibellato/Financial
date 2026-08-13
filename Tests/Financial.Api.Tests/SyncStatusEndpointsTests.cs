using System.Net;
using System.Net.Http.Json;
using Financial.Api.DTOs;
using Financial.CashFlow.Application.Interfaces;
using Financial.CashFlow.Domain.Entities;
using Financial.Shared.Infrastructure.Sync;
using FluentAssertions;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Financial.Api.Tests;

public class SyncStatusEndpointsTests
{
    [Fact]
    public async Task GetSyncStatus_ReturnsOk_WithBothContextsIdle()
    {
        await using var factory = new ApiTestFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/v1/financial/sync-status");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var status = await response.Content.ReadFromJsonAsync<SyncStatusResponseDTO>();
        status!.CashFlow.State.Should().Be("Idle");
        status.CashFlow.LastError.Should().BeNull();
        status.CashFlow.LastSuccessfulSaveUtc.Should().BeNull();
        status.Investment.State.Should().Be("Idle");
        status.Investment.LastError.Should().BeNull();
        status.Investment.LastSuccessfulSaveUtc.Should().BeNull();
    }

    [Fact]
    public async Task GetSyncStatus_JsonUsesCamelCasePropertyNames()
    {
        await using var factory = new ApiTestFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/v1/financial/sync-status");
        var json = await response.Content.ReadAsStringAsync();

        json.Should().Contain("\"cashFlow\"");
        json.Should().Contain("\"investment\"");
        json.Should().Contain("\"state\"");
        json.Should().Contain("\"lastError\"");
        json.Should().Contain("\"lastSuccessfulSaveUtc\"");
    }

    [Fact]
    public async Task GetSyncStatus_WhenCashFlowRepositoryIsFailed_ReflectsFailedStateForCashFlowOnly()
    {
        var failedTimestamp = new DateTime(2026, 8, 13, 9, 12, 4, DateTimeKind.Utc);
        await using var factory = new ApiTestFactory().WithWebHostBuilder(builder =>
            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<ICashFlowRepository>();
                services.AddSingleton<ICashFlowRepository>(
                    new FailedSyncStatusCashFlowRepository("Drive request failed with a transient status (503 ServiceUnavailable).", failedTimestamp));
            }));
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/v1/financial/sync-status");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var status = await response.Content.ReadFromJsonAsync<SyncStatusResponseDTO>();
        status!.CashFlow.State.Should().Be("Failed");
        status.CashFlow.LastError.Should().Be("Drive request failed with a transient status (503 ServiceUnavailable).");
        status.CashFlow.LastSuccessfulSaveUtc.Should().Be(failedTimestamp);
        status.Investment.State.Should().Be("Idle");
    }

    private sealed class FailedSyncStatusCashFlowRepository : ICashFlowRepository, ISyncStatusProvider
    {
        private readonly string _lastError;
        private readonly DateTime _lastSuccessfulSaveUtc;

        public FailedSyncStatusCashFlowRepository(string lastError, DateTime lastSuccessfulSaveUtc)
        {
            _lastError = lastError;
            _lastSuccessfulSaveUtc = lastSuccessfulSaveUtc;
        }

        public SyncStatus GetStatus() => new(SyncState.Failed, _lastError, _lastSuccessfulSaveUtc);

        public Task FlushAsync() => Task.CompletedTask;

        public Task SaveChangesAsync() => throw new NotImplementedException();
        public IEnumerable<Expense> GetExpenses() => throw new NotImplementedException();
        public void AddExpense(Expense expense) => throw new NotImplementedException();
        public void DeleteExpense(Guid id) => throw new NotImplementedException();
        public IEnumerable<ReserveMovement> GetReserveMovements() => throw new NotImplementedException();
        public void AddReserveMovement(ReserveMovement movement) => throw new NotImplementedException();
        public void DeleteReserveMovement(Guid id) => throw new NotImplementedException();
        public IEnumerable<CardStatement> GetCardStatements() => throw new NotImplementedException();
        public void AddCardStatement(CardStatement statement) => throw new NotImplementedException();
        public IEnumerable<RecurringBill> GetRecurringBills() => throw new NotImplementedException();
        public void AddRecurringBill(RecurringBill bill) => throw new NotImplementedException();
        public void DeleteRecurringBill(Guid id) => throw new NotImplementedException();
        public IEnumerable<MaeLedgerEntry> GetMaeLedgerEntries() => throw new NotImplementedException();
        public void AddMaeLedgerEntry(MaeLedgerEntry entry) => throw new NotImplementedException();
        public void DeleteMaeLedgerEntry(Guid id) => throw new NotImplementedException();
        public IEnumerable<InvestmentSnapshot> GetInvestmentSnapshots() => throw new NotImplementedException();
        public void AddInvestmentSnapshot(InvestmentSnapshot snapshot) => throw new NotImplementedException();
        public IEnumerable<InvestmentAccount> GetInvestmentAccounts() => throw new NotImplementedException();
        public void AddInvestmentAccount(InvestmentAccount account) => throw new NotImplementedException();
        public IEnumerable<Bank> GetBanks() => throw new NotImplementedException();
        public IEnumerable<IncomeSource> GetIncomeSources() => throw new NotImplementedException();
        public IEnumerable<ReserveBucket> GetReserveBuckets() => throw new NotImplementedException();
        public IEnumerable<CreditCard> GetCreditCards() => throw new NotImplementedException();
        public IEnumerable<Category> GetCategories() => throw new NotImplementedException();
        public IEnumerable<Income> GetIncomes() => throw new NotImplementedException();
        public void AddIncome(Income income) => throw new NotImplementedException();
        public void DeleteIncome(Guid id) => throw new NotImplementedException();
        public IEnumerable<Transfer> GetTransfers() => throw new NotImplementedException();
        public void AddTransfer(Transfer transfer) => throw new NotImplementedException();
        public void UpdateTransfer(Transfer transfer) => throw new NotImplementedException();
        public void DeleteTransfer(Guid id) => throw new NotImplementedException();
        public IEnumerable<BalanceAdjustment> GetBalanceAdjustments() => throw new NotImplementedException();
        public void AddBalanceAdjustment(BalanceAdjustment adjustment) => throw new NotImplementedException();
        public void UpdateBalanceAdjustment(BalanceAdjustment adjustment) => throw new NotImplementedException();
        public void DeleteBalanceAdjustment(Guid id) => throw new NotImplementedException();
    }
}
