using Financial.CashFlow.Application.Interfaces;
using Financial.CashFlow.Domain.Entities;
using Financial.CashFlow.Domain.Enums;

namespace Financial.TestUtilities;

/// <summary>
/// Shared in-memory <see cref="ICashFlowRepository"/> test double. Every collection is backed by a
/// real list so CRUD operations behave correctly regardless of which entity a given test exercises;
/// nothing is seeded by default. Adding a new member to <see cref="ICashFlowRepository"/> only
/// requires updating this one class instead of every service test file.
/// </summary>
public sealed class StubCashFlowRepository : ICashFlowRepository
{
    public List<Expense> Expenses { get; } = new();
    public List<ReserveMovement> ReserveMovements { get; } = new();
    public List<CardStatement> CardStatements { get; } = new();
    public List<RecurringBill> RecurringBills { get; } = new();
    public List<MaeLedgerEntry> MaeLedgerEntries { get; } = new();
    public List<InvestmentSnapshot> InvestmentSnapshots { get; } = new();
    public List<InvestmentAccount> InvestmentAccounts { get; } = new();
    public List<Bank> Banks { get; } = new();
    public List<IncomeSource> IncomeSources { get; } = new();
    public List<ReserveBucket> ReserveBuckets { get; } = new();
    public List<CreditCard> CreditCards { get; } = new();
    public List<Category> Categories { get; } = new();
    public List<Income> Incomes { get; } = new();
    public List<Transfer> Transfers { get; } = new();
    public List<BalanceAdjustment> BalanceAdjustments { get; } = new();

    public int SaveChangesCallCount { get; private set; }

    /// <summary>When true, the next <see cref="ApplyAndSaveAsync"/> that reaches the write throws
    /// once and resets to false.</summary>
    public bool ThrowOnNextSave { get; set; }

    public StubCashFlowRepository(
        bool seedDefaultBanks = false, bool seedDefaultIncomeSources = false, bool seedDefaultReserveBuckets = false,
        bool seedDefaultCreditCards = false, bool seedDefaultCategories = false)
    {
        if (seedDefaultBanks)
        {
            Banks.AddRange(DefaultBanks());
        }

        if (seedDefaultIncomeSources)
        {
            IncomeSources.AddRange(DefaultIncomeSources());
        }

        if (seedDefaultReserveBuckets)
        {
            ReserveBuckets.AddRange(DefaultReserveBuckets());
        }

        if (seedDefaultCreditCards)
        {
            CreditCards.AddRange(DefaultCreditCards());
        }

        if (seedDefaultCategories)
        {
            Categories.AddRange(DefaultCategories());
        }
    }

    /// <summary>The 3 banks seeded in a real deployment by the CashFlowSpreadsheetImport migration tool.</summary>
    public static IEnumerable<Bank> DefaultBanks() =>
    [
        Bank.Create("Barclays", roundUpEnabled: false),
        Bank.Create("Trading212", roundUpEnabled: true),
        Bank.Create("Chase", roundUpEnabled: true)
    ];

    /// <summary>The 4 income sources seeded in a real deployment by the CashFlowSpreadsheetImport migration tool.</summary>
    public static IEnumerable<IncomeSource> DefaultIncomeSources() =>
    [
        IncomeSource.Create("Gleison", IncomeGroup.Salary),
        IncomeSource.Create("Ariana", IncomeGroup.Salary),
        IncomeSource.Create("Lottery", IncomeGroup.NonReportable),
        IncomeSource.Create("DividendoJuros", IncomeGroup.DividendoJuros)
    ];

    /// <summary>The 4 reserve buckets seeded in a real deployment by the CashFlowSpreadsheetImport migration tool.</summary>
    public static IEnumerable<ReserveBucket> DefaultReserveBuckets() =>
    [
        ReserveBucket.Create("Investimento", 33.33m),
        ReserveBucket.Create("HouseTreats", 33.33m),
        ReserveBucket.Create("Ariana", 16.67m),
        ReserveBucket.Create("Gleison", 16.67m)
    ];

    /// <summary>The 5 credit cards seeded in a real deployment by the CashFlowSpreadsheetImport migration tool, all active.</summary>
    public static IEnumerable<CreditCard> DefaultCreditCards() =>
    [
        CreditCard.Create("BarclaysPlatinumVisa8003"),
        CreditCard.Create("BarclaysPlatinumVisa6007"),
        CreditCard.Create("ChaseMaster4023"),
        CreditCard.Create("BaAmex"),
        CreditCard.Create("PaypalCredit")
    ];

    /// <summary>The 14 categories seeded in a real deployment by the CashFlowSpreadsheetImport migration tool.</summary>
    public static IEnumerable<Category> DefaultCategories() =>
    [
        Category.Create("Ariana"),
        Category.Create("Carro"),
        Category.Create("Casa"),
        Category.Create("Estudo"),
        Category.Create("Extras"),
        Category.Create("Familia"),
        Category.Create("Gleison"),
        Category.Create("Mercado"),
        Category.Create("Samuel"),
        Category.Create("Saude"),
        Category.Create("Viagem"),
        Category.Create("Dizimo", isTithe: true),
        Category.Create("Investimento", isInvestment: true),
        Category.Create("Reserva")
    ];

    public void SetOpeningBalance(string bankName, decimal openingBalance, DateOnly openingBalanceDate) =>
        Banks.First(b => b.Name == bankName).SetOpeningBalance(openingBalance, openingBalanceDate);

    public IEnumerable<Expense> GetExpenses() => Expenses;
    public void AddExpense(Expense expense) => Expenses.Add(expense);
    public void DeleteExpense(Guid id) => Expenses.RemoveAll(e => e.Id == id);

    public IEnumerable<ReserveMovement> GetReserveMovements() => ReserveMovements;
    public void AddReserveMovement(ReserveMovement movement) => ReserveMovements.Add(movement);
    public void DeleteReserveMovement(Guid id) => ReserveMovements.RemoveAll(m => m.Id == id);

    public IEnumerable<CardStatement> GetCardStatements() => CardStatements;
    public void AddCardStatement(CardStatement statement) => CardStatements.Add(statement);

    public IEnumerable<RecurringBill> GetRecurringBills() => RecurringBills;
    public void AddRecurringBill(RecurringBill bill) => RecurringBills.Add(bill);
    public void DeleteRecurringBill(Guid id) => RecurringBills.RemoveAll(b => b.Id == id);

    public IEnumerable<MaeLedgerEntry> GetMaeLedgerEntries() => MaeLedgerEntries;
    public void AddMaeLedgerEntry(MaeLedgerEntry entry) => MaeLedgerEntries.Add(entry);
    public void DeleteMaeLedgerEntry(Guid id) => MaeLedgerEntries.RemoveAll(e => e.Id == id);

    public IEnumerable<InvestmentSnapshot> GetInvestmentSnapshots() => InvestmentSnapshots;
    public void AddInvestmentSnapshot(InvestmentSnapshot snapshot) => InvestmentSnapshots.Add(snapshot);

    public IEnumerable<InvestmentAccount> GetInvestmentAccounts() => InvestmentAccounts;
    public void AddInvestmentAccount(InvestmentAccount account) => InvestmentAccounts.Add(account);

    public IEnumerable<Bank> GetBanks() => Banks;

    public IEnumerable<IncomeSource> GetIncomeSources() => IncomeSources;

    public IEnumerable<ReserveBucket> GetReserveBuckets() => ReserveBuckets;

    public IEnumerable<CreditCard> GetCreditCards() => CreditCards;

    public IEnumerable<Category> GetCategories() => Categories;

    public IEnumerable<Income> GetIncomes() => Incomes;
    public void AddIncome(Income income) => Incomes.Add(income);
    public void DeleteIncome(Guid id) => Incomes.RemoveAll(i => i.Id == id);

    public IEnumerable<Transfer> GetTransfers() => Transfers;
    public void AddTransfer(Transfer transfer) => Transfers.Add(transfer);
    public void UpdateTransfer(Transfer transfer)
    {
        var index = Transfers.FindIndex(t => t.Id == transfer.Id);
        if (index >= 0)
        {
            Transfers[index] = transfer;
        }
    }
    public void DeleteTransfer(Guid id) => Transfers.RemoveAll(t => t.Id == id);

    public IEnumerable<BalanceAdjustment> GetBalanceAdjustments() => BalanceAdjustments;
    public void AddBalanceAdjustment(BalanceAdjustment adjustment) => BalanceAdjustments.Add(adjustment);
    public void UpdateBalanceAdjustment(BalanceAdjustment adjustment)
    {
        var index = BalanceAdjustments.FindIndex(a => a.Id == adjustment.Id);
        if (index >= 0)
        {
            BalanceAdjustments[index] = adjustment;
        }
    }
    public void DeleteBalanceAdjustment(Guid id) => BalanceAdjustments.RemoveAll(a => a.Id == id);

    /// <summary>Runs the mutation for real - the change lives in the delegate, so a stub that only
    /// counted would silently stop exercising it. Counts persisted writes, so a mutation that
    /// reports no change does not register as a save.</summary>
    public Task<bool> ApplyAndSaveAsync(Func<bool> applyChanges)
    {
        if (!applyChanges())
        {
            return Task.FromResult(false);
        }

        SaveChangesCallCount++;
        if (ThrowOnNextSave)
        {
            ThrowOnNextSave = false;
            throw new InvalidOperationException("Simulated save failure.");
        }

        return Task.FromResult(true);
    }
}
