using Financial.CashFlow.Domain.Entities;
using Financial.CashFlow.Domain.Enums;
using FluentAssertions;
using ReserveBucketEnum = Financial.CashFlow.Domain.Enums.ReserveBucket;

namespace Financial.CashFlow.Domain.Tests;

public class CashFlowDataTests
{
    private static readonly Bank Chase = Bank.Create("Chase", roundUpEnabled: true);
    private static readonly Bank Barclays = Bank.Create("Barclays", roundUpEnabled: false);
    private static readonly Bank Trading212 = Bank.Create("Trading212", roundUpEnabled: true);
    private static readonly IncomeSource Lottery = IncomeSource.Create("Lottery", IncomeGroup.NonReportable);
    private static readonly InvestmentAccount ChaseSaveAccount =
        InvestmentAccount.Create("ChaseSave", isActive: true, isLiability: false);

    [Fact]
    public void Create_StartsWithAllCollectionsEmpty()
    {
        var data = CashFlowData.Create();

        data.Expenses.Should().BeEmpty();
        data.ReserveMovements.Should().BeEmpty();
        data.CardStatements.Should().BeEmpty();
        data.RecurringBills.Should().BeEmpty();
        data.MaeLedgerEntries.Should().BeEmpty();
        data.InvestmentSnapshots.Should().BeEmpty();
        data.InvestmentAccounts.Should().BeEmpty();
        data.Banks.Should().BeEmpty();
        data.IncomeSources.Should().BeEmpty();
        data.Incomes.Should().BeEmpty();
        data.Transfers.Should().BeEmpty();
        data.BalanceAdjustments.Should().BeEmpty();
    }

    [Fact]
    public void AddBank_AddsOnlyToBanksCollection()
    {
        var data = CashFlowData.Create();

        data.AddBank(Bank.Create("Barclays", roundUpEnabled: false));

        data.Banks.Should().ContainSingle();
        data.Expenses.Should().BeEmpty();
    }

    [Fact]
    public void AddIncomeSource_AddsOnlyToIncomeSourcesCollection()
    {
        var data = CashFlowData.Create();

        data.AddIncomeSource(IncomeSource.Create("Gleison", IncomeGroup.Salary));

        data.IncomeSources.Should().ContainSingle();
        data.Expenses.Should().BeEmpty();
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
        Expense.Create(new DateOnly(2026, 7, 1), "Test expense", 10m, Category.Casa, Chase, null);

    [Fact]
    public void AddReserveMovement_AddsOnlyToReserveMovementsCollection()
    {
        var data = CashFlowData.Create();

        data.AddReserveMovement(CreateReserveMovement());

        data.ReserveMovements.Should().ContainSingle();
        data.Expenses.Should().BeEmpty();
    }

    [Fact]
    public void RemoveReserveMovement_RemovesOnlyTheMatchingMovement()
    {
        var data = CashFlowData.Create();
        var toKeep = CreateReserveMovement();
        var toRemove = CreateReserveMovement();
        data.AddReserveMovement(toKeep);
        data.AddReserveMovement(toRemove);

        data.RemoveReserveMovement(toRemove.Id);

        data.ReserveMovements.Should().ContainSingle().Which.Id.Should().Be(toKeep.Id);
    }

    [Fact]
    public void RemoveReserveMovement_WithUnknownId_LeavesCollectionUnchanged()
    {
        var data = CashFlowData.Create();
        data.AddReserveMovement(CreateReserveMovement());

        data.RemoveReserveMovement(Guid.NewGuid());

        data.ReserveMovements.Should().ContainSingle();
    }

    private static ReserveMovement CreateReserveMovement() =>
        ReserveMovement.Create(ReserveBucketEnum.Investimento, 10m, new DateOnly(2026, 7, 1), "Test movement");

    [Fact]
    public void AddCardStatement_AddsOnlyToCardStatementsCollection()
    {
        var data = CashFlowData.Create();

        data.AddCardStatement(CardStatement.Create(CreditCard.BarclaysPlatinumVisa8003, 2026, 7));

        data.CardStatements.Should().ContainSingle();
        data.Expenses.Should().BeEmpty();
    }

    [Fact]
    public void AddRecurringBill_AddsOnlyToRecurringBillsCollection()
    {
        var data = CashFlowData.Create();

        data.AddRecurringBill(CreateRecurringBill());

        data.RecurringBills.Should().ContainSingle();
        data.Expenses.Should().BeEmpty();
    }

    [Fact]
    public void RemoveRecurringBill_RemovesOnlyTheMatchingBill()
    {
        var data = CashFlowData.Create();
        var toKeep = CreateRecurringBill();
        var toRemove = CreateRecurringBill();
        data.AddRecurringBill(toKeep);
        data.AddRecurringBill(toRemove);

        data.RemoveRecurringBill(toRemove.Id);

        data.RecurringBills.Should().ContainSingle().Which.Id.Should().Be(toKeep.Id);
    }

    [Fact]
    public void RemoveRecurringBill_WithUnknownId_LeavesCollectionUnchanged()
    {
        var data = CashFlowData.Create();
        data.AddRecurringBill(CreateRecurringBill());

        data.RemoveRecurringBill(Guid.NewGuid());

        data.RecurringBills.Should().ContainSingle();
    }

    private static RecurringBill CreateRecurringBill() =>
        RecurringBill.Create(10, "Test bill", 100m, Area.Brasil, string.Empty, null, null);

    [Fact]
    public void AddMaeLedgerEntry_AddsOnlyToMaeLedgerEntriesCollection()
    {
        var data = CashFlowData.Create();

        data.AddMaeLedgerEntry(CreateMaeLedgerEntry());

        data.MaeLedgerEntries.Should().ContainSingle();
        data.Expenses.Should().BeEmpty();
    }

    private static MaeLedgerEntry CreateMaeLedgerEntry() =>
        MaeLedgerEntry.Create(new DateOnly(2026, 7, 1), "Test entry", string.Empty, Currency.BRL, 100m, 15m);

    [Fact]
    public void RemoveMaeLedgerEntry_RemovesOnlyTheMatchingEntry()
    {
        var data = CashFlowData.Create();
        var toKeep = CreateMaeLedgerEntry();
        var toRemove = CreateMaeLedgerEntry();
        data.AddMaeLedgerEntry(toKeep);
        data.AddMaeLedgerEntry(toRemove);

        data.RemoveMaeLedgerEntry(toRemove.Id);

        data.MaeLedgerEntries.Should().ContainSingle().Which.Id.Should().Be(toKeep.Id);
    }

    [Fact]
    public void RemoveMaeLedgerEntry_WithUnknownId_LeavesCollectionUnchanged()
    {
        var data = CashFlowData.Create();
        data.AddMaeLedgerEntry(CreateMaeLedgerEntry());

        data.RemoveMaeLedgerEntry(Guid.NewGuid());

        data.MaeLedgerEntries.Should().ContainSingle();
    }

    [Fact]
    public void AddInvestmentSnapshot_AddsOnlyToInvestmentSnapshotsCollection()
    {
        var data = CashFlowData.Create();

        data.AddInvestmentSnapshot(InvestmentSnapshot.Create(ChaseSaveAccount, 2026, 7, 100m));

        data.InvestmentSnapshots.Should().ContainSingle();
        data.Expenses.Should().BeEmpty();
    }

    [Fact]
    public void AddInvestmentAccount_AddsOnlyToInvestmentAccountsCollection()
    {
        var data = CashFlowData.Create();

        data.AddInvestmentAccount(InvestmentAccount.Create("ChaseSave", isActive: true, isLiability: false));

        data.InvestmentAccounts.Should().ContainSingle();
        data.Expenses.Should().BeEmpty();
    }

    [Fact]
    public void AddIncome_AddsOnlyToIncomesCollection()
    {
        var data = CashFlowData.Create();

        data.AddIncome(CreateIncome());

        data.Incomes.Should().ContainSingle();
        data.Expenses.Should().BeEmpty();
    }

    [Fact]
    public void RemoveIncome_RemovesOnlyTheMatchingIncome()
    {
        var data = CashFlowData.Create();
        var toKeep = CreateIncome();
        var toRemove = CreateIncome();
        data.AddIncome(toKeep);
        data.AddIncome(toRemove);

        data.RemoveIncome(toRemove.Id);

        data.Incomes.Should().ContainSingle().Which.Id.Should().Be(toKeep.Id);
    }

    [Fact]
    public void RemoveIncome_WithUnknownId_LeavesCollectionUnchanged()
    {
        var data = CashFlowData.Create();
        data.AddIncome(CreateIncome());

        data.RemoveIncome(Guid.NewGuid());

        data.Incomes.Should().ContainSingle();
    }

    private static Income CreateIncome() =>
        Income.Create(new DateOnly(2026, 7, 1), Lottery, null, 10m, Chase);

    [Fact]
    public void AddTransfer_AddsOnlyToTransfersCollection()
    {
        var data = CashFlowData.Create();

        data.AddTransfer(CreateTransfer());

        data.Transfers.Should().ContainSingle();
        data.Expenses.Should().BeEmpty();
    }

    [Fact]
    public void UpdateTransfer_ReplacesTheMatchingEntry()
    {
        var data = CashFlowData.Create();
        var transfer = CreateTransfer();
        data.AddTransfer(transfer);
        transfer.UpdateDetails(new DateOnly(2026, 8, 1), Chase, Trading212, 250m, "Updated");

        data.UpdateTransfer(transfer);

        data.Transfers.Should().ContainSingle().Which.Amount.Should().Be(250m);
    }

    [Fact]
    public void UpdateTransfer_WithUnknownId_LeavesCollectionUnchanged()
    {
        var data = CashFlowData.Create();
        data.AddTransfer(CreateTransfer());
        var unknown = CreateTransfer();

        data.UpdateTransfer(unknown);

        data.Transfers.Should().ContainSingle().Which.Id.Should().NotBe(unknown.Id);
    }

    [Fact]
    public void RemoveTransfer_RemovesOnlyTheMatchingTransfer()
    {
        var data = CashFlowData.Create();
        var toKeep = CreateTransfer();
        var toRemove = CreateTransfer();
        data.AddTransfer(toKeep);
        data.AddTransfer(toRemove);

        data.RemoveTransfer(toRemove.Id);

        data.Transfers.Should().ContainSingle().Which.Id.Should().Be(toKeep.Id);
    }

    [Fact]
    public void RemoveTransfer_WithUnknownId_LeavesCollectionUnchanged()
    {
        var data = CashFlowData.Create();
        data.AddTransfer(CreateTransfer());

        data.RemoveTransfer(Guid.NewGuid());

        data.Transfers.Should().ContainSingle();
    }

    private static Transfer CreateTransfer() =>
        Transfer.Create(new DateOnly(2026, 7, 1), Barclays, Trading212, 500m, "Test transfer");

    [Fact]
    public void AddBalanceAdjustment_AddsOnlyToBalanceAdjustmentsCollection()
    {
        var data = CashFlowData.Create();

        data.AddBalanceAdjustment(CreateBalanceAdjustment());

        data.BalanceAdjustments.Should().ContainSingle();
        data.Expenses.Should().BeEmpty();
    }

    [Fact]
    public void UpdateBalanceAdjustment_ReplacesTheMatchingEntry()
    {
        var data = CashFlowData.Create();
        var adjustment = CreateBalanceAdjustment();
        data.AddBalanceAdjustment(adjustment);
        adjustment.UpdateDetails(new DateOnly(2026, 8, 1), 250m, 10m, "Updated");

        data.UpdateBalanceAdjustment(adjustment);

        data.BalanceAdjustments.Should().ContainSingle().Which.TargetBalance.Should().Be(250m);
    }

    [Fact]
    public void UpdateBalanceAdjustment_WithUnknownId_LeavesCollectionUnchanged()
    {
        var data = CashFlowData.Create();
        data.AddBalanceAdjustment(CreateBalanceAdjustment());
        var unknown = CreateBalanceAdjustment();

        data.UpdateBalanceAdjustment(unknown);

        data.BalanceAdjustments.Should().ContainSingle().Which.Id.Should().NotBe(unknown.Id);
    }

    [Fact]
    public void RemoveBalanceAdjustment_RemovesOnlyTheMatchingAdjustment()
    {
        var data = CashFlowData.Create();
        var toKeep = CreateBalanceAdjustment();
        var toRemove = CreateBalanceAdjustment();
        data.AddBalanceAdjustment(toKeep);
        data.AddBalanceAdjustment(toRemove);

        data.RemoveBalanceAdjustment(toRemove.Id);

        data.BalanceAdjustments.Should().ContainSingle().Which.Id.Should().Be(toKeep.Id);
    }

    [Fact]
    public void RemoveBalanceAdjustment_WithUnknownId_LeavesCollectionUnchanged()
    {
        var data = CashFlowData.Create();
        data.AddBalanceAdjustment(CreateBalanceAdjustment());

        data.RemoveBalanceAdjustment(Guid.NewGuid());

        data.BalanceAdjustments.Should().ContainSingle();
    }

    private static BalanceAdjustment CreateBalanceAdjustment() =>
        BalanceAdjustment.Create(new DateOnly(2026, 7, 1), Barclays, 100m, 0m, "Test adjustment");
}
