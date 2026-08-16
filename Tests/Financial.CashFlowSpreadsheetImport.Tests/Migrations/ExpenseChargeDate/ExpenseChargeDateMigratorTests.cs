using Financial.CashFlow.Domain.Entities;
using Financial.CashFlow.Domain.Enums;
using Financial.CashFlow.Infrastructure.Tools.CashFlowSpreadsheetImport.Migrations.ExpenseChargeDate;
using FluentAssertions;
using FluentAssertions.Execution;
using CreditCardEntity = Financial.CashFlow.Domain.Entities.CreditCard;

namespace Financial.CashFlowSpreadsheetImport.Tests.Migrations.ExpenseChargeDate;

public class ExpenseChargeDateMigratorTests
{
    // Legacy records carry both CardTag and PaymentSource with ChargeDate/InvoiceDate absent -
    // a shape that can no longer be produced through Expense.Create, so build it the way
    // deserialization of pre-F01 data would (only public API + reflection to null the new fields).
    private static Expense LegacyUnpaidCharge(CashFlowData data, DateOnly date, CreditCardEntity card, decimal value = 30m)
    {
        var expense = Expense.Create(date, "Charge", value, Category.Create("Mercado"), null, card);
        NullOutMigratedFields(expense);
        data.AddExpense(expense);
        return expense;
    }

    private static Expense LegacySettledCharge(CashFlowData data, DateOnly date, CreditCardEntity card, Bank paymentSource, decimal value = 30m)
    {
        var expense = Expense.Create(date, "Charge", value, Category.Create("Mercado"), null, card);
        typeof(Expense).GetProperty(nameof(Expense.PaymentSourceBank))!.SetMethod!.Invoke(expense, new object?[] { paymentSource });
        NullOutMigratedFields(expense);
        data.AddExpense(expense);
        return expense;
    }

    private static void NullOutMigratedFields(Expense expense)
    {
        typeof(Expense).GetProperty(nameof(Expense.ChargeDate))!.SetMethod!.Invoke(expense, new object?[] { null });
        typeof(Expense).GetProperty(nameof(Expense.InvoiceDate))!.SetMethod!.Invoke(expense, new object?[] { null });
    }

    private static string BuildLegacyRawJson(params (Guid Id, DateOnly SettledAt)[] settlements)
    {
        var entries = settlements.Select(s => $$"""{ "Id": "{{s.Id}}", "SettledAt": "{{s.SettledAt:yyyy-MM-dd}}" }""");
        return $$"""{ "Expenses": [{{string.Join(",", entries)}}] }""";
    }

    [Fact]
    public void Migrate_UnpaidCardCharge_SetsChargeDateEqualToDateAndDefaultsInvoiceDate()
    {
        var data = CashFlowData.Create();
        var card = CreditCardEntity.Create("BarclaysPlatinumVisa8003");
        var expense = LegacyUnpaidCharge(data, new DateOnly(2026, 7, 10), card);

        var summary = ExpenseChargeDateMigrator.Migrate(data, legacyRawJson: null);

        using (new AssertionScope())
        {
            expense.ChargeDate.Should().Be(new DateOnly(2026, 7, 10));
            expense.InvoiceDate.Should().Be(new DateOnly(2026, 7, 1));
            expense.Date.Should().Be(new DateOnly(2026, 7, 10));
            summary.NewlyMigratedUnpaidCount.Should().Be(1);
        }
    }

    [Fact]
    public void Migrate_SettledChargeWithMatchingStatementAndRecoverableSettledAt_MigratesAllThreeFields()
    {
        var data = CashFlowData.Create();
        var card = CreditCardEntity.Create("BarclaysPlatinumVisa8003");
        var statement = CardStatement.Create(card, 2026, 7);
        statement.MarkPaid();
        data.AddCardStatement(statement);
        var expense = LegacySettledCharge(data, new DateOnly(2026, 7, 10), card, Bank.Create("Barclays", roundUpEnabled: false));
        var legacyRawJson = BuildLegacyRawJson((expense.Id, new DateOnly(2026, 8, 3)));

        var summary = ExpenseChargeDateMigrator.Migrate(data, legacyRawJson);

        using (new AssertionScope())
        {
            expense.ChargeDate.Should().Be(new DateOnly(2026, 7, 10));
            expense.InvoiceDate.Should().Be(new DateOnly(2026, 7, 1));
            expense.Date.Should().Be(new DateOnly(2026, 8, 3));
            summary.NewlyMigratedSettledCount.Should().Be(1);
        }
    }

    [Fact]
    public void Migrate_SettledChargeWithNoMatchingStatement_FlagsForReviewAndLeavesUntouched()
    {
        var data = CashFlowData.Create();
        var card = CreditCardEntity.Create("BarclaysPlatinumVisa8003");
        var expense = LegacySettledCharge(data, new DateOnly(2026, 7, 10), card, Bank.Create("Barclays", roundUpEnabled: false));
        var legacyRawJson = BuildLegacyRawJson((expense.Id, new DateOnly(2026, 8, 3)));

        var summary = ExpenseChargeDateMigrator.Migrate(data, legacyRawJson);

        using (new AssertionScope())
        {
            expense.ChargeDate.Should().BeNull();
            summary.MissingStatementExpenses.Should().ContainSingle().Which.Id.Should().Be(expense.Id);
        }
    }

    [Fact]
    public void Migrate_SettledChargeWithNoRecoverableSettledAt_FlagsForReviewAndLeavesUntouched()
    {
        var data = CashFlowData.Create();
        var card = CreditCardEntity.Create("BarclaysPlatinumVisa8003");
        var statement = CardStatement.Create(card, 2026, 7);
        statement.MarkPaid();
        data.AddCardStatement(statement);
        var expense = LegacySettledCharge(data, new DateOnly(2026, 7, 10), card, Bank.Create("Barclays", roundUpEnabled: false));

        var summary = ExpenseChargeDateMigrator.Migrate(data, legacyRawJson: null);

        using (new AssertionScope())
        {
            expense.ChargeDate.Should().BeNull();
            summary.MissingSettledAtExpenses.Should().ContainSingle().Which.Id.Should().Be(expense.Id);
        }
    }

    [Fact]
    public void Migrate_BankExpense_NeverModified()
    {
        var data = CashFlowData.Create();
        var expense = Expense.Create(new DateOnly(2026, 7, 10), "Groceries", 20m, Category.Create("Mercado"), Bank.Create("Chase", roundUpEnabled: true), null);
        data.AddExpense(expense);

        var summary = ExpenseChargeDateMigrator.Migrate(data, legacyRawJson: null);

        using (new AssertionScope())
        {
            expense.ChargeDate.Should().BeNull();
            expense.InvoiceDate.Should().BeNull();
            data.Expenses.Should().ContainSingle();
            summary.BankExpenseCount.Should().Be(1);
        }
    }

    [Fact]
    public void Migrate_SecondRunOnAlreadyMigratedData_ChangesNothing()
    {
        var data = CashFlowData.Create();
        var barclaysCard = CreditCardEntity.Create("BarclaysPlatinumVisa8003");
        var baAmexCard = CreditCardEntity.Create("BaAmex");
        var statement = CardStatement.Create(barclaysCard, 2026, 7);
        statement.MarkPaid();
        data.AddCardStatement(statement);
        var unpaid = LegacyUnpaidCharge(data, new DateOnly(2026, 7, 10), baAmexCard);
        var settled = LegacySettledCharge(data, new DateOnly(2026, 7, 12), barclaysCard, Bank.Create("Barclays", roundUpEnabled: false));
        var legacyRawJson = BuildLegacyRawJson((settled.Id, new DateOnly(2026, 8, 3)));

        ExpenseChargeDateMigrator.Migrate(data, legacyRawJson);
        var afterFirst = data.Expenses
            .Select(e => (e.Id, e.ChargeDate, e.InvoiceDate, e.Date))
            .ToList();

        var secondSummary = ExpenseChargeDateMigrator.Migrate(data, legacyRawJson: null);

        using (new AssertionScope())
        {
            data.Expenses.Select(e => (e.Id, e.ChargeDate, e.InvoiceDate, e.Date)).Should().Equal(afterFirst);
            secondSummary.NewlyMigratedUnpaidCount.Should().Be(0);
            secondSummary.NewlyMigratedSettledCount.Should().Be(0);
            secondSummary.AlreadyMigratedCount.Should().Be(2);
            unpaid.ChargeDate.Should().NotBeNull();
            settled.ChargeDate.Should().NotBeNull();
        }
    }
}
