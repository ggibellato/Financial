using Financial.CashFlow.Infrastructure.Persistence;
using Financial.Shared.Infrastructure.Persistence;
using FluentAssertions;

namespace Financial.CashFlow.Infrastructure.Tests.Persistence;

public class CashFlowLoaderTests
{
    /// <summary>Every test loads through the same serializer; only the backing file differs.</summary>
    private readonly CashFlowSerializerAdapter _serializer;

    public CashFlowLoaderTests()
    {
        _serializer = new CashFlowSerializerAdapter();
    }

    [Fact]
    public void LoadSync_WhenFileDoesNotExist_ReturnsEmptyCashFlowData()
    {
        var missingPath = Path.Combine(Path.GetTempPath(), $"cashflow-missing-{Guid.NewGuid()}.json");
        var storage = new LocalJsonStorage(missingPath);

        var data = CashFlowLoader.LoadSync(storage, _serializer);

        data.Expenses.Should().BeEmpty();
        data.ReserveMovements.Should().BeEmpty();
        data.CardStatements.Should().BeEmpty();
        data.RecurringBills.Should().BeEmpty();
        data.MaeLedgerEntries.Should().BeEmpty();
        data.InvestmentSnapshots.Should().BeEmpty();
        data.Banks.Should().BeEmpty();
    }

    [Fact]
    public void LoadSync_WhenFileIsMalformed_PropagatesParseException()
    {
        var path = Path.Combine(Path.GetTempPath(), $"cashflow-malformed-{Guid.NewGuid()}.json");
        File.WriteAllText(path, "{ not valid json");
        var storage = new LocalJsonStorage(path);

        try
        {
            var act = () => CashFlowLoader.LoadSync(storage, _serializer);

            act.Should().Throw<Exception>();
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void LoadSync_WhenFileIsValid_DeserializesExistingData()
    {
        var path = Path.Combine(Path.GetTempPath(), $"cashflow-valid-{Guid.NewGuid()}.json");
        var original = Financial.CashFlow.Domain.Entities.CashFlowData.Create();
        var bank = Financial.CashFlow.Domain.Entities.Bank.Create("Chase", roundUpEnabled: true);
        var category = Financial.CashFlow.Domain.Entities.Category.Create("Casa");
        original.AddBank(bank);
        original.AddCategory(category);
        original.AddExpense(Financial.CashFlow.Domain.Entities.Expense.Create(
            new DateOnly(2026, 7, 1),
            "Test expense",
            10m,
            category,
            bank,
            null));
        File.WriteAllText(path, _serializer.Serialize(original));
        var storage = new LocalJsonStorage(path);

        try
        {
            var data = CashFlowLoader.LoadSync(storage, _serializer);

            data.Expenses.Should().ContainSingle();
        }
        finally
        {
            File.Delete(path);
        }
    }
}
