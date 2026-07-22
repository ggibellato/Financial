using Financial.CashFlow.Domain.Entities;
using Financial.CashFlow.Domain.Enums;
using FluentAssertions;

namespace Financial.CashFlow.Domain.Tests;

public class CashFlowDataTests
{
    [Fact]
    public void Create_StartsWithAllSevenCollectionsEmpty()
    {
        var data = CashFlowData.Create();

        data.Expenses.Should().BeEmpty();
        data.ReserveMovements.Should().BeEmpty();
        data.CardStatements.Should().BeEmpty();
        data.RecurringBillTemplates.Should().BeEmpty();
        data.RecurringBillInstances.Should().BeEmpty();
        data.MaeLedgerEntries.Should().BeEmpty();
        data.InvestmentSnapshots.Should().BeEmpty();
    }

    [Fact]
    public void AddExpense_AddsOnlyToExpensesCollection()
    {
        var data = CashFlowData.Create();

        data.AddExpense(CreateExpense());

        data.Expenses.Should().ContainSingle();
        data.ReserveMovements.Should().BeEmpty();
    }

    [Fact]
    public void RemoveExpense_RemovesOnlyTheMatchingExpense()
    {
        var data = CashFlowData.Create();
        var toKeep = CreateExpense();
        var toRemove = CreateExpense();
        data.AddExpense(toKeep);
        data.AddExpense(toRemove);

        data.RemoveExpense(toRemove.Id);

        data.Expenses.Should().ContainSingle().Which.Id.Should().Be(toKeep.Id);
    }

    [Fact]
    public void RemoveExpense_WithUnknownId_LeavesCollectionUnchanged()
    {
        var data = CashFlowData.Create();
        data.AddExpense(CreateExpense());

        data.RemoveExpense(Guid.NewGuid());

        data.Expenses.Should().ContainSingle();
    }

    private static Expense CreateExpense() =>
        Expense.Create(new DateOnly(2026, 7, 1), "Test expense", 10m, Category.Casa, PaymentSource.Chase, null);

    [Fact]
    public void AddReserveMovement_AddsOnlyToReserveMovementsCollection()
    {
        var data = CashFlowData.Create();

        data.AddReserveMovement(ReserveMovement.Create());

        data.ReserveMovements.Should().ContainSingle();
        data.Expenses.Should().BeEmpty();
    }

    [Fact]
    public void AddCardStatement_AddsOnlyToCardStatementsCollection()
    {
        var data = CashFlowData.Create();

        data.AddCardStatement(CardStatement.Create());

        data.CardStatements.Should().ContainSingle();
        data.Expenses.Should().BeEmpty();
    }

    [Fact]
    public void AddRecurringBillTemplate_AddsOnlyToRecurringBillTemplatesCollection()
    {
        var data = CashFlowData.Create();

        data.AddRecurringBillTemplate(RecurringBillTemplate.Create());

        data.RecurringBillTemplates.Should().ContainSingle();
        data.RecurringBillInstances.Should().BeEmpty();
    }

    [Fact]
    public void AddRecurringBillInstance_AddsOnlyToRecurringBillInstancesCollection()
    {
        var data = CashFlowData.Create();

        data.AddRecurringBillInstance(RecurringBillInstance.Create());

        data.RecurringBillInstances.Should().ContainSingle();
        data.RecurringBillTemplates.Should().BeEmpty();
    }

    [Fact]
    public void AddMaeLedgerEntry_AddsOnlyToMaeLedgerEntriesCollection()
    {
        var data = CashFlowData.Create();

        data.AddMaeLedgerEntry(MaeLedgerEntry.Create());

        data.MaeLedgerEntries.Should().ContainSingle();
        data.Expenses.Should().BeEmpty();
    }

    [Fact]
    public void AddInvestmentSnapshot_AddsOnlyToInvestmentSnapshotsCollection()
    {
        var data = CashFlowData.Create();

        data.AddInvestmentSnapshot(InvestmentSnapshot.Create());

        data.InvestmentSnapshots.Should().ContainSingle();
        data.Expenses.Should().BeEmpty();
    }
}
