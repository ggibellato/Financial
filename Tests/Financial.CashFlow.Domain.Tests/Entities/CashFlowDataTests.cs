using Financial.CashFlow.Domain.Entities;
using Financial.CashFlow.Domain.Enums;
using Financial.Shared.Abstractions.Currencies;
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
    private static readonly Category Casa = Category.Create("Casa");
    private static readonly ReserveBucketEntity TestBucket = ReserveBucketEntity.Create("Investimento", 33.33m);

    private readonly CashFlowData _sut;

    public CashFlowDataTests() {
        _sut = CashFlowData.Create();
    }

    public sealed record CollectionWiring(
        string Name,
        Func<object> CreateItem,
        Action<CashFlowData, object> Add,
        Action<CashFlowData, Guid>? Remove,
        Func<CashFlowData, IReadOnlyCollection<object>> Items,
        Func<object, Guid> IdOf)
    {
        public override string ToString() => Name;
    }

    private static CollectionWiring Wiring<T>(
        string name,
        Func<T> create,
        Action<CashFlowData, T> add,
        Action<CashFlowData, Guid>? remove,
        Func<CashFlowData, IReadOnlyCollection<T>> items,
        Func<T, Guid> idOf) where T : class =>
        new(name, create, (data, item) => add(data, (T)item), remove, data => items(data).Cast<object>().ToArray(), item => idOf((T)item));

    private static readonly CollectionWiring[] AllWirings =
    [
        Wiring("Banks", () => Bank.Create("Barclays", roundUpEnabled: false), (d, i) => d.AddBank(i), (d, id) => d.RemoveBank(id), d => d.Banks, i => i.Id),
        Wiring("IncomeSources", () => IncomeSource.Create("Gleison", IncomeGroup.Salary), (d, i) => d.AddIncomeSource(i), (d, id) => d.RemoveIncomeSource(id), d => d.IncomeSources, i => i.Id),
        Wiring("ReserveBuckets", () => ReserveBucketEntity.Create("Investimento", 33.33m), (d, i) => d.AddReserveBucket(i), null, d => d.ReserveBuckets, i => i.Id),
        Wiring("CreditCards", () => Domain.Entities.CreditCard.Create("VISA 1", isActive: true), (d, i) => d.AddCreditCard(i), (d, id) => d.RemoveCreditCard(id), d => d.CreditCards, i => i.Id),
        Wiring("Categories", () => Category.Create("Mercado"), (d, i) => d.AddCategory(i), (d, id) => d.RemoveCategory(id), d => d.Categories, i => i.Id),
        Wiring("Expenses", () => Expense.Create(new DateOnly(2026, 7, 1), "Test expense", 10m, Casa, Chase, null), (d, i) => d.AddExpense(i), (d, id) => d.RemoveExpense(id), d => d.Expenses, i => i.Id),
        Wiring("ReserveMovements", () => ReserveMovement.Create(TestBucket, 10m, new DateOnly(2026, 7, 1), "Test movement"), (d, i) => d.AddReserveMovement(i), (d, id) => d.RemoveReserveMovement(id), d => d.ReserveMovements, i => i.Id),
        Wiring("CardStatements", () => CardStatement.Create(Domain.Entities.CreditCard.Create("BarclaysPlatinumVisa8003"), 2026, 7), (d, i) => d.AddCardStatement(i), null, d => d.CardStatements, i => i.Id),
        Wiring("RecurringBills", () => RecurringBill.Create(10, "Test bill", 100m, Area.Brasil, string.Empty, null, null), (d, i) => d.AddRecurringBill(i), (d, id) => d.RemoveRecurringBill(id), d => d.RecurringBills, i => i.Id),
        Wiring("MaeLedgerEntries", () => MaeLedgerEntry.Create(new DateOnly(2026, 7, 1), "Test entry", string.Empty, Currency.BRL, 100m, 15m), (d, i) => d.AddMaeLedgerEntry(i), (d, id) => d.RemoveMaeLedgerEntry(id), d => d.MaeLedgerEntries, i => i.Id),
        Wiring("InvestmentSnapshots", () => InvestmentSnapshot.Create(ChaseSaveAccount, 2026, 7, 100m), (d, i) => d.AddInvestmentSnapshot(i), null, d => d.InvestmentSnapshots, i => i.Id),
        Wiring("InvestmentAccounts", () => InvestmentAccount.Create("ChaseSave", isActive: true, isLiability: false), (d, i) => d.AddInvestmentAccount(i), (d, id) => d.RemoveInvestmentAccount(id), d => d.InvestmentAccounts, i => i.Id),
        Wiring("Incomes", CreateIncome, (d, i) => d.AddIncome(i), (d, id) => d.RemoveIncome(id), d => d.Incomes, i => i.Id),
        Wiring("Transfers", CreateTransfer, (d, i) => d.AddTransfer(i), (d, id) => d.RemoveTransfer(id), d => d.Transfers, i => i.Id),
        Wiring("BalanceAdjustments", CreateBalanceAdjustment, (d, i) => d.AddBalanceAdjustment(i), (d, id) => d.RemoveBalanceAdjustment(id), d => d.BalanceAdjustments, i => i.Id),
        Wiring("TitheCarryForwards", () => TitheCarryForward.Create(2026, 8, false), (d, i) => d.AddTitheCarryForward(i), null, d => d.TitheCarryForwards, _ => Guid.Empty),
    ];

    public static TheoryData<CollectionWiring> AddableCollections() => new(AllWirings);

    public static TheoryData<CollectionWiring> RemovableCollections() => new(AllWirings.Where(w => w.Remove is not null));

    [Fact]
    public void Create_StartsWithAllCollectionsEmpty()
    {
        AllWirings.Should().OnlyContain(w => w.Items(_sut).Count == 0);
        _sut.TitheCarryForwardEffectiveFrom.Should().BeNull();
    }

    [Theory]
    [MemberData(nameof(AddableCollections))]
    public void Add_AddsOnlyToTheMatchingCollection(CollectionWiring wiring)
    {
        var item = wiring.CreateItem();

        wiring.Add(_sut, item);

        wiring.Items(_sut).Should().ContainSingle().Which.Should().BeSameAs(item);
        AllWirings.Where(other => other != wiring).Should().OnlyContain(other => other.Items(_sut).Count == 0);
    }

    [Theory]
    [MemberData(nameof(RemovableCollections))]
    public void Remove_RemovesOnlyTheMatchingItem(CollectionWiring wiring)
    {
        var toRemove = wiring.CreateItem();
        var toKeep = wiring.CreateItem();
        wiring.Add(_sut, toRemove);
        wiring.Add(_sut, toKeep);

        wiring.Remove!(_sut, wiring.IdOf(toRemove));

        wiring.Items(_sut).Should().ContainSingle().Which.Should().BeSameAs(toKeep);
    }

    [Theory]
    [MemberData(nameof(RemovableCollections))]
    public void Remove_WithUnknownId_LeavesTheCollectionUnchanged(CollectionWiring wiring)
    {
        var item = wiring.CreateItem();
        wiring.Add(_sut, item);

        wiring.Remove!(_sut, Guid.NewGuid());

        wiring.Items(_sut).Should().ContainSingle().Which.Should().BeSameAs(item);
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
    public void SetTitheCarryForwardEffectiveFrom_SetsTheValue()
    {
        _sut.SetTitheCarryForwardEffectiveFrom(new DateOnly(2026, 9, 1));

        _sut.TitheCarryForwardEffectiveFrom.Should().Be(new DateOnly(2026, 9, 1));
    }

    [Fact]
    public void SetTitheCarryForwardEffectiveFrom_CalledAgain_OverwritesThePreviousValue()
    {
        _sut.SetTitheCarryForwardEffectiveFrom(new DateOnly(2026, 9, 1));

        _sut.SetTitheCarryForwardEffectiveFrom(new DateOnly(2026, 10, 1));

        _sut.TitheCarryForwardEffectiveFrom.Should().Be(new DateOnly(2026, 10, 1));
    }

    private static Income CreateIncome() =>
        Income.Create(new DateOnly(2026, 7, 1), Lottery, null, 10m, Chase);

    private static Transfer CreateTransfer() =>
        Transfer.Create(new DateOnly(2026, 7, 1), Barclays, Trading212, 500m, "Test transfer");

    private static BalanceAdjustment CreateBalanceAdjustment() =>
        BalanceAdjustment.Create(new DateOnly(2026, 7, 1), Barclays, 100m, 0m, "Test adjustment");
}
