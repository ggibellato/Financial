using Financial.CashFlow.Domain.Entities;
using Financial.CashFlow.Domain.Enums;
using Financial.CashFlow.Infrastructure.Persistence;
using Financial.CashFlow.Infrastructure.Repositories;
using Financial.Shared.Abstractions.Persistence;
using Financial.Shared.Infrastructure.Persistence;
using Financial.Shared.Infrastructure.Sync;
using Financial.TestUtilities;
using FluentAssertions;

namespace Financial.CashFlow.Infrastructure.Tests.Repositories;

public class CashFlowJsonRepositoryTests
{
    [Fact]
    public async Task ApplyAndSaveAsync_WritesSerializedDataThroughStorage()
    {
        var path = Path.Combine(Path.GetTempPath(), $"cashflow-repo-{Guid.NewGuid()}.json");
        var storage = new LocalJsonStorage(path);
        var serializer = new CashFlowSerializerAdapter();
        var data = CashFlowData.Create();
        var repository = new CashFlowJsonRepository(data, storage, serializer);

        try
        {
            var bank = Bank.Create("Chase", roundUpEnabled: true);
            var category = Category.Create("Casa");
            data.AddBank(bank);
            data.AddCategory(category);
            repository.AddExpense(Expense.Create(new DateOnly(2026, 7, 1), "Test expense", 10m, category, bank, null));

            await repository.ApplyAndSaveAsync(() => true);

            var written = await storage.ReadAsync();
            serializer.Deserialize(written).Expenses.Should().ContainSingle();
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public async Task ApplyAndSaveAsync_WhenWriteFails_PropagatesException()
    {
        var invalidPath = Path.Combine(Path.GetTempPath(), $"cashflow-missing-dir-{Guid.NewGuid()}", "data.json");
        var storage = new LocalJsonStorage(invalidPath);
        var serializer = new CashFlowSerializerAdapter();
        var repository = new CashFlowJsonRepository(CashFlowData.Create(), storage, serializer);

        var act = async () => await repository.ApplyAndSaveAsync(() => true);

        await act.Should().ThrowAsync<DirectoryNotFoundException>();
    }

    [Fact]
    public async Task ApplyAndSaveAsync_WhenApplyReportsAChange_AppliesItAndWrites()
    {
        var storage = new RecordingJsonStorage();
        var data = CashFlowData.Create();
        var repository = new CashFlowJsonRepository(data, storage, new CashFlowSerializerAdapter());

        var saved = await repository.ApplyAndSaveAsync(() =>
        {
            repository.AddInvestmentAccount(InvestmentAccount.Create("ChaseSave", isActive: true, isLiability: false));
            return true;
        });

        saved.Should().BeTrue();
        storage.WriteCount.Should().Be(1);
        repository.GetInvestmentAccounts().Should().ContainSingle();
    }

    /// <summary>
    /// How an in-memory-only correction runs: a compensating rollback must undo the change under the
    /// same exclusion, but must not write - the save it is undoing has already failed.
    /// </summary>
    [Fact]
    public async Task ApplyAndSaveAsync_WhenApplyReportsNoChange_StillAppliesItButWritesNothing()
    {
        var storage = new RecordingJsonStorage();
        var repository = new CashFlowJsonRepository(CashFlowData.Create(), storage, new CashFlowSerializerAdapter());

        var saved = await repository.ApplyAndSaveAsync(() =>
        {
            repository.AddInvestmentAccount(InvestmentAccount.Create("ChaseSave", isActive: true, isLiability: false));
            return false;
        });

        saved.Should().BeFalse();
        storage.WriteCount.Should().Be(0);
        repository.GetInvestmentAccounts().Should().ContainSingle();
    }

    [Fact]
    public async Task ApplyAndSaveAsync_WithNullApplyChanges_Throws()
    {
        var repository = new CashFlowJsonRepository(CashFlowData.Create(), new RecordingJsonStorage(), new CashFlowSerializerAdapter());

        var act = async () => await repository.ApplyAndSaveAsync(null!);

        await act.Should().ThrowAsync<ArgumentNullException>().WithParameterName("applyChanges");
    }

    /// <summary>
    /// The contract is "one mutation at a time", not "one write at a time": a second caller's change
    /// must not be applied while the first caller's document is still being written, because writing
    /// serializes the whole graph.
    /// </summary>
    [Fact]
    public async Task ApplyAndSaveAsync_WhileAnotherSaveIsWriting_DoesNotRunItsMutation()
    {
        var storage = new BlockingJsonStorage();
        var repository = new CashFlowJsonRepository(CashFlowData.Create(), storage, new CashFlowSerializerAdapter());
        var secondApplied = false;

        var first = repository.ApplyAndSaveAsync(() => true);
        await storage.WriteEntered.Task;

        var second = repository.ApplyAndSaveAsync(() => { secondApplied = true; return true; });
        await Task.WhenAny(second, Task.Delay(300));

        second.IsCompleted.Should().BeFalse("the first save still holds the gate");
        secondApplied.Should().BeFalse("a mutation must not run while the graph is being serialized");

        storage.ReleaseWrite();
        await Task.WhenAll(first, second);

        secondApplied.Should().BeTrue();
        storage.WriteCount.Should().Be(2);
    }

    /// <summary>
    /// The repository is a singleton, so a gate left held by a failed write would hang every later
    /// save for the lifetime of the process instead of throwing. This is what makes the compensating
    /// rollbacks in CardStatementService able to acquire the gate after their save threw.
    /// </summary>
    [Fact]
    public async Task ApplyAndSaveAsync_WhenTheWriteThrows_LeavesTheGateAvailable()
    {
        var storage = new RecordingJsonStorage { FailNextWrite = true };
        var repository = new CashFlowJsonRepository(CashFlowData.Create(), storage, new CashFlowSerializerAdapter());

        var failing = async () => await repository.ApplyAndSaveAsync(() => true);
        await failing.Should().ThrowAsync<IOException>();

        var next = repository.ApplyAndSaveAsync(() => true);

        (await Task.WhenAny(next, Task.Delay(2000))).Should().BeSameAs(next, "the gate was released");
        (await next).Should().BeTrue();
    }

    private sealed class RecordingJsonStorage : IJsonStorage
    {
        public int WriteCount { get; private set; }

        public bool FailNextWrite { get; set; }

        public Task<string> ReadAsync() => throw new NotSupportedException("These tests build the data directly.");

        public Task WriteAsync(string json)
        {
            if (FailNextWrite)
            {
                FailNextWrite = false;
                throw new IOException("The process cannot access the file because it is being used by another process.");
            }

            WriteCount++;
            return Task.CompletedTask;
        }
    }

    /// <summary>Holds a write open until the test releases it, so "is the second caller blocked?" is
    /// answered by the gate rather than by timing.</summary>
    private sealed class BlockingJsonStorage : IJsonStorage
    {
        private readonly TaskCompletionSource _release = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public TaskCompletionSource WriteEntered { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public int WriteCount { get; private set; }

        public Task<string> ReadAsync() => throw new NotSupportedException("These tests build the data directly.");

        public void ReleaseWrite() => _release.TrySetResult();

        public async Task WriteAsync(string json)
        {
            WriteCount++;
            WriteEntered.TrySetResult();
            await _release.Task;
        }
    }

    [Fact]
    public void GetStatus_WhenStorageIsNotASyncStatusProvider_ReturnsIdleWithNoError()
    {
        var path = Path.GetTempFileName();
        try
        {
            var repository = new CashFlowJsonRepository(CashFlowData.Create(), new LocalJsonStorage(path), new CashFlowSerializerAdapter());

            var status = ((ISyncStatusProvider)repository).GetStatus();

            status.Should().Be(new SyncStatus(SyncState.Idle, null, null));
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void GetStatus_WhenStorageIsASyncStatusProvider_DelegatesToIt()
    {
        var expectedStatus = new SyncStatus(SyncState.Failed, "Drive unreachable", null);
        var storage = new FakeSyncStatusStorage { Status = expectedStatus };
        var repository = new CashFlowJsonRepository(CashFlowData.Create(), storage, new CashFlowSerializerAdapter());

        var status = ((ISyncStatusProvider)repository).GetStatus();

        status.Should().Be(expectedStatus);
    }

    [Fact]
    public async Task FlushAsync_WhenStorageIsNotASyncStatusProvider_CompletesWithoutError()
    {
        var path = Path.GetTempFileName();
        try
        {
            var repository = new CashFlowJsonRepository(CashFlowData.Create(), new LocalJsonStorage(path), new CashFlowSerializerAdapter());

            var act = async () => await ((ISyncStatusProvider)repository).FlushAsync();

            await act.Should().NotThrowAsync();
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public async Task FlushAsync_WhenStorageIsASyncStatusProvider_DelegatesToIt()
    {
        var storage = new FakeSyncStatusStorage();
        var repository = new CashFlowJsonRepository(CashFlowData.Create(), storage, new CashFlowSerializerAdapter());

        await ((ISyncStatusProvider)repository).FlushAsync();

        storage.FlushAsyncCallCount.Should().Be(1);
    }

    [Fact]
    public void Constructor_WithNullData_Throws()
    {
        var path = Path.GetTempFileName();
        try
        {
            Action act = () => new CashFlowJsonRepository(null!, new LocalJsonStorage(path), new CashFlowSerializerAdapter());
            act.Should().Throw<ArgumentNullException>().WithParameterName("data");
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void GetExpenses_ReturnsAddedExpenses()
    {
        var path = Path.GetTempFileName();
        try
        {
            var data = CashFlowData.Create();
            var repository = new CashFlowJsonRepository(data, new LocalJsonStorage(path), new CashFlowSerializerAdapter());
            var expense = Expense.Create(new DateOnly(2026, 7, 1), "Test expense", 10m, Category.Create("Casa"), Bank.Create("Chase", roundUpEnabled: true), null);

            repository.AddExpense(expense);

            repository.GetExpenses().Should().ContainSingle().Which.Id.Should().Be(expense.Id);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void DeleteExpense_RemovesTheMatchingExpense()
    {
        var path = Path.GetTempFileName();
        try
        {
            var data = CashFlowData.Create();
            var repository = new CashFlowJsonRepository(data, new LocalJsonStorage(path), new CashFlowSerializerAdapter());
            var expense = Expense.Create(new DateOnly(2026, 7, 1), "Test expense", 10m, Category.Create("Casa"), Bank.Create("Chase", roundUpEnabled: true), null);
            repository.AddExpense(expense);

            repository.DeleteExpense(expense.Id);

            repository.GetExpenses().Should().BeEmpty();
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void DeleteReserveMovement_RemovesTheMatchingMovement()
    {
        var path = Path.GetTempFileName();
        try
        {
            var data = CashFlowData.Create();
            var repository = new CashFlowJsonRepository(data, new LocalJsonStorage(path), new CashFlowSerializerAdapter());
            var movement = ReserveMovement.Create(ReserveBucket.Create("Investimento", 33.33m), 10m, new DateOnly(2026, 7, 1), "Test movement");
            repository.AddReserveMovement(movement);

            repository.DeleteReserveMovement(movement.Id);

            repository.GetReserveMovements().Should().BeEmpty();
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void GetBanks_ReturnsBanksFromTheUnderlyingData()
    {
        var path = Path.GetTempFileName();
        try
        {
            var data = CashFlowData.Create();
            data.AddBank(Bank.Create("Barclays", roundUpEnabled: false));
            var repository = new CashFlowJsonRepository(data, new LocalJsonStorage(path), new CashFlowSerializerAdapter());

            repository.GetBanks().Should().ContainSingle().Which.Name.Should().Be("Barclays");
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void GetIncomeSources_ReturnsIncomeSourcesFromTheUnderlyingData()
    {
        var path = Path.GetTempFileName();
        try
        {
            var data = CashFlowData.Create();
            data.AddIncomeSource(IncomeSource.Create("Gleison", IncomeGroup.Salary));
            var repository = new CashFlowJsonRepository(data, new LocalJsonStorage(path), new CashFlowSerializerAdapter());

            repository.GetIncomeSources().Should().ContainSingle().Which.Name.Should().Be("Gleison");
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void GetReserveBuckets_ReturnsReserveBucketsFromTheUnderlyingData()
    {
        var path = Path.GetTempFileName();
        try
        {
            var data = CashFlowData.Create();
            data.AddReserveBucket(ReserveBucket.Create("Investimento", 33.33m));
            var repository = new CashFlowJsonRepository(data, new LocalJsonStorage(path), new CashFlowSerializerAdapter());

            repository.GetReserveBuckets().Should().ContainSingle().Which.Name.Should().Be("Investimento");
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void GetCreditCards_ReturnsCreditCardsFromTheUnderlyingData()
    {
        var path = Path.GetTempFileName();
        try
        {
            var data = CashFlowData.Create();
            data.AddCreditCard(Domain.Entities.CreditCard.Create("Chase Freedom"));
            var repository = new CashFlowJsonRepository(data, new LocalJsonStorage(path), new CashFlowSerializerAdapter());

            repository.GetCreditCards().Should().ContainSingle().Which.Name.Should().Be("Chase Freedom");
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void GetCategories_ReturnsCategoriesFromTheUnderlyingData()
    {
        var path = Path.GetTempFileName();
        try
        {
            var data = CashFlowData.Create();
            data.AddCategory(Domain.Entities.Category.Create("Mercado"));
            var repository = new CashFlowJsonRepository(data, new LocalJsonStorage(path), new CashFlowSerializerAdapter());

            repository.GetCategories().Should().ContainSingle().Which.Name.Should().Be("Mercado");
        }
        finally
        {
            File.Delete(path);
        }
    }


    [Fact]
    public void GetInvestmentAccounts_ReturnsInvestmentAccountsFromTheUnderlyingData()
    {
        var path = Path.GetTempFileName();
        try
        {
            var data = CashFlowData.Create();
            var repository = new CashFlowJsonRepository(data, new LocalJsonStorage(path), new CashFlowSerializerAdapter());

            repository.AddInvestmentAccount(InvestmentAccount.Create("ChaseSave", isActive: true, isLiability: false));

            repository.GetInvestmentAccounts().Should().ContainSingle().Which.Name.Should().Be("ChaseSave");
        }
        finally
        {
            File.Delete(path);
        }
    }
}
