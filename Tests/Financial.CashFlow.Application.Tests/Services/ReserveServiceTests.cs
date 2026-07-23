using Financial.CashFlow.Application.DTOs;
using Financial.CashFlow.Application.Exceptions;
using Financial.CashFlow.Application.Interfaces;
using Financial.CashFlow.Application.Services;
using Financial.CashFlow.Domain.Entities;
using Financial.CashFlow.Domain.Enums;
using FluentAssertions;

namespace Financial.CashFlow.Application.Tests.Services;

public class ReserveServiceTests
{
    [Fact]
    public void Constructor_WithNullRepository_Throws()
    {
        Action act = () => new ReserveService(null!);
        act.Should().Throw<ArgumentNullException>().WithParameterName("repository");
    }

    [Fact]
    public async Task PostIncomeSplitAsync_WithValidRequest_PostsExactlyFourMovementsAndReturnsAmounts()
    {
        var repository = new StubCashFlowRepository();
        var service = new ReserveService(repository);

        var result = await service.PostIncomeSplitAsync(ValidIncomeSplitRequest());

        repository.ReserveMovements.Should().HaveCount(4);
        result.Dizimo.Should().Be(637m);
        result.Investimento.Should().Be(654.33m);
        result.HouseTreats.Should().Be(654.33m);
        result.Ariana.Should().Be(327.17m);
        result.Gleison.Should().Be(327.17m);
        repository.SaveChangesCallCount.Should().Be(1);
    }

    [Theory]
    [InlineData(-1, 3600, 2600, 50, 120)]
    [InlineData(4500, -1, 2600, 50, 120)]
    [InlineData(4500, 3600, -1, 50, 120)]
    [InlineData(4500, 3600, 2600, -1, 120)]
    [InlineData(4500, 3600, 2600, 50, -1)]
    public async Task PostIncomeSplitAsync_WithAnyNegativeField_ThrowsBeforeTouchingRepository(
        decimal gleisonGross, decimal gleisonNet, decimal arianaGross, decimal arianaNet, decimal lottery)
    {
        var repository = new StubCashFlowRepository();
        var service = new ReserveService(repository);
        var request = new IncomeSplitRequestDTO
        {
            Date = new DateOnly(2026, 7, 1),
            GleisonSalaryGross = gleisonGross,
            GleisonSalaryNet = gleisonNet,
            ArianaSalaryGross = arianaGross,
            ArianaSalaryNet = arianaNet,
            Lottery = lottery,
            DividendoJuros = 120m
        };

        var act = async () => await service.PostIncomeSplitAsync(request);

        await act.Should().ThrowAsync<ArgumentException>();
        repository.ReserveMovements.Should().BeEmpty();
    }

    [Fact]
    public async Task PostIncomeSplitAsync_WhenSaveFails_RollsBackAllFourMovements()
    {
        var repository = new StubCashFlowRepository { ThrowOnSave = true };
        var service = new ReserveService(repository);

        var act = async () => await service.PostIncomeSplitAsync(ValidIncomeSplitRequest());

        await act.Should().ThrowAsync<InvalidOperationException>();
        repository.ReserveMovements.Should().BeEmpty();
    }

    [Fact]
    public async Task PostWithdrawalAsync_WithinBalance_PostsNegativeMovement()
    {
        var repository = new StubCashFlowRepository();
        repository.Seed(ReserveBucket.Investimento, 100m);
        var service = new ReserveService(repository);

        var result = await service.PostWithdrawalAsync(new WithdrawalRequestDTO
        {
            Bucket = "Investimento",
            Amount = 30m,
            Date = new DateOnly(2026, 7, 1),
            Description = "Groceries top-up",
            Confirmed = false
        });

        result.Amount.Should().Be(-30m);
        repository.ReserveMovements.Should().HaveCount(2);
    }

    [Fact]
    public async Task PostWithdrawalAsync_ExceedingBalanceUnconfirmed_ThrowsOverdraftException()
    {
        var repository = new StubCashFlowRepository();
        repository.Seed(ReserveBucket.Ariana, 50m);
        var service = new ReserveService(repository);

        var act = async () => await service.PostWithdrawalAsync(new WithdrawalRequestDTO
        {
            Bucket = "Ariana",
            Amount = 100m,
            Date = new DateOnly(2026, 7, 1),
            Description = "Big purchase",
            Confirmed = false
        });

        await act.Should().ThrowAsync<OverdraftConfirmationRequiredException>().WithMessage("*Ariana*50*");
        repository.ReserveMovements.Should().HaveCount(1);
    }

    [Fact]
    public async Task PostWithdrawalAsync_ExceedingBalanceConfirmed_Saves()
    {
        var repository = new StubCashFlowRepository();
        repository.Seed(ReserveBucket.Ariana, 50m);
        var service = new ReserveService(repository);

        var result = await service.PostWithdrawalAsync(new WithdrawalRequestDTO
        {
            Bucket = "Ariana",
            Amount = 100m,
            Date = new DateOnly(2026, 7, 1),
            Description = "Big purchase",
            Confirmed = true
        });

        result.Amount.Should().Be(-100m);
        repository.ReserveMovements.Should().HaveCount(2);
    }

    [Fact]
    public async Task PostWithdrawalAsync_WithZeroAmount_ThrowsArgumentException()
    {
        var service = new ReserveService(new StubCashFlowRepository());

        var act = async () => await service.PostWithdrawalAsync(new WithdrawalRequestDTO
        {
            Bucket = "Investimento",
            Amount = 0m,
            Date = new DateOnly(2026, 7, 1),
            Description = "Nothing"
        });

        await act.Should().ThrowAsync<ArgumentException>().WithMessage("*greater than zero*");
    }

    [Fact]
    public async Task PostWithdrawalAsync_WithUnknownBucket_ThrowsArgumentException()
    {
        var service = new ReserveService(new StubCashFlowRepository());

        var act = async () => await service.PostWithdrawalAsync(new WithdrawalRequestDTO
        {
            Bucket = "NotABucket",
            Amount = 10m,
            Date = new DateOnly(2026, 7, 1),
            Description = "Test"
        });

        await act.Should().ThrowAsync<ArgumentException>().WithMessage("*not recognized*");
    }

    [Fact]
    public void GetBucketBalances_AlwaysReturnsExactlyFourBuckets()
    {
        var repository = new StubCashFlowRepository();
        var service = new ReserveService(repository);

        var balances = service.GetBucketBalances();

        balances.Should().HaveCount(4);
        balances.Should().OnlyContain(b => b.Balance == 0m);
    }

    [Fact]
    public async Task GetBucketBalances_ReflectsPostedMovements()
    {
        var repository = new StubCashFlowRepository();
        var service = new ReserveService(repository);
        await service.PostIncomeSplitAsync(ValidIncomeSplitRequest());

        var balances = service.GetBucketBalances();

        balances.Should().ContainSingle(b => b.Bucket == "Investimento" && b.Balance == 654.33m);
    }

    [Fact]
    public void GetMovementHistory_ReturnsAllMovementsOrderedByDateDescending()
    {
        var repository = new StubCashFlowRepository();
        repository.Seed(ReserveBucket.Investimento, 10m, new DateOnly(2026, 8, 1));
        repository.Seed(ReserveBucket.Investimento, 5m, new DateOnly(2026, 7, 1));
        var service = new ReserveService(repository);

        var history = service.GetMovementHistory();

        history.Should().HaveCount(2);
        history.Select(m => m.Date).Should().BeInDescendingOrder();
    }

    private static IncomeSplitRequestDTO ValidIncomeSplitRequest() => new()
    {
        Date = new DateOnly(2026, 7, 1),
        GleisonSalaryGross = 4500m,
        GleisonSalaryNet = 3600m,
        ArianaSalaryGross = 3200m,
        ArianaSalaryNet = 2600m,
        Lottery = 50m,
        DividendoJuros = 120m
    };

    private sealed class StubCashFlowRepository : ICashFlowRepository
    {
        public List<ReserveMovement> ReserveMovements { get; } = new();
        public int SaveChangesCallCount { get; private set; }
        public bool ThrowOnSave { get; set; }

        public void Seed(ReserveBucket bucket, decimal amount, DateOnly? date = null) =>
            ReserveMovements.Add(ReserveMovement.Create(bucket, amount, date ?? new DateOnly(2026, 1, 1), "Seed"));

        public IEnumerable<Expense> GetExpenses() => Array.Empty<Expense>();
        public void AddExpense(Expense expense) { }
        public void DeleteExpense(Guid id) { }

        public IEnumerable<ReserveMovement> GetReserveMovements() => ReserveMovements;
        public void AddReserveMovement(ReserveMovement movement) => ReserveMovements.Add(movement);
        public void DeleteReserveMovement(Guid id) => ReserveMovements.RemoveAll(m => m.Id == id);

        public IEnumerable<CardStatement> GetCardStatements() => Array.Empty<CardStatement>();
        public void AddCardStatement(CardStatement statement) { }

        public IEnumerable<RecurringBillTemplate> GetRecurringBillTemplates() => Array.Empty<RecurringBillTemplate>();
        public void AddRecurringBillTemplate(RecurringBillTemplate template) { }
        public void DeleteRecurringBillTemplate(Guid id) { }

        public IEnumerable<RecurringBillInstance> GetRecurringBillInstances() => Array.Empty<RecurringBillInstance>();
        public void AddRecurringBillInstance(RecurringBillInstance instance) { }
        public void DeleteRecurringBillInstance(Guid id) { }

        public IEnumerable<MaeLedgerEntry> GetMaeLedgerEntries() => Array.Empty<MaeLedgerEntry>();
        public void AddMaeLedgerEntry(MaeLedgerEntry entry) { }

        public IEnumerable<InvestmentSnapshot> GetInvestmentSnapshots() => Array.Empty<InvestmentSnapshot>();
        public void AddInvestmentSnapshot(InvestmentSnapshot snapshot) { }

        public Task SaveChangesAsync()
        {
            SaveChangesCallCount++;
            if (ThrowOnSave)
            {
                throw new InvalidOperationException("Simulated save failure.");
            }

            return Task.CompletedTask;
        }
    }
}
