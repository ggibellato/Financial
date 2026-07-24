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
            null,
            CreditCard.BarclaysPlatinumVisa8003);
        expense.Settle(PaymentSource.Barclays, new DateOnly(2026, 7, 31));
        var reserveMovement = ReserveMovement.Create(ReserveBucket.Investimento, 866.67m, new DateOnly(2026, 7, 1), "Monthly income split");
        var cardStatement = CardStatement.Create(CreditCard.BarclaysPlatinumVisa8003, 2026, 7);
        var recurringBill = RecurringBill.Create(10, "INSS", 850m, Area.Brasil, "Direct debit", "12345678901", 1621m);
        var maeLedgerEntry = MaeLedgerEntry.Create(new DateOnly(2026, 7, 15), "School supplies", "Note", Currency.BRL, 350m, 51.23m);
        var investmentSnapshot = InvestmentSnapshot.Create(InvestmentAccount.PlatinumVisa8003, 2026, 7, 1250.00m);

        original.AddExpense(expense);
        original.AddReserveMovement(reserveMovement);
        original.AddCardStatement(cardStatement);
        original.AddRecurringBill(recurringBill);
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
        resultExpense.SettledAt.Should().Be(expense.SettledAt);
        var resultMovement = result.ReserveMovements.Should().ContainSingle().Which;
        resultMovement.Id.Should().Be(reserveMovement.Id);
        resultMovement.Bucket.Should().Be(reserveMovement.Bucket);
        resultMovement.Amount.Should().Be(reserveMovement.Amount);
        resultMovement.Date.Should().Be(reserveMovement.Date);
        resultMovement.Description.Should().Be(reserveMovement.Description);
        result.CardStatements.Should().ContainSingle().Which.Id.Should().Be(cardStatement.Id);
        result.RecurringBills.Should().ContainSingle().Which.Id.Should().Be(recurringBill.Id);
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
        result.RecurringBills.Should().BeEmpty();
        result.MaeLedgerEntries.Should().BeEmpty();
        result.InvestmentSnapshots.Should().BeEmpty();
    }
}
