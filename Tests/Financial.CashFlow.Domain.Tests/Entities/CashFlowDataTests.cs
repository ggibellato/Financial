using Financial.CashFlow.Domain.Entities;
using Financial.CashFlow.Domain.Enums;
using FluentAssertions;
using ReserveBucketEntity = Financial.CashFlow.Domain.Entities.ReserveBucket;
using CategoryEntity = Financial.CashFlow.Domain.Entities.Category;

namespace Financial.CashFlow.Domain.Tests;

public class CashFlowDataTests
{
    private static readonly Bank Chase = Bank.Create("Chase", roundUpEnabled: true);
    private static readonly Bank Barclays = Bank.Create("Barclays", roundUpEnabled: false);
    private static readonly Bank Trading212 = Bank.Create("Trading212", roundUpEnabled: true);
    private static readonly IncomeSource Lottery = IncomeSource.Create("Lottery", IncomeGroup.NonReportable);
    private static readonly InvestmentAccount ChaseSaveAccount =
        InvestmentAccount.Create("ChaseSave", isActive: true, isLiability: false);

    private readonly CashFlowData _sut;

    public CashFlowDataTests() {
        _sut = CashFlowData.Create();
    }

    [Fact]
    public void Create_StartsWithAllCollectionsEmpty()
    {
        _sut.Expenses.Should().BeEmpty();
        _sut.ReserveMovements.Should().BeEmpty();
        _sut.CardStatements.Should().BeEmpty();
        _sut.RecurringBills.Should().BeEmpty();
        _sut.MaeLedgerEntries.Should().BeEmpty();
        _sut.InvestmentSnapshots.Should().BeEmpty();
        _sut.InvestmentAccounts.Should().BeEmpty();
        _sut.Banks.Should().BeEmpty();
        _sut.IncomeSources.Should().BeEmpty();
        _sut.ReserveBuckets.Should().BeEmpty();
        _sut.Incomes.Should().BeEmpty();
        _sut.Transfers.Should().BeEmpty();
        _sut.BalanceAdjustments.Should().BeEmpty();
        _sut.CreditCards.Should().BeEmpty();
        _sut.Categories.Should().BeEmpty();
    }

    [Fact]
    public void AddBank_AddsOnlyToBanksCollection()
    {
        _sut.AddBank(Bank.Create("Barclays", roundUpEnabled: false));
        CheckCollectionCounts(new CheckItemsQuantity(Banks: 1));
    }

    [Fact]
    public void AddIncomeSource_AddsOnlyToIncomeSourcesCollection()
    {
        _sut.AddIncomeSource(IncomeSource.Create("Gleison", IncomeGroup.Salary));

        CheckCollectionCounts(new CheckItemsQuantity(IncomeSources: 1));
    }

    [Fact]
    public void AddReserveBucket_AddsOnlyToReserveBucketsCollection()
    {
        _sut.AddReserveBucket(ReserveBucketEntity.Create("Investimento", 33.33m));

        CheckCollectionCounts(new CheckItemsQuantity(ReserveBuckets: 1));
    }

    [Fact]
    public void AddCreditCard_AddsOnlyToCreditCardsCollection()
    {
        _sut.AddCreditCard(Domain.Entities.CreditCard.Create("VISA 1", isActive: true));

        CheckCollectionCounts(new CheckItemsQuantity(CreditCards: 1));
    }

    [Fact]
    public void AddCategory_AddsOnlyToCategoriesCollection()
    {
        _sut.AddCategory(CategoryEntity.Create("Mercado"));

        CheckCollectionCounts(new CheckItemsQuantity(Categories: 1));
    }

    [Fact]
    public void AddExpense_AddsOnlyToExpensesCollection()
    {
        _sut.AddExpense(CreateExpense());

        CheckCollectionCounts(new CheckItemsQuantity(Expenses: 1));
    }

    [Fact]
    public void RemoveExpense_RemovesOnlyTheMatchingExpense()
    {
        var toKeep = CreateExpense();
        var toRemove = CreateExpense();
        _sut.AddExpense(toKeep);
        _sut.AddExpense(toRemove);

        _sut.RemoveExpense(toRemove.Id);

        _sut.Expenses.Should().ContainSingle().Which.Id.Should().Be(toKeep.Id);
    }

    [Fact]
    public void RemoveExpense_WithUnknownId_LeavesCollectionUnchanged()
    {
        _sut.AddExpense(CreateExpense());
        
        _sut.RemoveExpense(Guid.NewGuid());

        _sut.Expenses.Should().ContainSingle();
    }

    private static readonly CategoryEntity Casa = CategoryEntity.Create("Casa");

    private static Expense CreateExpense() =>
        Expense.Create(new DateOnly(2026, 7, 1), "Test expense", 10m, Casa, Chase, null);

    [Fact]
    public void AddReserveMovement_AddsOnlyToReserveMovementsCollection()
    {
        _sut.AddReserveMovement(CreateReserveMovement());

        CheckCollectionCounts(new CheckItemsQuantity(ReserveMovements: 1));
    }

    [Fact]
    public void RemoveReserveMovement_RemovesOnlyTheMatchingMovement()
    {
        var toKeep = CreateReserveMovement();
        var toRemove = CreateReserveMovement();
        _sut.AddReserveMovement(toKeep);
        _sut.AddReserveMovement(toRemove);

        _sut.RemoveReserveMovement(toRemove.Id);

        _sut.ReserveMovements.Should().ContainSingle().Which.Id.Should().Be(toKeep.Id);
    }

    [Fact]
    public void RemoveReserveMovement_WithUnknownId_LeavesCollectionUnchanged()
    {
        _sut.AddReserveMovement(CreateReserveMovement());

        _sut.RemoveReserveMovement(Guid.NewGuid());

        _sut.ReserveMovements.Should().ContainSingle();
    }

    private static readonly ReserveBucketEntity TestBucket = ReserveBucketEntity.Create("Investimento", 33.33m);

    private static ReserveMovement CreateReserveMovement() =>
        ReserveMovement.Create(TestBucket, 10m, new DateOnly(2026, 7, 1), "Test movement");

    [Fact]
    public void AddCardStatement_AddsOnlyToCardStatementsCollection()
    {
        _sut.AddCardStatement(CardStatement.Create(Domain.Entities.CreditCard.Create("BarclaysPlatinumVisa8003"), 2026, 7));

        CheckCollectionCounts(new CheckItemsQuantity(CardStatements: 1));
    }

    [Fact]
    public void AddRecurringBill_AddsOnlyToRecurringBillsCollection()
    {
        _sut.AddRecurringBill(CreateRecurringBill());

        CheckCollectionCounts(new CheckItemsQuantity(RecurringBills: 1));
    }

    [Fact]
    public void RemoveRecurringBill_RemovesOnlyTheMatchingBill()
    {
        var toKeep = CreateRecurringBill();
        var toRemove = CreateRecurringBill();
        _sut.AddRecurringBill(toKeep);
        _sut.AddRecurringBill(toRemove);

        _sut.RemoveRecurringBill(toRemove.Id);

        _sut.RecurringBills.Should().ContainSingle().Which.Id.Should().Be(toKeep.Id);
    }

    [Fact]
    public void RemoveRecurringBill_WithUnknownId_LeavesCollectionUnchanged()
    {
        _sut.AddRecurringBill(CreateRecurringBill());

        _sut.RemoveRecurringBill(Guid.NewGuid());

        _sut.RecurringBills.Should().ContainSingle();
    }

    private static RecurringBill CreateRecurringBill() =>
        RecurringBill.Create(10, "Test bill", 100m, Area.Brasil, string.Empty, null, null);

    [Fact]
    public void AddMaeLedgerEntry_AddsOnlyToMaeLedgerEntriesCollection()
    {
        _sut.AddMaeLedgerEntry(CreateMaeLedgerEntry());

        CheckCollectionCounts(new CheckItemsQuantity(MaeLedgerEntries: 1));
    }

    private static MaeLedgerEntry CreateMaeLedgerEntry() =>
        MaeLedgerEntry.Create(new DateOnly(2026, 7, 1), "Test entry", string.Empty, Currency.BRL, 100m, 15m);

    [Fact]
    public void RemoveMaeLedgerEntry_RemovesOnlyTheMatchingEntry()
    {
        var toKeep = CreateMaeLedgerEntry();
        var toRemove = CreateMaeLedgerEntry();
        _sut.AddMaeLedgerEntry(toKeep);
        _sut.AddMaeLedgerEntry(toRemove);

        _sut.RemoveMaeLedgerEntry(toRemove.Id);

        _sut.MaeLedgerEntries.Should().ContainSingle().Which.Id.Should().Be(toKeep.Id);
    }

    [Fact]
    public void RemoveMaeLedgerEntry_WithUnknownId_LeavesCollectionUnchanged()
    {
        _sut.AddMaeLedgerEntry(CreateMaeLedgerEntry());

        _sut.RemoveMaeLedgerEntry(Guid.NewGuid());

        _sut.MaeLedgerEntries.Should().ContainSingle();
    }

    [Fact]
    public void AddInvestmentSnapshot_AddsOnlyToInvestmentSnapshotsCollection()
    {
        _sut.AddInvestmentSnapshot(InvestmentSnapshot.Create(ChaseSaveAccount, 2026, 7, 100m));

        CheckCollectionCounts(new CheckItemsQuantity(InvestmentSnapshots: 1));
    }

    [Fact]
    public void AddInvestmentAccount_AddsOnlyToInvestmentAccountsCollection()
    {
        _sut.AddInvestmentAccount(InvestmentAccount.Create("ChaseSave", isActive: true, isLiability: false));

        CheckCollectionCounts(new CheckItemsQuantity(InvestmentAccounts: 1));
    }

    [Fact]
    public void AddIncome_AddsOnlyToIncomesCollection()
    {
        _sut.AddIncome(CreateIncome());

        CheckCollectionCounts(new CheckItemsQuantity(Incomes: 1));
    }

    [Fact]
    public void RemoveIncome_RemovesOnlyTheMatchingIncome()
    {
        var toKeep = CreateIncome();
        var toRemove = CreateIncome();
        _sut.AddIncome(toKeep);
        _sut.AddIncome(toRemove);

        _sut.RemoveIncome(toRemove.Id);

        _sut.Incomes.Should().ContainSingle().Which.Id.Should().Be(toKeep.Id);
    }

    [Fact]
    public void RemoveIncome_WithUnknownId_LeavesCollectionUnchanged()
    {
        _sut.AddIncome(CreateIncome());

        _sut.RemoveIncome(Guid.NewGuid());

        _sut.Incomes.Should().ContainSingle();
    }

    private static Income CreateIncome() =>
        Income.Create(new DateOnly(2026, 7, 1), Lottery, null, 10m, Chase);

    [Fact]
    public void AddTransfer_AddsOnlyToTransfersCollection()
    {
        _sut.AddTransfer(CreateTransfer());

        CheckCollectionCounts(new CheckItemsQuantity(Transfers: 1));
    }

    [Fact]
    public void UpdateTransfer_ReplacesTheMatchingEntry()
    {
        var transfer = CreateTransfer();
        _sut.AddTransfer(transfer);
        transfer.UpdateDetails(new DateOnly(2026, 8, 1), Chase, Trading212, 250m, "Updated");

        _sut.UpdateTransfer(transfer);

        _sut.Transfers.Should().ContainSingle().Which.Amount.Should().Be(250m);
    }

    [Fact]
    public void UpdateTransfer_WithUnknownId_LeavesCollectionUnchanged()
    {
        _sut.AddTransfer(CreateTransfer());
        var unknown = CreateTransfer();

        _sut.UpdateTransfer(unknown);

        _sut.Transfers.Should().ContainSingle().Which.Id.Should().NotBe(unknown.Id);
    }

    [Fact]
    public void RemoveTransfer_RemovesOnlyTheMatchingTransfer()
    {
        var toKeep = CreateTransfer();
        var toRemove = CreateTransfer();
        _sut.AddTransfer(toKeep);
        _sut.AddTransfer(toRemove);

        _sut.RemoveTransfer(toRemove.Id);

        _sut.Transfers.Should().ContainSingle().Which.Id.Should().Be(toKeep.Id);
    }

    [Fact]
    public void RemoveTransfer_WithUnknownId_LeavesCollectionUnchanged()
    {
        _sut.AddTransfer(CreateTransfer());

        _sut.RemoveTransfer(Guid.NewGuid());

        _sut.Transfers.Should().ContainSingle();
    }

    private static Transfer CreateTransfer() =>
        Transfer.Create(new DateOnly(2026, 7, 1), Barclays, Trading212, 500m, "Test transfer");

    [Fact]
    public void AddBalanceAdjustment_AddsOnlyToBalanceAdjustmentsCollection()
    {
        _sut.AddBalanceAdjustment(CreateBalanceAdjustment());

        CheckCollectionCounts(new CheckItemsQuantity(BalanceAdjustments: 1));
    }

    [Fact]
    public void UpdateBalanceAdjustment_ReplacesTheMatchingEntry()
    {
        var adjustment = CreateBalanceAdjustment();
        _sut.AddBalanceAdjustment(adjustment);
        adjustment.UpdateDetails(new DateOnly(2026, 8, 1), 250m, 10m, "Updated");

        _sut.UpdateBalanceAdjustment(adjustment);

        _sut.BalanceAdjustments.Should().ContainSingle().Which.TargetBalance.Should().Be(250m);
    }

    [Fact]
    public void UpdateBalanceAdjustment_WithUnknownId_LeavesCollectionUnchanged()
    {
        _sut.AddBalanceAdjustment(CreateBalanceAdjustment());
        var unknown = CreateBalanceAdjustment();

        _sut.UpdateBalanceAdjustment(unknown);

        _sut.BalanceAdjustments.Should().ContainSingle().Which.Id.Should().NotBe(unknown.Id);
    }

    [Fact]
    public void RemoveBalanceAdjustment_RemovesOnlyTheMatchingAdjustment()
    {
        var toKeep = CreateBalanceAdjustment();
        var toRemove = CreateBalanceAdjustment();
        _sut.AddBalanceAdjustment(toKeep);
        _sut.AddBalanceAdjustment(toRemove);

        _sut.RemoveBalanceAdjustment(toRemove.Id);

        _sut.BalanceAdjustments.Should().ContainSingle().Which.Id.Should().Be(toKeep.Id);
    }

    [Fact]
    public void RemoveBalanceAdjustment_WithUnknownId_LeavesCollectionUnchanged()
    {
        _sut.AddBalanceAdjustment(CreateBalanceAdjustment());

        _sut.RemoveBalanceAdjustment(Guid.NewGuid());

        _sut.BalanceAdjustments.Should().ContainSingle();
    }

    private static BalanceAdjustment CreateBalanceAdjustment() =>
        BalanceAdjustment.Create(new DateOnly(2026, 7, 1), Barclays, 100m, 0m, "Test adjustment");

    private record CheckItemsQuantity(int Expenses = 0, int ReserveMovements = 0, int CardStatements = 0,
        int RecurringBills = 0, int MaeLedgerEntries = 0, int InvestmentSnapshots = 0, int InvestmentAccounts = 0, int Banks = 0,
        int IncomeSources = 0, int ReserveBuckets = 0, int Incomes = 0, int Transfers = 0, int BalanceAdjustments = 0, int CreditCards = 0,
        int Categories = 0);

    private void CheckCollectionCounts(CheckItemsQuantity expected)
    {
        _sut.Expenses.Count.Should().Be(expected.Expenses);
        _sut.ReserveMovements.Count.Should().Be(expected.ReserveMovements);
        _sut.CardStatements.Count.Should().Be(expected.CardStatements);
        _sut.RecurringBills.Count.Should().Be(expected.RecurringBills);
        _sut.MaeLedgerEntries.Count.Should().Be(expected.MaeLedgerEntries);
        _sut.InvestmentSnapshots.Count.Should().Be(expected.InvestmentSnapshots);
        _sut.InvestmentAccounts.Count.Should().Be(expected.InvestmentAccounts);
        _sut.Banks.Count.Should().Be(expected.Banks);
        _sut.IncomeSources.Count.Should().Be(expected.IncomeSources);
        _sut.ReserveBuckets.Count.Should().Be(expected.ReserveBuckets);
        _sut.Incomes.Count.Should().Be(expected.Incomes);
        _sut.Transfers.Count.Should().Be(expected.Transfers);
        _sut.BalanceAdjustments.Count.Should().Be(expected.BalanceAdjustments);
        _sut.CreditCards.Count.Should().Be(expected.CreditCards);
        _sut.Categories.Count.Should().Be(expected.Categories);
    }
}
