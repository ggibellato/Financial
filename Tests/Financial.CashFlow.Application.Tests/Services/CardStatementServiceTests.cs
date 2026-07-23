using Financial.CashFlow.Application.DTOs;
using Financial.CashFlow.Application.Interfaces;
using Financial.CashFlow.Application.Services;
using Financial.CashFlow.Domain.Entities;
using Financial.CashFlow.Domain.Enums;
using FluentAssertions;

namespace Financial.CashFlow.Application.Tests.Services;

public class CardStatementServiceTests
{
    [Fact]
    public void Constructor_WithNullRepository_Throws()
    {
        Action act = () => new CardStatementService(null!);
        act.Should().Throw<ArgumentNullException>().WithParameterName("repository");
    }

    [Fact]
    public async Task GetStatementsForMonthAsync_FirstCall_GeneratesExactlyFiveUnpaidStatements()
    {
        var repository = new StubCashFlowRepository();
        var service = new CardStatementService(repository);

        var result = await service.GetStatementsForMonthAsync(2026, 7);

        result.Should().HaveCount(5);
        result.Should().OnlyContain(s => !s.IsPaid);
        repository.Statements.Should().HaveCount(5);
    }

    [Fact]
    public async Task GetStatementsForMonthAsync_SecondCallSameMonth_DoesNotCreateDuplicates()
    {
        var repository = new StubCashFlowRepository();
        var service = new CardStatementService(repository);

        await service.GetStatementsForMonthAsync(2026, 7);
        var result = await service.GetStatementsForMonthAsync(2026, 7);

        result.Should().HaveCount(5);
        repository.Statements.Should().HaveCount(5);
        repository.SaveChangesCallCount.Should().Be(1);
    }

    [Fact]
    public async Task GetStatementsForMonthAsync_OutstandingTotalSumsThatMonthsTaggedExpensesForTheCard()
    {
        var repository = new StubCashFlowRepository();
        repository.Expenses.Add(Expense.Create(new DateOnly(2026, 7, 10), "Charge 1", 30m, Category.Mercado, PaymentSource.Barclays, CreditCard.BarclaysPlatinumVisa8003));
        repository.Expenses.Add(Expense.Create(new DateOnly(2026, 7, 15), "Charge 2", 20m, Category.Mercado, PaymentSource.Barclays, CreditCard.BarclaysPlatinumVisa8003));
        repository.Expenses.Add(Expense.Create(new DateOnly(2026, 8, 1), "Other month", 100m, Category.Mercado, PaymentSource.Barclays, CreditCard.BarclaysPlatinumVisa8003));
        repository.Expenses.Add(Expense.Create(new DateOnly(2026, 7, 12), "Other card", 999m, Category.Mercado, PaymentSource.Barclays, CreditCard.BaAmex));
        var service = new CardStatementService(repository);

        var result = await service.GetStatementsForMonthAsync(2026, 7);

        result.Should().ContainSingle(s => s.Card == "BarclaysPlatinumVisa8003" && s.OutstandingTotal == 50m);
    }

    [Fact]
    public async Task GetStatementsForMonthAsync_WhenStatementIsPaid_OutstandingTotalIsZeroDespiteTaggedExpenses()
    {
        var repository = new StubCashFlowRepository();
        repository.Expenses.Add(Expense.Create(new DateOnly(2026, 7, 10), "Charge", 30m, Category.Mercado, PaymentSource.Barclays, CreditCard.BarclaysPlatinumVisa8003));
        var service = new CardStatementService(repository);
        await service.GetStatementsForMonthAsync(2026, 7);
        var statement = repository.Statements.Single(s => s.Card == CreditCard.BarclaysPlatinumVisa8003);
        await service.MarkStatementPaidAsync(statement.Id);

        var result = await service.GetStatementsForMonthAsync(2026, 7);

        result.Should().ContainSingle(s => s.Card == "BarclaysPlatinumVisa8003" && s.OutstandingTotal == 0m && s.IsPaid);
    }

    [Fact]
    public async Task MarkStatementPaidAsync_SetsIsPaidAndZeroesOutstandingTotal()
    {
        var repository = new StubCashFlowRepository();
        repository.Expenses.Add(Expense.Create(new DateOnly(2026, 7, 10), "Charge", 30m, Category.Mercado, PaymentSource.Barclays, CreditCard.BarclaysPlatinumVisa8003));
        var service = new CardStatementService(repository);
        await service.GetStatementsForMonthAsync(2026, 7);
        var statement = repository.Statements.Single(s => s.Card == CreditCard.BarclaysPlatinumVisa8003);

        var result = await service.MarkStatementPaidAsync(statement.Id);

        result.IsPaid.Should().BeTrue();
        result.OutstandingTotal.Should().Be(0m);
    }

    [Fact]
    public async Task MarkStatementPaidAsync_CalledAgainOnAlreadyPaidStatement_IsANoOpThatStillSucceeds()
    {
        var repository = new StubCashFlowRepository();
        var service = new CardStatementService(repository);
        await service.GetStatementsForMonthAsync(2026, 7);
        var statement = repository.Statements.First();
        await service.MarkStatementPaidAsync(statement.Id);

        var result = await service.MarkStatementPaidAsync(statement.Id);

        result.IsPaid.Should().BeTrue();
        repository.SaveChangesCallCount.Should().Be(2);
    }

    [Fact]
    public async Task MarkStatementPaidAsync_WhenSaveFails_RollsBackIsPaid()
    {
        var repository = new StubCashFlowRepository();
        var service = new CardStatementService(repository);
        await service.GetStatementsForMonthAsync(2026, 7);
        var statement = repository.Statements.First();
        repository.ThrowOnNextSave = true;

        var act = async () => await service.MarkStatementPaidAsync(statement.Id);

        await act.Should().ThrowAsync<InvalidOperationException>();
        statement.IsPaid.Should().BeFalse();
    }

    [Fact]
    public async Task MarkStatementPaidAsync_WithUnknownId_ThrowsKeyNotFoundException()
    {
        var service = new CardStatementService(new StubCashFlowRepository());

        var act = async () => await service.MarkStatementPaidAsync(Guid.NewGuid());

        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    private sealed class StubCashFlowRepository : ICashFlowRepository
    {
        public List<CardStatement> Statements { get; } = new();
        public List<Expense> Expenses { get; } = new();
        public int SaveChangesCallCount { get; private set; }
        public bool ThrowOnNextSave { get; set; }

        public IEnumerable<Expense> GetExpenses() => Expenses;
        public void AddExpense(Expense expense) => Expenses.Add(expense);
        public void DeleteExpense(Guid id) { }

        public IEnumerable<ReserveMovement> GetReserveMovements() => Array.Empty<ReserveMovement>();
        public void AddReserveMovement(ReserveMovement movement) { }
        public void DeleteReserveMovement(Guid id) { }

        public IEnumerable<CardStatement> GetCardStatements() => Statements;
        public void AddCardStatement(CardStatement statement) => Statements.Add(statement);

        public IEnumerable<RecurringBill> GetRecurringBills() => Array.Empty<RecurringBill>();
        public void AddRecurringBill(RecurringBill bill) { }
        public void DeleteRecurringBill(Guid id) { }

        public IEnumerable<MaeLedgerEntry> GetMaeLedgerEntries() => Array.Empty<MaeLedgerEntry>();
        public void AddMaeLedgerEntry(MaeLedgerEntry entry) { }
        public void DeleteMaeLedgerEntry(Guid id) { }

        public IEnumerable<InvestmentSnapshot> GetInvestmentSnapshots() => Array.Empty<InvestmentSnapshot>();
        public void AddInvestmentSnapshot(InvestmentSnapshot snapshot) { }

        public Task SaveChangesAsync()
        {
            SaveChangesCallCount++;
            if (ThrowOnNextSave)
            {
                ThrowOnNextSave = false;
                throw new InvalidOperationException("Simulated save failure.");
            }

            return Task.CompletedTask;
        }
    }
}
