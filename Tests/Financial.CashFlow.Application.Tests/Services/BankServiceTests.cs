using Financial.CashFlow.Application.Interfaces;
using Financial.CashFlow.Application.Services;
using Financial.CashFlow.Domain.Entities;
using FluentAssertions;

namespace Financial.CashFlow.Application.Tests.Services;

public class BankServiceTests
{
    [Fact]
    public void Constructor_WithNullRepository_Throws()
    {
        Action act = () => new BankService(null!);
        act.Should().Throw<ArgumentNullException>().WithParameterName("repository");
    }

    [Fact]
    public void GetBanks_MapsEveryRepositoryBankToADto()
    {
        var repository = new StubCashFlowRepository();
        repository.Banks.Add(Bank.Create("Barclays", roundUpEnabled: false));
        repository.Banks.Add(Bank.Create("Trading212", roundUpEnabled: true));
        var service = new BankService(repository);

        var result = service.GetBanks();

        result.Should().HaveCount(2);
        result.Should().ContainSingle(b => b.Name == "Barclays" && !b.RoundUpEnabled);
        result.Should().ContainSingle(b => b.Name == "Trading212" && b.RoundUpEnabled);
    }

    [Fact]
    public void GetBanks_WithNoBanks_ReturnsEmptyList()
    {
        var service = new BankService(new StubCashFlowRepository());

        var result = service.GetBanks();

        result.Should().BeEmpty();
    }

    private sealed class StubCashFlowRepository : ICashFlowRepository
    {
        public List<Bank> Banks { get; } = new();

        public IEnumerable<Expense> GetExpenses() => Array.Empty<Expense>();
        public void AddExpense(Expense expense) { }
        public void DeleteExpense(Guid id) { }

        public IEnumerable<ReserveMovement> GetReserveMovements() => Array.Empty<ReserveMovement>();
        public void AddReserveMovement(ReserveMovement movement) { }
        public void DeleteReserveMovement(Guid id) { }

        public IEnumerable<CardStatement> GetCardStatements() => Array.Empty<CardStatement>();
        public void AddCardStatement(CardStatement statement) { }

        public IEnumerable<RecurringBill> GetRecurringBills() => Array.Empty<RecurringBill>();
        public void AddRecurringBill(RecurringBill bill) { }
        public void DeleteRecurringBill(Guid id) { }

        public IEnumerable<MaeLedgerEntry> GetMaeLedgerEntries() => Array.Empty<MaeLedgerEntry>();
        public void AddMaeLedgerEntry(MaeLedgerEntry entry) { }
        public void DeleteMaeLedgerEntry(Guid id) { }

        public IEnumerable<InvestmentSnapshot> GetInvestmentSnapshots() => Array.Empty<InvestmentSnapshot>();
        public void AddInvestmentSnapshot(InvestmentSnapshot snapshot) { }

        public IEnumerable<Bank> GetBanks() => Banks;

        public IEnumerable<Income> GetIncomes() => Array.Empty<Income>();
        public void AddIncome(Income income) { }
        public void DeleteIncome(Guid id) { }

        public Task SaveChangesAsync() => Task.CompletedTask;
    }
}
