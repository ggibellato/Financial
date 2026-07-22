using Financial.CashFlow.Domain.Entities;
using Financial.CashFlow.Domain.Enums;
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
        var expense = Expense.Create(
            new DateOnly(2026, 7, 15),
            "Weekly groceries",
            54.32m,
            Category.Mercado,
            PaymentSource.Barclays,
            CreditCard.BarclaysPlatinumVisa8003);
        var reserveMovement = ReserveMovement.Create(ReserveBucket.Investimento, 866.67m, new DateOnly(2026, 7, 1), "Monthly income split");
        var cardStatement = CardStatement.Create();
        var recurringBillTemplate = RecurringBillTemplate.Create(10, "INSS", 850m, Area.Brasil, "Direct debit", "12345678901", 1412m);
        var recurringBillInstance = RecurringBillInstance.Create(Guid.NewGuid(), 2026, 7, 850m);
        var maeLedgerEntry = MaeLedgerEntry.Create(new DateOnly(2026, 7, 15), "School supplies", "Note", Currency.BRL, 350m, 51.23m);
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

        var resultExpense = result.Expenses.Should().ContainSingle().Which;
        resultExpense.Id.Should().Be(expense.Id);
        resultExpense.Date.Should().Be(expense.Date);
        resultExpense.Description.Should().Be(expense.Description);
        resultExpense.Value.Should().Be(expense.Value);
        resultExpense.Category.Should().Be(expense.Category);
        resultExpense.PaymentSource.Should().Be(expense.PaymentSource);
        resultExpense.CardTag.Should().Be(expense.CardTag);
        var resultMovement = result.ReserveMovements.Should().ContainSingle().Which;
        resultMovement.Id.Should().Be(reserveMovement.Id);
        resultMovement.Bucket.Should().Be(reserveMovement.Bucket);
        resultMovement.Amount.Should().Be(reserveMovement.Amount);
        resultMovement.Date.Should().Be(reserveMovement.Date);
        resultMovement.Description.Should().Be(reserveMovement.Description);
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
