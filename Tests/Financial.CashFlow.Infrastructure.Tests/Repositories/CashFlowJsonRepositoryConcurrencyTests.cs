using Financial.CashFlow.Domain.Entities;
using Financial.CashFlow.Infrastructure.Persistence;
using Financial.CashFlow.Infrastructure.Repositories;
using Financial.Shared.Abstractions.Persistence;
using FluentAssertions;

namespace Financial.CashFlow.Infrastructure.Tests.Repositories;

public class CashFlowJsonRepositoryConcurrencyTests
{
    private const int SeededExpenseCount = 1000;

    [Fact]
    public async Task GetExpenses_EnumerationStartedBeforeSave_CompletesWhenSaveAddsExpense()
    {
        var (repository, storage) = CreateRepositoryWithExpenses(SeededExpenseCount);

        using var enumerator = repository.GetExpenses().GetEnumerator();
        enumerator.MoveNext().Should().BeTrue();

        var save = repository.ApplyAndSaveAsync(() =>
        {
            repository.AddExpense(NewExpense(repository, "added mid enumeration"));
            return true;
        });
        await storage.WriteEntered.Task;

        var seen = 1;
        var drain = () =>
        {
            while (enumerator.MoveNext())
            {
                seen++;
            }
        };

        drain.Should().NotThrow();
        seen.Should().Be(SeededExpenseCount);

        storage.ReleaseWrite();
        await save;
        repository.GetExpenses().Should().HaveCount(SeededExpenseCount + 1);
    }

    [Fact]
    public async Task GetExpenses_EnumerationStartedBeforeSave_CompletesWhenSaveRemovesExpense()
    {
        var (repository, storage) = CreateRepositoryWithExpenses(SeededExpenseCount);
        var victim = repository.GetExpenses().First();

        using var enumerator = repository.GetExpenses().GetEnumerator();
        enumerator.MoveNext().Should().BeTrue();

        var save = repository.ApplyAndSaveAsync(() =>
        {
            repository.DeleteExpense(victim.Id);
            return true;
        });
        await storage.WriteEntered.Task;

        var seen = 1;
        var drain = () =>
        {
            while (enumerator.MoveNext())
            {
                seen++;
            }
        };

        drain.Should().NotThrow();
        seen.Should().Be(SeededExpenseCount);

        storage.ReleaseWrite();
        await save;
        repository.GetExpenses().Should().HaveCount(SeededExpenseCount - 1);
    }

    [Fact]
    public async Task GetReserveBuckets_EnumerationStartedBeforeSave_CompletesWhenSaveAddsBucket()
    {
        var data = CashFlowData.Create();
        data.AddReserveBucket(ReserveBucket.Create("Investimento", 50m));
        data.AddReserveBucket(ReserveBucket.Create("Viagem", 50m));
        var storage = new BlockingJsonStorage();
        var repository = new CashFlowJsonRepository(data, storage, new CashFlowSerializerAdapter());

        using var enumerator = repository.GetReserveBuckets().GetEnumerator();
        enumerator.MoveNext().Should().BeTrue();

        var save = repository.ApplyAndSaveAsync(() =>
        {
            repository.AddReserveBucket(ReserveBucket.Create("Casa", 10m));
            return true;
        });
        await storage.WriteEntered.Task;

        var seen = 1;
        var drain = () =>
        {
            while (enumerator.MoveNext())
            {
                seen++;
            }
        };

        drain.Should().NotThrow();
        seen.Should().Be(2);

        storage.ReleaseWrite();
        await save;
    }

    private static (CashFlowJsonRepository Repository, BlockingJsonStorage Storage) CreateRepositoryWithExpenses(int count)
    {
        var data = CashFlowData.Create();
        var bank = Bank.Create("Barclays", roundUpEnabled: false);
        var category = Category.Create("Mercado");
        data.AddBank(bank);
        data.AddCategory(category);
        for (var i = 0; i < count; i++)
        {
            data.AddExpense(Expense.Create(new DateOnly(2026, 7, 1), $"seed {i}", 10m, category, bank, creditCard: null));
        }

        var storage = new BlockingJsonStorage();
        return (new CashFlowJsonRepository(data, storage, new CashFlowSerializerAdapter()), storage);
    }

    private static Expense NewExpense(CashFlowJsonRepository repository, string description) =>
        Expense.Create(
            new DateOnly(2026, 7, 2),
            description,
            5m,
            repository.GetCategories().First(),
            repository.GetBanks().First(),
            creditCard: null);

    private sealed class BlockingJsonStorage : IJsonStorage
    {
        private readonly TaskCompletionSource _release = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public TaskCompletionSource WriteEntered { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public Task<string> ReadAsync() => throw new NotSupportedException("These tests build the data directly.");

        public void ReleaseWrite() => _release.TrySetResult();

        public async Task WriteAsync(string json)
        {
            WriteEntered.TrySetResult();
            await _release.Task;
        }
    }
}
