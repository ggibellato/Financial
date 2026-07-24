using Financial.CashFlow.Application.DTOs;
using Financial.CashFlow.Application.Interfaces;
using Financial.CashFlow.Application.Services;
using Financial.CashFlow.Domain.Entities;
using Financial.CashFlow.Domain.Enums;
using FluentAssertions;

namespace Financial.CashFlow.Application.Tests.Services;

public class CardStatementServiceTests
{
    private static MarkStatementPaidDTO PaidBy(string? paymentSource) => new() { PaymentSource = paymentSource };

    private static Expense AddCharge(
        StubCashFlowRepository repository, DateOnly date, decimal value, CreditCard card)
    {
        var expense = Expense.Create(date, "Charge", value, Category.Mercado, null, card);
        repository.Expenses.Add(expense);
        return expense;
    }

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
    public async Task GetStatementsForMonthAsync_OutstandingTotalSumsThatMonthsChargesForTheCard()
    {
        var repository = new StubCashFlowRepository();
        AddCharge(repository, new DateOnly(2026, 7, 10), 30m, CreditCard.BarclaysPlatinumVisa8003);
        AddCharge(repository, new DateOnly(2026, 7, 15), 20m, CreditCard.BarclaysPlatinumVisa8003);
        AddCharge(repository, new DateOnly(2026, 8, 1), 100m, CreditCard.BarclaysPlatinumVisa8003);
        AddCharge(repository, new DateOnly(2026, 7, 12), 999m, CreditCard.BaAmex);
        var service = new CardStatementService(repository);

        var result = await service.GetStatementsForMonthAsync(2026, 7);

        result.Should().ContainSingle(s => s.Card == "BarclaysPlatinumVisa8003" && s.OutstandingTotal == 50m);
    }

    [Fact]
    public async Task GetStatementsForMonthAsync_ExcludesSettledAndImmediateExpensesFromOutstandingTotal()
    {
        var repository = new StubCashFlowRepository();
        var settled = AddCharge(repository, new DateOnly(2026, 7, 10), 30m, CreditCard.BarclaysPlatinumVisa8003);
        settled.Settle(PaymentSource.Barclays, new DateOnly(2026, 7, 20));
        AddCharge(repository, new DateOnly(2026, 7, 11), 20m, CreditCard.BarclaysPlatinumVisa8003);
        repository.Expenses.Add(Expense.Create(new DateOnly(2026, 7, 12), "Immediate", 5m, Category.Casa, PaymentSource.Barclays, null));
        var service = new CardStatementService(repository);

        var result = await service.GetStatementsForMonthAsync(2026, 7);

        result.Should().ContainSingle(s => s.Card == "BarclaysPlatinumVisa8003" && s.OutstandingTotal == 20m);
    }

    [Fact]
    public async Task MarkStatementPaidAsync_SettlesEveryChargeForTheCardMonthWithBankAndToday()
    {
        var repository = new StubCashFlowRepository();
        var first = AddCharge(repository, new DateOnly(2026, 7, 10), 30m, CreditCard.BarclaysPlatinumVisa8003);
        var second = AddCharge(repository, new DateOnly(2026, 7, 15), 20m, CreditCard.BarclaysPlatinumVisa8003);
        var otherMonth = AddCharge(repository, new DateOnly(2026, 8, 1), 100m, CreditCard.BarclaysPlatinumVisa8003);
        var otherCard = AddCharge(repository, new DateOnly(2026, 7, 12), 999m, CreditCard.BaAmex);
        var service = new CardStatementService(repository);
        await service.GetStatementsForMonthAsync(2026, 7);
        var statement = repository.Statements.Single(s => s.Card == CreditCard.BarclaysPlatinumVisa8003 && s.Month == 7);

        var result = await service.MarkStatementPaidAsync(statement.Id, PaidBy("Trading212"));

        result.IsPaid.Should().BeTrue();
        result.OutstandingTotal.Should().Be(0m);
        var today = DateOnly.FromDateTime(DateTime.Today);
        foreach (var expense in new[] { first, second })
        {
            expense.PaymentStatus.Should().Be(ExpensePaymentStatus.CreditCardSettled);
            expense.PaymentSource.Should().Be(PaymentSource.Trading212);
            expense.SettledAt.Should().Be(today);
        }

        otherMonth.PaymentStatus.Should().Be(ExpensePaymentStatus.CreditCardCharge);
        otherCard.PaymentStatus.Should().Be(ExpensePaymentStatus.CreditCardCharge);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("NotABank")]
    public async Task MarkStatementPaidAsync_WithMissingOrUnknownPaymentSource_ThrowsWithoutChangingState(string? paymentSource)
    {
        var repository = new StubCashFlowRepository();
        var charge = AddCharge(repository, new DateOnly(2026, 7, 10), 30m, CreditCard.BarclaysPlatinumVisa8003);
        var service = new CardStatementService(repository);
        await service.GetStatementsForMonthAsync(2026, 7);
        var statement = repository.Statements.Single(s => s.Card == CreditCard.BarclaysPlatinumVisa8003);
        var savesBefore = repository.SaveChangesCallCount;

        var act = async () => await service.MarkStatementPaidAsync(statement.Id, PaidBy(paymentSource));

        await act.Should().ThrowAsync<ArgumentException>().WithMessage("*Payment source*not recognized*");
        statement.IsPaid.Should().BeFalse();
        charge.PaymentStatus.Should().Be(ExpensePaymentStatus.CreditCardCharge);
        repository.SaveChangesCallCount.Should().Be(savesBefore);
    }

    [Fact]
    public async Task MarkStatementPaidAsync_CalledAgainOnAlreadyPaidStatement_IsANoOpThatStillSucceeds()
    {
        var repository = new StubCashFlowRepository();
        var service = new CardStatementService(repository);
        await service.GetStatementsForMonthAsync(2026, 7);
        var statement = repository.Statements.First();
        await service.MarkStatementPaidAsync(statement.Id, PaidBy("Barclays"));
        var savesBefore = repository.SaveChangesCallCount;

        var result = await service.MarkStatementPaidAsync(statement.Id, PaidBy("Chase"));

        result.IsPaid.Should().BeTrue();
        repository.SaveChangesCallCount.Should().Be(savesBefore);
    }

    [Fact]
    public async Task MarkStatementPaidAsync_WhenSaveFails_RollsBackStatementAndCascadedExpenses()
    {
        var repository = new StubCashFlowRepository();
        var charge = AddCharge(repository, new DateOnly(2026, 7, 10), 30m, CreditCard.BarclaysPlatinumVisa8003);
        var service = new CardStatementService(repository);
        await service.GetStatementsForMonthAsync(2026, 7);
        var statement = repository.Statements.Single(s => s.Card == CreditCard.BarclaysPlatinumVisa8003);
        repository.ThrowOnNextSave = true;

        var act = async () => await service.MarkStatementPaidAsync(statement.Id, PaidBy("Barclays"));

        await act.Should().ThrowAsync<InvalidOperationException>();
        statement.IsPaid.Should().BeFalse();
        charge.PaymentStatus.Should().Be(ExpensePaymentStatus.CreditCardCharge);
        charge.PaymentSource.Should().BeNull();
        charge.SettledAt.Should().BeNull();
    }

    [Fact]
    public async Task MarkStatementPaidAsync_WithUnknownId_ThrowsKeyNotFoundException()
    {
        var service = new CardStatementService(new StubCashFlowRepository());

        var act = async () => await service.MarkStatementPaidAsync(Guid.NewGuid(), PaidBy("Barclays"));

        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task UnmarkStatementPaidAsync_RevertsEverySettledExpenseForTheCardMonth()
    {
        var repository = new StubCashFlowRepository();
        var first = AddCharge(repository, new DateOnly(2026, 7, 10), 30m, CreditCard.BarclaysPlatinumVisa8003);
        var second = AddCharge(repository, new DateOnly(2026, 7, 15), 20m, CreditCard.BarclaysPlatinumVisa8003);
        var service = new CardStatementService(repository);
        await service.GetStatementsForMonthAsync(2026, 7);
        var statement = repository.Statements.Single(s => s.Card == CreditCard.BarclaysPlatinumVisa8003);
        await service.MarkStatementPaidAsync(statement.Id, PaidBy("Barclays"));

        var result = await service.UnmarkStatementPaidAsync(statement.Id);

        result.IsPaid.Should().BeFalse();
        result.OutstandingTotal.Should().Be(50m);
        foreach (var expense in new[] { first, second })
        {
            expense.PaymentStatus.Should().Be(ExpensePaymentStatus.CreditCardCharge);
            expense.PaymentSource.Should().BeNull();
            expense.SettledAt.Should().BeNull();
        }
    }

    [Fact]
    public async Task UnmarkStatementPaidAsync_OnAlreadyUnpaidStatement_IsANoOpThatStillSucceeds()
    {
        var repository = new StubCashFlowRepository();
        var service = new CardStatementService(repository);
        await service.GetStatementsForMonthAsync(2026, 7);
        var statement = repository.Statements.First();
        var savesBefore = repository.SaveChangesCallCount;

        var result = await service.UnmarkStatementPaidAsync(statement.Id);

        result.IsPaid.Should().BeFalse();
        repository.SaveChangesCallCount.Should().Be(savesBefore);
    }

    [Fact]
    public async Task UnmarkStatementPaidAsync_WithNoSettledExpenses_StillFlipsStatementToUnpaid()
    {
        var repository = new StubCashFlowRepository();
        var service = new CardStatementService(repository);
        await service.GetStatementsForMonthAsync(2026, 7);
        var statement = repository.Statements.First();
        await service.MarkStatementPaidAsync(statement.Id, PaidBy("Barclays"));

        var result = await service.UnmarkStatementPaidAsync(statement.Id);

        result.IsPaid.Should().BeFalse();
        statement.IsPaid.Should().BeFalse();
    }

    [Fact]
    public async Task UnmarkStatementPaidAsync_WhenSaveFails_RollsBackStatementAndCascadedExpenses()
    {
        var repository = new StubCashFlowRepository();
        var charge = AddCharge(repository, new DateOnly(2026, 7, 10), 30m, CreditCard.BarclaysPlatinumVisa8003);
        var service = new CardStatementService(repository);
        await service.GetStatementsForMonthAsync(2026, 7);
        var statement = repository.Statements.Single(s => s.Card == CreditCard.BarclaysPlatinumVisa8003);
        await service.MarkStatementPaidAsync(statement.Id, PaidBy("Trading212"));
        var settledAt = charge.SettledAt;
        repository.ThrowOnNextSave = true;

        var act = async () => await service.UnmarkStatementPaidAsync(statement.Id);

        await act.Should().ThrowAsync<InvalidOperationException>();
        statement.IsPaid.Should().BeTrue();
        charge.PaymentStatus.Should().Be(ExpensePaymentStatus.CreditCardSettled);
        charge.PaymentSource.Should().Be(PaymentSource.Trading212);
        charge.SettledAt.Should().Be(settledAt);
    }

    [Fact]
    public async Task UnmarkStatementPaidAsync_WithUnknownId_ThrowsKeyNotFoundException()
    {
        var service = new CardStatementService(new StubCashFlowRepository());

        var act = async () => await service.UnmarkStatementPaidAsync(Guid.NewGuid());

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
