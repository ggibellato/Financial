using Financial.CashFlow.Domain.Entities;
using Financial.CashFlow.Infrastructure.Persistence;
using FluentAssertions;

namespace Financial.CashFlow.Infrastructure.Tests.Persistence;

public class CashFlowSerializerAdapterTests
{
    [Fact]
    public void SerializeThenDeserialize_RoundTripsAllSevenCollections()
    {
        var serializer = new CashFlowSerializerAdapter();
        var original = CashFlowData.Create();
        var expense = Expense.Create();
        var reserveMovement = ReserveMovement.Create();
        var cardStatement = CardStatement.Create();
        var recurringBillTemplate = RecurringBillTemplate.Create();
        var recurringBillInstance = RecurringBillInstance.Create();
        var maeLedgerEntry = MaeLedgerEntry.Create();
        var investmentSnapshot = InvestmentSnapshot.Create();

        original.AddExpense(expense);
        original.AddReserveMovement(reserveMovement);
        original.AddCardStatement(cardStatement);
        original.AddRecurringBillTemplate(recurringBillTemplate);
        original.AddRecurringBillInstance(recurringBillInstance);
        original.AddMaeLedgerEntry(maeLedgerEntry);
        original.AddInvestmentSnapshot(investmentSnapshot);

        var json = serializer.Serialize(original);
        var result = serializer.Deserialize(json);

        result.Expenses.Should().ContainSingle().Which.Id.Should().Be(expense.Id);
        result.ReserveMovements.Should().ContainSingle().Which.Id.Should().Be(reserveMovement.Id);
        result.CardStatements.Should().ContainSingle().Which.Id.Should().Be(cardStatement.Id);
        result.RecurringBillTemplates.Should().ContainSingle().Which.Id.Should().Be(recurringBillTemplate.Id);
        result.RecurringBillInstances.Should().ContainSingle().Which.Id.Should().Be(recurringBillInstance.Id);
        result.MaeLedgerEntries.Should().ContainSingle().Which.Id.Should().Be(maeLedgerEntry.Id);
        result.InvestmentSnapshots.Should().ContainSingle().Which.Id.Should().Be(investmentSnapshot.Id);
    }

    [Fact]
    public void SerializeThenDeserialize_WhenAllCollectionsEmpty_RoundTripsEmpty()
    {
        var serializer = new CashFlowSerializerAdapter();
        var original = CashFlowData.Create();

        var json = serializer.Serialize(original);
        var result = serializer.Deserialize(json);

        result.Expenses.Should().BeEmpty();
        result.ReserveMovements.Should().BeEmpty();
        result.CardStatements.Should().BeEmpty();
        result.RecurringBillTemplates.Should().BeEmpty();
        result.RecurringBillInstances.Should().BeEmpty();
        result.MaeLedgerEntries.Should().BeEmpty();
        result.InvestmentSnapshots.Should().BeEmpty();
    }
}
