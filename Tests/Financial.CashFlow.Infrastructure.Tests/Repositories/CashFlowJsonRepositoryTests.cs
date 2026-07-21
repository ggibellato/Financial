using Financial.CashFlow.Domain.Entities;
using Financial.CashFlow.Infrastructure.Persistence;
using Financial.CashFlow.Infrastructure.Repositories;
using Financial.Shared.Infrastructure.Persistence;
using FluentAssertions;

namespace Financial.CashFlow.Infrastructure.Tests.Repositories;

public class CashFlowJsonRepositoryTests
{
    [Fact]
    public async Task SaveChangesAsync_WritesSerializedDataThroughStorage()
    {
        var path = Path.Combine(Path.GetTempPath(), $"cashflow-repo-{Guid.NewGuid()}.json");
        var storage = new LocalJsonStorage(path);
        var serializer = new CashFlowSerializerAdapter();
        var data = CashFlowData.Create();
        var repository = new CashFlowJsonRepository(data, storage, serializer);

        try
        {
            repository.AddExpense(Expense.Create());

            await repository.SaveChangesAsync();

            var written = await storage.ReadAsync();
            serializer.Deserialize(written).Expenses.Should().ContainSingle();
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public async Task SaveChangesAsync_WhenWriteFails_PropagatesException()
    {
        var invalidPath = Path.Combine(Path.GetTempPath(), $"cashflow-missing-dir-{Guid.NewGuid()}", "data.json");
        var storage = new LocalJsonStorage(invalidPath);
        var serializer = new CashFlowSerializerAdapter();
        var repository = new CashFlowJsonRepository(CashFlowData.Create(), storage, serializer);

        var act = async () => await repository.SaveChangesAsync();

        await act.Should().ThrowAsync<DirectoryNotFoundException>();
    }

    [Fact]
    public void Constructor_WithNullData_Throws()
    {
        Action act = () => new CashFlowJsonRepository(null!, new LocalJsonStorage(Path.GetTempFileName()), new CashFlowSerializerAdapter());
        act.Should().Throw<ArgumentNullException>().WithParameterName("data");
    }

    [Fact]
    public void GetExpenses_ReturnsAddedExpenses()
    {
        var data = CashFlowData.Create();
        var repository = new CashFlowJsonRepository(data, new LocalJsonStorage(Path.GetTempFileName()), new CashFlowSerializerAdapter());
        var expense = Expense.Create();

        repository.AddExpense(expense);

        repository.GetExpenses().Should().ContainSingle().Which.Id.Should().Be(expense.Id);
    }
}
