using Financial.CashFlow.Domain.Entities;
using Financial.CashFlow.Domain.Enums;
using Financial.CashFlow.Infrastructure.Persistence;
using FluentAssertions;

namespace Financial.CashFlow.Infrastructure.Tests.Persistence;

public class CashFlowSerializerAdapterTests
{
    [Fact]
    public void SerializeThenDeserialize_RoundTripsAllNineCollections()
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
        expense.Settle("Barclays", new DateOnly(2026, 7, 31));
        var reserveMovement = ReserveMovement.Create(ReserveBucket.Investimento, 866.67m, new DateOnly(2026, 7, 1), "Monthly income split");
        var cardStatement = CardStatement.Create(CreditCard.BarclaysPlatinumVisa8003, 2026, 7);
        var recurringBill = RecurringBill.Create(10, "INSS", 850m, Area.Brasil, "Direct debit", "12345678901", 1621m);
        var maeLedgerEntry = MaeLedgerEntry.Create(new DateOnly(2026, 7, 15), "School supplies", "Note", Currency.BRL, 350m, 51.23m);
        var investmentSnapshot = InvestmentSnapshot.Create("PlatinumVisa8003", 2026, 7, 1250.00m);
        var investmentAccount = InvestmentAccount.Create("PlatinumVisa8003", isActive: true, isLiability: true);
        var bank = Bank.Create("Barclays", roundUpEnabled: false);
        bank.SetOpeningBalance(1250.75m, new DateOnly(2026, 7, 1));
        var incomeSource = IncomeSource.Create("Gleison", IncomeGroup.Salary);
        var income = Income.Create(new DateOnly(2026, 7, 25), "Gleison", 3200.00m, 2450.00m, "Barclays");
        var transfer = Transfer.Create(new DateOnly(2026, 7, 25), "Barclays", "Trading212", 500.00m, "Round-up top-up");
        var balanceAdjustment = BalanceAdjustment.Create(new DateOnly(2026, 7, 25), "Barclays", 2340.17m, -4.20m, "Matched against July statement");

        original.AddExpense(expense);
        original.AddReserveMovement(reserveMovement);
        original.AddCardStatement(cardStatement);
        original.AddRecurringBill(recurringBill);
        original.AddMaeLedgerEntry(maeLedgerEntry);
        original.AddInvestmentSnapshot(investmentSnapshot);
        original.AddInvestmentAccount(investmentAccount);
        original.AddBank(bank);
        original.AddIncomeSource(incomeSource);
        original.AddIncome(income);
        original.AddTransfer(transfer);
        original.AddBalanceAdjustment(balanceAdjustment);

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
        resultExpense.ChargeDate.Should().Be(expense.ChargeDate);
        resultExpense.InvoiceDate.Should().Be(expense.InvoiceDate);
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
        var resultInvestmentAccount = result.InvestmentAccounts.Should().ContainSingle().Which;
        resultInvestmentAccount.Id.Should().Be(investmentAccount.Id);
        resultInvestmentAccount.Name.Should().Be(investmentAccount.Name);
        resultInvestmentAccount.IsActive.Should().Be(investmentAccount.IsActive);
        resultInvestmentAccount.IsLiability.Should().Be(investmentAccount.IsLiability);
        var resultBank = result.Banks.Should().ContainSingle().Which;
        resultBank.Name.Should().Be(bank.Name);
        resultBank.RoundUpEnabled.Should().Be(bank.RoundUpEnabled);
        resultBank.OpeningBalance.Should().Be(bank.OpeningBalance);
        resultBank.OpeningBalanceDate.Should().Be(bank.OpeningBalanceDate);
        var resultIncomeSource = result.IncomeSources.Should().ContainSingle().Which;
        resultIncomeSource.Id.Should().Be(incomeSource.Id);
        resultIncomeSource.Name.Should().Be(incomeSource.Name);
        resultIncomeSource.IsActive.Should().Be(incomeSource.IsActive);
        resultIncomeSource.Group.Should().Be(incomeSource.Group);
        var resultIncome = result.Incomes.Should().ContainSingle().Which;
        resultIncome.Id.Should().Be(income.Id);
        resultIncome.Date.Should().Be(income.Date);
        resultIncome.IncomeSource.Should().Be(income.IncomeSource);
        resultIncome.GrossValue.Should().Be(income.GrossValue);
        resultIncome.NetValue.Should().Be(income.NetValue);
        resultIncome.Bank.Should().Be(income.Bank);
        var resultTransfer = result.Transfers.Should().ContainSingle().Which;
        resultTransfer.Id.Should().Be(transfer.Id);
        resultTransfer.Date.Should().Be(transfer.Date);
        resultTransfer.SourceBank.Should().Be(transfer.SourceBank);
        resultTransfer.DestinationBank.Should().Be(transfer.DestinationBank);
        resultTransfer.Amount.Should().Be(transfer.Amount);
        resultTransfer.Note.Should().Be(transfer.Note);
        var resultBalanceAdjustment = result.BalanceAdjustments.Should().ContainSingle().Which;
        resultBalanceAdjustment.Id.Should().Be(balanceAdjustment.Id);
        resultBalanceAdjustment.Date.Should().Be(balanceAdjustment.Date);
        resultBalanceAdjustment.Bank.Should().Be(balanceAdjustment.Bank);
        resultBalanceAdjustment.TargetBalance.Should().Be(balanceAdjustment.TargetBalance);
        resultBalanceAdjustment.Delta.Should().Be(balanceAdjustment.Delta);
        resultBalanceAdjustment.Note.Should().Be(balanceAdjustment.Note);
    }

    [Fact]
    public void Serialize_ProducesCompactJsonWithoutIndentation()
    {
        var serializer = new CashFlowSerializerAdapter();
        var data = CashFlowData.Create();

        var json = serializer.Serialize(data);

        json.Should().NotContain("\n");
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
        result.InvestmentAccounts.Should().BeEmpty();
        result.Banks.Should().BeEmpty();
        result.IncomeSources.Should().BeEmpty();
        result.Incomes.Should().BeEmpty();
        result.Transfers.Should().BeEmpty();
        result.BalanceAdjustments.Should().BeEmpty();
    }
}
