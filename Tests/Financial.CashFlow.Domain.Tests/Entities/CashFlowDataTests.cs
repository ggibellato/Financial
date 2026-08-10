using Financial.CashFlow.Domain.Entities;
using Financial.CashFlow.Domain.Enums;
using FluentAssertions;
using ReserveBucketEntity = Financial.CashFlow.Domain.Entities.ReserveBucket;

namespace Financial.CashFlow.Domain.Tests;

public class CashFlowDataTests
{
    private static readonly Bank Chase = Bank.Create("Chase", roundUpEnabled: true);
    private static readonly Bank Barclays = Bank.Create("Barclays", roundUpEnabled: false);
    private static readonly Bank Trading212 = Bank.Create("Trading212", roundUpEnabled: true);
    private static readonly IncomeSource Lottery = IncomeSource.Create("Lottery", IncomeGroup.NonReportable);
    private static readonly InvestmentAccount ChaseSaveAccount =
        InvestmentAccount.Create("ChaseSave", isActive: true, isLiability: false);

    private readonly CashFlowData _suv;

    public CashFlowDataTests() {
        _suv = CashFlowData.Create();
    }

    [Fact]
    public void Create_StartsWithAllCollectionsEmpty()
    {
        _suv.Expenses.Should().BeEmpty();
        _suv.ReserveMovements.Should().BeEmpty();
        _suv.CardStatements.Should().BeEmpty();
        _suv.RecurringBills.Should().BeEmpty();
        _suv.MaeLedgerEntries.Should().BeEmpty();
        _suv.InvestmentSnapshots.Should().BeEmpty();
        _suv.InvestmentAccounts.Should().BeEmpty();
        _suv.Banks.Should().BeEmpty();
        _suv.IncomeSources.Should().BeEmpty();
        _suv.ReserveBuckets.Should().BeEmpty();
        _suv.Incomes.Should().BeEmpty();
        _suv.Transfers.Should().BeEmpty();
        _suv.BalanceAdjustments.Should().BeEmpty();
        _suv.CreditCards.Should().BeEmpty();
    }

    [Fact]
    public void AddBank_AddsOnlyToBanksCollection()
    {
        _suv.AddBank(Bank.Create("Barclays", roundUpEnabled: false));
        CheckCollectionCounts(new CheckItemsQuantity(Banks: 1));
    }

    [Fact]
    public void AddIncomeSource_AddsOnlyToIncomeSourcesCollection()
    {
        _suv.AddIncomeSource(IncomeSource.Create("Gleison", IncomeGroup.Salary));

        CheckCollectionCounts(new CheckItemsQuantity(IncomeSources: 1));
    }

    [Fact]
    public void AddReserveBucket_AddsOnlyToReserveBucketsCollection()
    {
        _suv.AddReserveBucket(ReserveBucketEntity.Create("Investimento", 33.33m));

        CheckCollectionCounts(new CheckItemsQuantity(ReserveBuckets: 1));
    }

    [Fact]
    public void AddCreditCard_AddsOnlyToCreditCardsCollection()
    {
        _suv.AddCreditCard(Domain.Entities.CreditCard.Create("VISA 1", isActive: true));

        CheckCollectionCounts(new CheckItemsQuantity(CreditCards: 1));
    }

    [Fact]
    public void AddExpense_AddsOnlyToExpensesCollection()
    {
        _suv.AddExpense(CreateExpense());

        CheckCollectionCounts(new CheckItemsQuantity(Expenses: 1));
    }

    [Fact]
    public void RemoveExpense_RemovesOnlyTheMatchingExpense()
    {
        var toKeep = CreateExpense();
        var toRemove = CreateExpense();
        _suv.AddExpense(toKeep);
        _suv.AddExpense(toRemove);

        _suv.RemoveExpense(toRemove.Id);

        _suv.Expenses.Should().ContainSingle().Which.Id.Should().Be(toKeep.Id);
    }

    [Fact]
    public void RemoveExpense_WithUnknownId_LeavesCollectionUnchanged()
    {
        _suv.AddExpense(CreateExpense());
        
        _suv.RemoveExpense(Guid.NewGuid());

        _suv.Expenses.Should().ContainSingle();
    }

    private static Expense CreateExpense() =>
        Expense.Create(new DateOnly(2026, 7, 1), "Test expense", 10m, Category.Casa, Chase, null);

    [Fact]
    public void AddReserveMovement_AddsOnlyToReserveMovementsCollection()
    {
        _suv.AddReserveMovement(CreateReserveMovement());

        CheckCollectionCounts(new CheckItemsQuantity(ReserveMovements: 1));
    }

    [Fact]
    public void RemoveReserveMovement_RemovesOnlyTheMatchingMovement()
    {
        var toKeep = CreateReserveMovement();
        var toRemove = CreateReserveMovement();
        _suv.AddReserveMovement(toKeep);
        _suv.AddReserveMovement(toRemove);

        _suv.RemoveReserveMovement(toRemove.Id);

        _suv.ReserveMovements.Should().ContainSingle().Which.Id.Should().Be(toKeep.Id);
    }

    [Fact]
    public void RemoveReserveMovement_WithUnknownId_LeavesCollectionUnchanged()
    {
        _suv.AddReserveMovement(CreateReserveMovement());

        _suv.RemoveReserveMovement(Guid.NewGuid());

        _suv.ReserveMovements.Should().ContainSingle();
    }

    private static readonly ReserveBucketEntity TestBucket = ReserveBucketEntity.Create("Investimento", 33.33m);

    private static ReserveMovement CreateReserveMovement() =>
        ReserveMovement.Create(TestBucket, 10m, new DateOnly(2026, 7, 1), "Test movement");

    [Fact]
    public void AddCardStatement_AddsOnlyToCardStatementsCollection()
    {
        _suv.AddCardStatement(CardStatement.Create(Enums.CreditCard.BarclaysPlatinumVisa8003, 2026, 7));

        CheckCollectionCounts(new CheckItemsQuantity(CardStatements: 1));
    }

    [Fact]
    public void AddRecurringBill_AddsOnlyToRecurringBillsCollection()
    {
        _suv.AddRecurringBill(CreateRecurringBill());

        CheckCollectionCounts(new CheckItemsQuantity(RecurringBills: 1));
    }

    [Fact]
    public void RemoveRecurringBill_RemovesOnlyTheMatchingBill()
    {
        var toKeep = CreateRecurringBill();
        var toRemove = CreateRecurringBill();
        _suv.AddRecurringBill(toKeep);
        _suv.AddRecurringBill(toRemove);

        _suv.RemoveRecurringBill(toRemove.Id);

        _suv.RecurringBills.Should().ContainSingle().Which.Id.Should().Be(toKeep.Id);
    }

    [Fact]
    public void RemoveRecurringBill_WithUnknownId_LeavesCollectionUnchanged()
    {
        _suv.AddRecurringBill(CreateRecurringBill());

        _suv.RemoveRecurringBill(Guid.NewGuid());

        _suv.RecurringBills.Should().ContainSingle();
    }

    private static RecurringBill CreateRecurringBill() =>
        RecurringBill.Create(10, "Test bill", 100m, Area.Brasil, string.Empty, null, null);

    [Fact]
    public void AddMaeLedgerEntry_AddsOnlyToMaeLedgerEntriesCollection()
    {
        _suv.AddMaeLedgerEntry(CreateMaeLedgerEntry());

        CheckCollectionCounts(new CheckItemsQuantity(MaeLedgerEntries: 1));
    }

    private static MaeLedgerEntry CreateMaeLedgerEntry() =>
        MaeLedgerEntry.Create(new DateOnly(2026, 7, 1), "Test entry", string.Empty, Currency.BRL, 100m, 15m);

    [Fact]
    public void RemoveMaeLedgerEntry_RemovesOnlyTheMatchingEntry()
    {
        var toKeep = CreateMaeLedgerEntry();
        var toRemove = CreateMaeLedgerEntry();
        _suv.AddMaeLedgerEntry(toKeep);
        _suv.AddMaeLedgerEntry(toRemove);

        _suv.RemoveMaeLedgerEntry(toRemove.Id);

        _suv.MaeLedgerEntries.Should().ContainSingle().Which.Id.Should().Be(toKeep.Id);
    }

    [Fact]
    public void RemoveMaeLedgerEntry_WithUnknownId_LeavesCollectionUnchanged()
    {
        _suv.AddMaeLedgerEntry(CreateMaeLedgerEntry());

        _suv.RemoveMaeLedgerEntry(Guid.NewGuid());

        _suv.MaeLedgerEntries.Should().ContainSingle();
    }

    [Fact]
    public void AddInvestmentSnapshot_AddsOnlyToInvestmentSnapshotsCollection()
    {
        _suv.AddInvestmentSnapshot(InvestmentSnapshot.Create(ChaseSaveAccount, 2026, 7, 100m));

        CheckCollectionCounts(new CheckItemsQuantity(InvestmentSnapshots: 1));
    }

    [Fact]
    public void AddInvestmentAccount_AddsOnlyToInvestmentAccountsCollection()
    {
        _suv.AddInvestmentAccount(InvestmentAccount.Create("ChaseSave", isActive: true, isLiability: false));

        CheckCollectionCounts(new CheckItemsQuantity(InvestmentAccounts: 1));
    }

    [Fact]
    public void AddIncome_AddsOnlyToIncomesCollection()
    {
        _suv.AddIncome(CreateIncome());

        CheckCollectionCounts(new CheckItemsQuantity(Incomes: 1));
    }

    [Fact]
    public void RemoveIncome_RemovesOnlyTheMatchingIncome()
    {
        var toKeep = CreateIncome();
        var toRemove = CreateIncome();
        _suv.AddIncome(toKeep);
        _suv.AddIncome(toRemove);

        _suv.RemoveIncome(toRemove.Id);

        _suv.Incomes.Should().ContainSingle().Which.Id.Should().Be(toKeep.Id);
    }

    [Fact]
    public void RemoveIncome_WithUnknownId_LeavesCollectionUnchanged()
    {
        _suv.AddIncome(CreateIncome());

        _suv.RemoveIncome(Guid.NewGuid());

        _suv.Incomes.Should().ContainSingle();
    }

    private static Income CreateIncome() =>
        Income.Create(new DateOnly(2026, 7, 1), Lottery, null, 10m, Chase);

    [Fact]
    public void AddTransfer_AddsOnlyToTransfersCollection()
    {
        _suv.AddTransfer(CreateTransfer());

        CheckCollectionCounts(new CheckItemsQuantity(Transfers: 1));
    }

    [Fact]
    public void UpdateTransfer_ReplacesTheMatchingEntry()
    {
        var transfer = CreateTransfer();
        _suv.AddTransfer(transfer);
        transfer.UpdateDetails(new DateOnly(2026, 8, 1), Chase, Trading212, 250m, "Updated");

        _suv.UpdateTransfer(transfer);

        _suv.Transfers.Should().ContainSingle().Which.Amount.Should().Be(250m);
    }

    [Fact]
    public void UpdateTransfer_WithUnknownId_LeavesCollectionUnchanged()
    {
        _suv.AddTransfer(CreateTransfer());
        var unknown = CreateTransfer();

        _suv.UpdateTransfer(unknown);

        _suv.Transfers.Should().ContainSingle().Which.Id.Should().NotBe(unknown.Id);
    }

    [Fact]
    public void RemoveTransfer_RemovesOnlyTheMatchingTransfer()
    {
        var toKeep = CreateTransfer();
        var toRemove = CreateTransfer();
        _suv.AddTransfer(toKeep);
        _suv.AddTransfer(toRemove);

        _suv.RemoveTransfer(toRemove.Id);

        _suv.Transfers.Should().ContainSingle().Which.Id.Should().Be(toKeep.Id);
    }

    [Fact]
    public void RemoveTransfer_WithUnknownId_LeavesCollectionUnchanged()
    {
        _suv.AddTransfer(CreateTransfer());

        _suv.RemoveTransfer(Guid.NewGuid());

        _suv.Transfers.Should().ContainSingle();
    }

    private static Transfer CreateTransfer() =>
        Transfer.Create(new DateOnly(2026, 7, 1), Barclays, Trading212, 500m, "Test transfer");

    [Fact]
    public void AddBalanceAdjustment_AddsOnlyToBalanceAdjustmentsCollection()
    {
        _suv.AddBalanceAdjustment(CreateBalanceAdjustment());

        CheckCollectionCounts(new CheckItemsQuantity(BalanceAdjustments: 1));
    }

    [Fact]
    public void UpdateBalanceAdjustment_ReplacesTheMatchingEntry()
    {
        var adjustment = CreateBalanceAdjustment();
        _suv.AddBalanceAdjustment(adjustment);
        adjustment.UpdateDetails(new DateOnly(2026, 8, 1), 250m, 10m, "Updated");

        _suv.UpdateBalanceAdjustment(adjustment);

        _suv.BalanceAdjustments.Should().ContainSingle().Which.TargetBalance.Should().Be(250m);
    }

    [Fact]
    public void UpdateBalanceAdjustment_WithUnknownId_LeavesCollectionUnchanged()
    {
        _suv.AddBalanceAdjustment(CreateBalanceAdjustment());
        var unknown = CreateBalanceAdjustment();

        _suv.UpdateBalanceAdjustment(unknown);

        _suv.BalanceAdjustments.Should().ContainSingle().Which.Id.Should().NotBe(unknown.Id);
    }

    [Fact]
    public void RemoveBalanceAdjustment_RemovesOnlyTheMatchingAdjustment()
    {
        var toKeep = CreateBalanceAdjustment();
        var toRemove = CreateBalanceAdjustment();
        _suv.AddBalanceAdjustment(toKeep);
        _suv.AddBalanceAdjustment(toRemove);

        _suv.RemoveBalanceAdjustment(toRemove.Id);

        _suv.BalanceAdjustments.Should().ContainSingle().Which.Id.Should().Be(toKeep.Id);
    }

    [Fact]
    public void RemoveBalanceAdjustment_WithUnknownId_LeavesCollectionUnchanged()
    {
        _suv.AddBalanceAdjustment(CreateBalanceAdjustment());

        _suv.RemoveBalanceAdjustment(Guid.NewGuid());

        _suv.BalanceAdjustments.Should().ContainSingle();
    }

    private static BalanceAdjustment CreateBalanceAdjustment() =>
        BalanceAdjustment.Create(new DateOnly(2026, 7, 1), Barclays, 100m, 0m, "Test adjustment");

    private record CheckItemsQuantity(int Expenses = 0, int ReserveMovements = 0, int CardStatements = 0,
        int RecurringBills = 0, int MaeLedgerEntries = 0, int InvestmentSnapshots = 0, int InvestmentAccounts = 0, int Banks = 0,
        int IncomeSources = 0, int ReserveBuckets = 0, int Incomes = 0, int Transfers = 0, int BalanceAdjustments = 0, int CreditCards = 0);

    private void CheckCollectionCounts(CheckItemsQuantity expected)
    {
        _suv.Expenses.Count.Should().Be(expected.Expenses);
        _suv.ReserveMovements.Count.Should().Be(expected.ReserveMovements);
        _suv.CardStatements.Count.Should().Be(expected.CardStatements);
        _suv.RecurringBills.Count.Should().Be(expected.RecurringBills);
        _suv.MaeLedgerEntries.Count.Should().Be(expected.MaeLedgerEntries);
        _suv.InvestmentSnapshots.Count.Should().Be(expected.InvestmentSnapshots);
        _suv.InvestmentAccounts.Count.Should().Be(expected.InvestmentAccounts);
        _suv.Banks.Count.Should().Be(expected.Banks);
        _suv.IncomeSources.Count.Should().Be(expected.IncomeSources);
        _suv.ReserveBuckets.Count.Should().Be(expected.ReserveBuckets);
        _suv.Incomes.Count.Should().Be(expected.Incomes);
        _suv.Transfers.Count.Should().Be(expected.Transfers);
        _suv.BalanceAdjustments.Count.Should().Be(expected.BalanceAdjustments);
        _suv.CreditCards.Count.Should().Be(expected.CreditCards);
    }
}
